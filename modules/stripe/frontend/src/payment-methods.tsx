import React, { useState } from 'react';
import { api } from '@/api-client';
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  CardFooter,
} from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { Badge } from '@/components/ui/badge';
import { CreditCard, Plus, Trash2 } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Separator } from '@/components/ui/separator';
import { useToast } from '@/hooks/use-toast';

interface PaymentMethodsProps {
  userId: string;
}

export const PaymentMethods: React.FC<PaymentMethodsProps> = ({ userId }) => {
  const { toast } = useToast();
  const [openDialog, setOpenDialog] = useState(false);

  const { data: paymentMethodsData, isLoading, error, refetch } = api.v1.getStripePaymentMethodsUserUserId.useQuery({
    path: { userId },
  });

  // This would use the Stripe API mutations in a real implementation
  const handleAddCard = async () => {
    // Example: integrate with Stripe Elements for card addition
    toast({
      title: "Adding payment method",
      description: "This would integrate with Stripe Elements in a real implementation",
    });
    setOpenDialog(false);
  };

  const handleSetDefault = async (paymentMethodId: string) => {
    // Example: would call the API to set default payment method
    toast({
      title: "Payment method updated",
      description: "Default payment method has been updated.",
      variant: "success",
    });
    await refetch();
  };

  const handleDelete = async (paymentMethodId: string) => {
    // Example: would call the API to delete payment method
    toast({
      title: "Payment method removed",
      description: "Your payment method has been removed.",
      variant: "success",
    });
    await refetch();
  };

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
        Error loading payment methods: {(error as Error).message}
      </div>
    );
  }

  const paymentMethods = paymentMethodsData?.paymentMethods || [];

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-medium">Payment Methods</h3>
        <Dialog open={openDialog} onOpenChange={setOpenDialog}>
          <DialogTrigger asChild>
            <Button size="sm" variant="outline">
              <Plus className="h-4 w-4 mr-2" /> Add Payment Method
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Add Payment Method</DialogTitle>
              <DialogDescription>
                Enter your card information to add a new payment method.
              </DialogDescription>
            </DialogHeader>
            <div className="py-4">
              {/* In a real implementation, this would contain Stripe Elements */}
              <div className="rounded-md border p-4">
                <p className="text-muted-foreground text-center text-sm">
                  Stripe Elements would be integrated here
                </p>
              </div>
            </div>
            <DialogFooter>
              <Button variant="outline" onClick={() => setOpenDialog(false)}>Cancel</Button>
              <Button onClick={handleAddCard}>Add Card</Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>

      {paymentMethods.length > 0 ? (
        <div className="space-y-3">
          {paymentMethods.map((method) => (
            <Card key={method.id} className={method.isDefault ? 'border-primary/50' : ''}>
              <CardHeader className="pb-2">
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <CreditCard className="h-4 w-4" />
                    <CardTitle className="text-base">
                      {method.brand} •••• {method.last4}
                    </CardTitle>
                  </div>
                  {method.isDefault && (
                    <Badge variant="outline" className="ml-2">Default</Badge>
                  )}
                </div>
                <CardDescription>
                  Expires {method.expiryMonth}/{method.expiryYear}
                </CardDescription>
              </CardHeader>
              <CardFooter className="pt-1">
                <div className="flex justify-end space-x-2 w-full">
                  {!method.isDefault && (
                    <Button 
                      variant="ghost" 
                      size="sm"
                      onClick={() => handleSetDefault(method.id!)}
                    >
                      Make Default
                    </Button>
                  )}
                  <Button 
                    variant="ghost" 
                    size="sm" 
                    className="text-destructive"
                    onClick={() => handleDelete(method.id!)}
                  >
                    <Trash2 className="h-4 w-4 mr-1" /> Remove
                  </Button>
                </div>
              </CardFooter>
            </Card>
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="py-6">
            <div className="text-center space-y-3">
              <p className="text-muted-foreground">No payment methods added yet</p>
              <Button variant="outline" onClick={() => setOpenDialog(true)}>
                <Plus className="h-4 w-4 mr-2" /> Add Payment Method
              </Button>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

export default PaymentMethods;
