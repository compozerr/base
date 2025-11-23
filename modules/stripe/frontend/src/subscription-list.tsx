import React, { useCallback, useMemo } from 'react';
import { api } from '@/api-client';
import {
  Card,
  CardContent,
} from '@/components/ui/card';
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

import { Link, useNavigate } from '@tanstack/react-router'

interface SubscriptionListProps {
}

export const SubscriptionList: React.FC<SubscriptionListProps> = () => {
  const { data: subscriptionsData, isLoading, error } = api.v1.getStripeSubscriptionsUser.useQuery();

  const subscriptions = subscriptionsData?.subscriptions || [];

  const projectQueries = api.v1.getProjectsProjectId.useQueries({
    queries: (subscriptions || []).map((s) => ({
      parameters: {
        path: {
          projectId: s.projectId || ''
        }
      },
      enabled: !!s.projectId
    }))
  });

  const projects = useMemo(() => {
    if (isLoading || error) return [];
    return projectQueries.map(x => x.data).filter(Boolean);
  }, [projectQueries, subscriptions]);

  const getProjectNameById = useCallback((projectId: string) => {
    const project = projects.find(p => p?.id === projectId);
    return project ? project.name : 'Unknown Project';
  }, [projects]);

  const navigate = useNavigate();

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

  const navigateToProjectSettings = (projectId: string) => {
    if (!projectId) return;
    window.location.href = `/projects/${projectId}/settings/general`;
  };

  return (
    <div className="space-y-4">
      <h3 className="text-lg font-medium">Your Subscriptions</h3>

      {subscriptions.length > 0 ? (
        <Card>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Tier</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Period</TableHead>
                  <TableHead className="text-right">Amount</TableHead>
                  <TableHead></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {subscriptions.map((subscription) => (
                  <TableRow key={subscription.id}>
                    <TableCell className="font-medium"><Link to={`/projects/$projectId`} params={{ projectId: subscription.projectId! }}>{getProjectNameById(subscription.projectId!)}</Link></TableCell>
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
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => navigate({ to: `/projects/${subscription.projectId}/settings/general` })}
                        >
                          Manage Subscription
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
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
