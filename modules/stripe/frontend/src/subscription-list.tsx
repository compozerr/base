import React from 'react';
import { api } from '@/api-client';
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
} from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';

interface SubscriptionListProps {
}

export const SubscriptionList: React.FC<SubscriptionListProps> = () => {
  const { data: subscriptionsData, isLoading, error } = api.v1.getStripeSubscriptionsUser.useQuery();

  if (isLoading) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-4 w-3/4" />
        <Skeleton className="h-12 w-full" />
        <Skeleton className="h-12 w-full" />
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

  const subscriptions = subscriptionsData?.subscriptions || [];

  return (
    <div className="space-y-4">
      <h3 className="text-lg font-medium">Your Subscriptions</h3>

      {subscriptions.length > 0 ? (
        <div className="space-y-4">
          {subscriptions.map((subscription) => (
            <Card key={subscription.id} className="overflow-hidden">
              <CardHeader className="bg-muted/50">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-base">{subscription.name}</CardTitle>
                  <Badge variant={
                    subscription.status === 'active' ? 'default' :
                    subscription.status === 'canceled' ? 'destructive' :
                    subscription.status === 'past_due' ? 'destructive' :
                    subscription.status === 'trialing' ? 'secondary' :
                    'outline'
                  }>
                    {subscription.status}
                    {subscription.cancelAtPeriodEnd ? ' (Cancels at period end)' : ''}
                  </Badge>
                </div>
                <CardDescription>
                  Tier: {subscription.serverTierId}
                </CardDescription>
              </CardHeader>
              <CardContent className="pt-4">
                <div className="space-y-2">
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Amount:</span>
                    <span className="font-medium">
                      {new Intl.NumberFormat('en-US', {
                        style: 'currency',
                        currency: subscription.currency!,
                      }).format(subscription.amount!)}
                    </span>
                  </div>
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">Current period:</span>
                    <span>
                      {new Date(subscription.currentPeriodStart!).toLocaleDateString()} - {new Date(subscription.currentPeriodEnd!).toLocaleDateString()}
                    </span>
                  </div>
                  {subscription.status === 'active' && !subscription.cancelAtPeriodEnd && (
                    <div className="pt-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => {
                          // Would normally implement cancel subscription functionality here
                          console.log('Cancel subscription', subscription.id);
                        }}
                      >
                        Cancel Subscription
                      </Button>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="py-6">
            <div className="text-center space-y-3">
              <p className="text-muted-foreground">No active subscriptions</p>
              <Button variant="outline">Subscribe Now</Button>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

export default SubscriptionList;
