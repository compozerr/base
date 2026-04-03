import React, { useMemo } from 'react';
import { api } from '@/api-client';
import {
  Card,
  CardContent,
} from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
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

  const formatCurrency = (amount: number, currency: string) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency,
    }).format(amount);
  };

  const calculateTotal = () => {
    if (subscriptions.length === 0) return { amount: 0, currency: 'USD' };

    const total = subscriptions.reduce((sum, subscription) => {
      if (subscription.status === 'active' && !subscription.cancelAtPeriodEnd) {
        return sum + (subscription.amount.amount || 0);
      }
      return sum;
    }, 0);

    return {
      amount: total,
      currency: subscriptions[0]?.amount.currency || 'USD'
    };
  };

  const total = calculateTotal();

  return (
    <div className="space-y-4">
      <h3 className="text-lg font-medium">Your Subscriptions</h3>

      {subscriptions.length > 0 ? (
        <Card>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Project</TableHead>
                  <TableHead>Environment</TableHead>
                  <TableHead>Tier</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Period</TableHead>
                  <TableHead className="text-right">Amount</TableHead>
                  <TableHead></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {subscriptions.map((subscription) => {
                  const envInfo = subscriptionToEnvMap.get(subscription.id!);
                  return (
                    <TableRow key={subscription.id}>
                      <TableCell className="font-medium">
                        <Link to={`/projects/$projectId`} params={{ projectId: subscription.projectId! }}>
                          {projectNameMap.get(subscription.projectId!) ?? 'Unknown Project'}
                        </Link>
                      </TableCell>
                      <TableCell>
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
                      <TableCell className='text-right'>
                        {subscription.status === 'active' && !subscription.cancelAtPeriodEnd && subscription.projectId && (
                          <Link to="/projects/$projectId/environments" params={{ projectId: subscription.projectId! }}>
                            <Button variant="outline" size="sm">
                              Manage
                            </Button>
                          </Link>
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
              <TableFooter>
                <TableRow>
                  <TableCell colSpan={5}>Total</TableCell>
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
