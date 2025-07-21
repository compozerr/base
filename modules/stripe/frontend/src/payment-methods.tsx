import React, { useEffect, useState } from 'react';
import { api } from '@/api-client';
import {
    Card,
    CardHeader,
    CardTitle,
    CardDescription,
    CardContent,
} from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { Badge } from '@/components/ui/badge';
import { CreditCard, Plus, Trash2, Star, Shield, Calendar } from 'lucide-react';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog';
import { useToast } from '@/hooks/use-toast';
import StripeProvider from './stripe-provider';
import StripeElementsForm from './stripe-elements-form';
import LoadingButton from '@/components/loading-button';

interface PaymentMethodsProps {
    openAddPaymentMethodDialogOnInit?: boolean;
}

export const PaymentMethods: React.FC<PaymentMethodsProps> = ({ openAddPaymentMethodDialogOnInit }) => {
    const { toast } = useToast();
    const [openDialog, setOpenDialog] = useState(false);

    const { data: paymentMethodsData, isLoading, error, refetch } = api.v1.getStripePaymentMethodsUser.useQuery();

    const { mutateAsync: createSetupIntent } = api.v1.postStripePaymentMethodsSetupIntent.useMutation();
    const { mutateAsync: attachPaymentMethod } = api.v1.postStripePaymentMethodsAttach.useMutation();
    const { mutateAsync: setDefaultPaymentMethod } = api.v1.postStripePaymentMethodsDefault.useMutation();
    const { mutateAsync: removePaymentMethod } = api.v1.deleteStripePaymentMethods.useMutation();

    const [deleteIsLoading, setDeleteIsLoading] = useState(false);
    const [clientSecret, setClientSecret] = useState<string | null>(null);
    const [isCreatingSetupIntent, setIsCreatingSetupIntent] = useState(false);

    // Handler for when a payment method is successfully created by the Stripe Elements form
    const handleCardAdded = async (paymentMethodId: string) => {
        try {
            await attachPaymentMethod({
                body: { paymentMethodId }
            });

            toast({
                title: "Card added successfully",
                description: "Your payment method has been added.",
                variant: "success",
            });

            await refetch();
            setOpenDialog(false);
            setClientSecret(null);
        } catch (err) {
            toast({
                title: "Error adding card",
                description: err instanceof Error ? err.message : "An unknown error occurred",
                variant: "destructive",
            });
        }
    };

    const handleOpenDialog = async () => {
        try {
            setIsCreatingSetupIntent(true);
            const response = await createSetupIntent({});
            if (response.clientSecret) {
                setClientSecret(response.clientSecret);
                setOpenDialog(true);
            }
        } catch (err) {
            toast({
                title: "Error preparing payment form",
                description: err instanceof Error ? err.message : "An unknown error occurred",
                variant: "destructive",
            });
        } finally {
            setIsCreatingSetupIntent(false);
        }
    };

    useEffect(() => {
        if (openAddPaymentMethodDialogOnInit) {
            handleOpenDialog();
        }
    }, [openAddPaymentMethodDialogOnInit]);

    const handleSetDefault = async (paymentMethodId: string) => {
        try {
            // Call API to set default payment method
            await setDefaultPaymentMethod({
                body: { paymentMethodId }
            });

            toast({
                title: "Payment method updated",
                description: "Default payment method has been updated.",
                variant: "success",
            });
            await refetch();
        } catch (err) {
            toast({
                title: "Error updating payment method",
                description: err instanceof Error ? err.message : "An unknown error occurred",
                variant: "destructive",
            });
        }
    };

    const handleDelete = async (paymentMethodId: string) => {
        if (deleteIsLoading) return; // Prevent multiple clicks
        const confirmDelete = window.confirm("Are you sure you want to remove this payment method?");
        if (!confirmDelete) return;
        setDeleteIsLoading(true);
        try {
            // Call API to delete payment method
            await removePaymentMethod({
                query: { paymentMethodId }
            });

            await refetch();

            toast({
                title: "Payment method removed",
                description: "Your payment method has been removed.",
                variant: "success",
            });
        } catch (err) {
            toast({
                title: "Error removing payment method",
                description: err instanceof Error ? err.message : "An unknown error occurred",
                variant: "destructive",
            });
        } finally {
            setDeleteIsLoading(false);
        }
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

    const shouldReplace = paymentMethods.length > 0;

    return (
        <div className="space-y-4">
            <div className="flex items-center justify-between">
                <h3 className="text-lg font-medium">Payment Method</h3>
                {<Dialog open={openDialog} onOpenChange={(open) => {
                    setOpenDialog(open);
                    if (!open) {
                        setClientSecret(null);
                    }
                }}>
                    {/* <DialogTrigger asChild>
                        <Button size="sm" variant="outline">
                            <Plus className="h-4 w-4 mr-2" /> Add Payment Method
                        </Button>
                    </DialogTrigger> */}
                    <DialogContent>
                        <DialogHeader>
                            <DialogTitle>{shouldReplace ? "Replace" : "Add"} Payment Method</DialogTitle>
                            <DialogDescription>
                                Enter your card information to add a new payment method.
                            </DialogDescription>
                        </DialogHeader>
                        <div className="py-4">
                            {clientSecret ? (
                                <StripeProvider>
                                    <StripeElementsForm
                                        shouldReplace={shouldReplace}
                                        clientSecret={clientSecret}
                                        onSuccess={handleCardAdded}
                                        onError={(errorMessage) => {
                                            toast({
                                                title: "Error",
                                                description: errorMessage,
                                                variant: "destructive",
                                            });
                                        }}
                                        onCancel={() => {
                                            setOpenDialog(false);
                                            setClientSecret(null);
                                        }}
                                    />
                                </StripeProvider>
                            ) : (
                                <div className="flex items-center justify-center p-4">
                                    <div className="text-center">
                                        {isCreatingSetupIntent ? "Preparing form..." : "Loading..."}
                                    </div>
                                </div>
                            )}
                        </div>
                    </DialogContent>
                </Dialog>}
            </div>

            {paymentMethods.length > 0 ? (
                <div className="space-y-3">
                    <style>{`
                        @keyframes subtle-glow {
                            0%, 100% {
                                box-shadow: 0 0 15px rgba(59, 130, 246, 0.05);
                            }
                            50% {
                                box-shadow: 0 0 25px rgba(147, 51, 234, 0.1);
                            }
                        }
                        .payment-card {
                            animation: subtle-glow 4s ease-in-out infinite;
                        }
                        .payment-card:hover {
                            transform: translateY(-1px);
                        }
                    `}</style>
                    {paymentMethods.map((method) => {
                        const getCardAccent = (brand: string) => {
                            const brandLower = brand.toLowerCase();
                            switch (brandLower) {
                                case 'visa':
                                    return 'from-blue-500/20 to-blue-600/30 border-blue-500/30';
                                case 'mastercard':
                                    return 'from-red-500/20 to-orange-500/30 border-red-500/30';
                                case 'amex':
                                    return 'from-teal-500/20 to-cyan-500/30 border-teal-500/30';
                                case 'discover':
                                    return 'from-orange-500/20 to-amber-500/30 border-orange-500/30';
                                default:
                                    return 'from-zinc-500/20 to-zinc-600/30 border-zinc-500/30';
                            }
                        };

                        const getBrandIcon = (brand: string) => {
                            const brandLower = brand.toLowerCase();
                            switch (brandLower) {
                                case 'visa':
                                    return <div className="text-blue-300 font-bold text-xs tracking-wider">VISA</div>;
                                case 'mastercard':
                                    return (
                                        <div className="flex items-center gap-0.5">
                                            <div className="w-3 h-3 rounded-full bg-red-400/80"></div>
                                            <div className="w-3 h-3 rounded-full bg-yellow-400/80 -ml-1.5"></div>
                                        </div>
                                    );
                                case 'amex':
                                    return <div className="text-teal-300 font-bold text-[10px] tracking-wider">AMEX</div>;
                                default:
                                    return <CreditCard className="h-3 w-3 text-zinc-300" />;
                            }
                        };

                        return (
                            <div key={method.id} className="group max-w-sm">
                                <Card className={`payment-card border-zinc-800 bg-zinc-900/70 backdrop-blur-sm overflow-hidden transition-all duration-300 hover:border-zinc-700 aspect-[1.586/1] ${method.isDefault ? 'ring-1 ring-blue-500/30' : ''}`}>
                                    <CardContent className="p-0 h-full flex flex-col">
                                        <div className={`relative p-4 bg-gradient-to-br ${getCardAccent(method.brand || '')} border-b border-zinc-800/50 flex-1`}>
                                            <div className="absolute inset-0 bg-gradient-to-br from-blue-500/5 via-purple-500/5 to-transparent"></div>
                                            <div className="absolute top-2 right-2 w-8 h-8 rounded-full border border-zinc-700/30 opacity-20"></div>
                                            <div className="absolute top-3 right-3 w-6 h-6 rounded-full border border-zinc-600/20 opacity-15"></div>
                                            
                                            <div className="relative z-10 h-full flex flex-col justify-between">
                                                <div className="flex items-center justify-between">
                                                    {getBrandIcon(method.brand || '')}
                                                    <span className="inline-flex items-center px-1.5 py-0.5 text-xs font-medium bg-green-500/20 text-green-300 rounded border border-green-500/30">
                                                        <Shield className="w-2.5 h-2.5 mr-1 fill-current" />
                                                        Secured
                                                    </span>
                                                </div>
                                                
                                                <div className="space-y-2">
                                                    <div className="font-mono text-sm font-medium text-white tracking-[0.15em]">
                                                        •••• •••• •••• {method.last4}
                                                    </div>
                                                    
                                                    <div className="flex items-center justify-between text-xs text-zinc-400">
                                                        <div className="flex items-center gap-1">
                                                            <Calendar className="w-2.5 h-2.5" />
                                                            <span>{method.expiryMonth?.toString().padStart(2, '0')}/{method.expiryYear}</span>
                                                        </div>
                                                        
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                        
                                        <div className="p-3 flex flex-col justify-center">
                                            <div className="mb-2">
                                                <h4 className="font-medium text-white text-xs mb-0.5">
                                                    {method.brand?.toUpperCase()} Card
                                                </h4>
                                                <p className="text-xs text-zinc-400">
                                                    {method.isDefault 
                                                        ? "Your default payment method" 
                                                        : "Available for billing"
                                                    }
                                                </p>
                                            </div>
                                            
                                            <div className="flex flex-wrap gap-1.5">
                                                {!method.isDefault && (
                                                    <Button
                                                        variant="ghost"
                                                        size="sm"
                                                        onClick={() => handleSetDefault(method.id!)}
                                                        className="h-6 px-2 text-xs text-zinc-300 hover:text-white hover:bg-zinc-800 border border-zinc-700 hover:border-zinc-600 transition-all duration-200"
                                                    >
                                                        <Star className="w-2 h-2 mr-1" />
                                                        Default
                                                    </Button>
                                                )}
                                                <Button
                                                    variant="ghost"
                                                    size="sm"
                                                    onClick={handleOpenDialog}
                                                    className="h-6 px-2 text-xs text-zinc-300 hover:text-white hover:bg-zinc-800 border border-zinc-700 hover:border-zinc-600 transition-all duration-200"
                                                >
                                                    Replace
                                                </Button>
                                                <LoadingButton
                                                    isLoading={deleteIsLoading}
                                                    variant="ghost"
                                                    size="sm"
                                                    className="h-6 px-2 text-xs text-red-400 hover:text-red-300 hover:bg-red-500/10 border border-zinc-700 hover:border-red-500/30 transition-all duration-200"
                                                    onClick={() => handleDelete(method.id!)}
                                                >
                                                    Remove
                                                </LoadingButton>
                                            </div>
                                        </div>
                                    </CardContent>
                                </Card>
                            </div>
                        );
                    })}
                </div>
            ) : (
                <Card className="border-zinc-800 bg-zinc-900/50 backdrop-blur-sm border-dashed hover:bg-zinc-900/70 hover:border-zinc-700 transition-all duration-200">
                    <CardContent className="py-8">
                        <div className="text-center space-y-4">
                            <div className="mx-auto w-12 h-12 rounded-full bg-zinc-800/50 flex items-center justify-center">
                                <CreditCard className="h-5 w-5 text-zinc-500" />
                            </div>
                            <div className="space-y-2">
                                <h4 className="font-medium text-white text-sm">No payment method</h4>
                                <p className="text-xs text-zinc-500 max-w-xs mx-auto">
                                    Add a payment method to enable billing
                                </p>
                            </div>
                            <Button 
                                variant="ghost"
                                size="sm"
                                onClick={handleOpenDialog}
                                className="bg-white text-black hover:bg-zinc-200 font-medium px-4 py-2 text-sm transition-all duration-200"
                            >
                                <Plus className="h-3.5 w-3.5 mr-1.5" /> 
                                Add Payment Method
                            </Button>
                        </div>
                    </CardContent>
                </Card>
            )}
        </div>
    );
};

export default PaymentMethods;
