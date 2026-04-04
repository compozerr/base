import React, { useMemo } from 'react';
import { api } from '@/api-client';
import {
  Card,
  CardContent,
} from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { formatCurrency } from './invoice-utils';
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
  TableFooter
} from '@/components/ui/table';

import { Link } from '@tanstack/react-router'

const BILLING_STATUS_VARIANTS: Record<string, { variant: 'default' | 'secondary' | 'destructive' | 'outline'; label: string }> = {
  Active: { variant: 'default', label: 'Active' },
  Trialing: { variant: 'secondary', label: 'Trial' },
  Suspended: { variant: 'destructive', label: 'Suspended' },
  Cancelled: { variant: 'outline', label: 'Cancelled' },
};

function BillingStatusBadge({ status }: { status: string | null }) {
  if (!status) return null;
  const cfg = BILLING_STATUS_VARIANTS[status];
  if (!cfg) return null;
  return <Badge variant={cfg.variant} className="text-[10px] ml-1.5">{cfg.label}</Badge>;
}

interface SubscriptionListProps {
}

export const SubscriptionList: React.FC<SubscriptionListProps> = () => {
  const { data: subscriptionsData, isLoading, error } = api.v1.getStripeSubscriptionsUser.useQuery();

  const subscriptions = subscriptionsData?.subscriptions || [];

  // Get unique project IDs from subscriptions
  const uniqueProjectIds = useMemo(() => {
    return [...new Set(subscriptions.map(s => s.projectId).filter(Boolean))] as string[];
  }, [subscriptions]);

  const projectQueries = api.v1.getProjectsProjectId.useQueries({
    queries: uniqueProjectIds.map((pid) => ({
      parameters: { path: { projectId: pid } },
      enabled: !!pid
    }))
  });

  // Fetch environments for each project to match subscriptions to environments
  const environmentQueries = api.v1.getProjectsProjectIdEnvironments.useQueries({
    queries: uniqueProjectIds.map((pid) => ({
      parameters: { path: { projectId: pid } },
      enabled: !!pid
    }))
  });

  const projects = useMemo(() => {
    if (isLoading || error) return [];
    return projectQueries.map(x => x.data).filter(Boolean);
  }, [projectQueries, isLoading, error]);

  // Build a map: stripeSubscriptionId -> environment
  const subscriptionToEnvMap = useMemo(() => {
    const map = new Map<string, { name: string; type: string; billingStatus: string | null }>();
    for (const q of environmentQueries) {
      if (!q.data?.environments) continue;
      for (const env of q.data.environments) {
        if (env.stripeSubscriptionId) {
          map.set(env.stripeSubscriptionId, {
            name: env.name ?? 'Unknown',
            type: env.type ?? 'Production',
            billingStatus: env.billingStatus ?? null,
          });
        }
      }
    }
    return map;
  }, [environmentQueries]);

  const projectNameMap = useMemo(() => {
    const map = new Map<string, string>();
    for (const p of projects) {
      if (p?.id && p.name) map.set(p.id, p.name);
    }
    return map;
  }, [projects]);

  const total = useMemo(() => {
    if (subscriptions.length === 0) return { amount: 0, currency: 'USD' };
    const sum = subscriptions.reduce((acc, s) =>
      s.status === 'active' && !s.cancelAtPeriodEnd ? acc + (s.amount.amount || 0) : acc, 0);
    return { amount: sum, currency: subscriptions[0]?.amount.currency || 'USD' };
  }, [subscriptions]);

  const groupedByProject = useMemo(() => {
    const groups = new Map<string, typeof subscriptions>();
    for (const sub of subscriptions) {
      const pid = sub.projectId ?? 'unknown';
      if (!groups.has(pid)) groups.set(pid, []);
      groups.get(pid)!.push(sub);
    }
    return groups;
  }, [subscriptions]);

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-64" />
        <Card>
          <CardContent className="p-0">
            <div className="space-y-2 p-4">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-destructive">
        Error loading subscriptions: {(error as Error).message}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <h3 className="text-lg font-medium">Your Subscriptions</h3>

      {subscriptions.length > 0 ? (
        <Card>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Environment</TableHead>
                  <TableHead>Tier</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Period</TableHead>
                  <TableHead className="text-right">Amount</TableHead>
                  <TableHead></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {[...groupedByProject.entries()].map(([projectId, projectSubs]) => {
                  const projectName = projectNameMap.get(projectId) ?? 'Unknown Project';
                  const projectTotal = projectSubs.reduce((sum, s) =>
                    s.status === 'active' && !s.cancelAtPeriodEnd ? sum + (s.amount.amount || 0) : sum, 0);
                  const currency = projectSubs[0]?.amount.currency || 'USD';

                  return (
                    <React.Fragment key={projectId}>
                      <TableRow className="bg-muted/30 hover:bg-muted/30">
                        <TableCell colSpan={4} className="font-semibold py-2">
                          <Link to="/projects/$projectId" params={{ projectId }}>
                            {projectName}
                          </Link>
                        </TableCell>
                        <TableCell className="text-right font-semibold py-2">
                          {formatCurrency(projectTotal, currency)}/mo
                        </TableCell>
                        <TableCell className="py-2">
                          <Link to="/projects/$projectId/environments" params={{ projectId }}>
                            <Button variant="outline" size="sm" className="h-7 text-xs">
                              Manage
                            </Button>
                          </Link>
                        </TableCell>
                      </TableRow>

                      {projectSubs.map((subscription) => {
                        const envInfo = subscriptionToEnvMap.get(subscription.id!);
                        return (
                          <TableRow key={subscription.id}>
                            <TableCell className="pl-6">
                              {envInfo ? (
                                <span className="flex items-center gap-1.5">
                                  <span>{envInfo.name}</span>
                                  <Badge variant="outline" className="text-[10px]">{envInfo.type}</Badge>
                                  <BillingStatusBadge status={envInfo.billingStatus} />
                                </span>
                              ) : (
                                <span className="text-muted-foreground text-xs">—</span>
                              )}
                            </TableCell>
                            <TableCell>{subscription.serverTierId}</TableCell>
                            <TableCell>
                              {subscription.status!.charAt(0).toUpperCase() + subscription.status!.slice(1)}
                              {subscription.cancelAtPeriodEnd ? ' (Cancels at period end)' : ''}
                            </TableCell>
                            <TableCell>
                              {new Date(subscription.currentPeriodStart!).toLocaleDateString()} - {new Date(subscription.currentPeriodEnd!).toLocaleDateString()}
                            </TableCell>
                            <TableCell className="text-right">
                              {formatCurrency(subscription.amount.amount || 0, subscription.amount.currency || 'USD')}
                            </TableCell>
                            <TableCell></TableCell>
                          </TableRow>
                        );
                      })}
                    </React.Fragment>
                  );
                })}
              </TableBody>
              <TableFooter>
                <TableRow>
                  <TableCell colSpan={4}>Total</TableCell>
                  <TableCell className="text-right">{formatCurrency(total.amount, total.currency)}</TableCell>
                  <TableCell></TableCell>
                </TableRow>
              </TableFooter>
            </Table>
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardContent className="py-6">
            <div className="text-center space-y-3">
              <p className="text-muted-foreground">No active subscriptions</p>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

export default SubscriptionList;
