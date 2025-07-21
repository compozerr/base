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
                <div className="space-y-6">
                    <style>{`
                        @keyframes wave {
                            0%, 100% {
                                transform: scale(0.8);
                                opacity: 0.2;
                            }
                            50% {
                                transform: scale(1);
                                opacity: 0.4;
                            }
                        }
                        @keyframes rotate3d {
                            0% {
                                transform: rotateX(0deg) rotateY(0deg) rotateZ(0deg);
                            }
                            33% {
                                transform: rotateX(120deg) rotateY(120deg) rotateZ(0deg);
                            }
                            66% {
                                transform: rotateX(240deg) rotateY(240deg) rotateZ(120deg);
                            }
                            100% {
                                transform: rotateX(360deg) rotateY(360deg) rotateZ(360deg);
                            }
                        }
                        @keyframes float {
                            0%, 100% {
                                transform: translateZ(0px);
                            }
                            50% {
                                transform: translateZ(15px);
                            }
                        }
                        @keyframes glow {
                            0%, 100% {
                                box-shadow: 0 0 20px rgba(59, 130, 246, 0.2);
                            }
                            50% {
                                box-shadow: 0 0 35px rgba(147, 51, 234, 0.3);
                            }
                        }
                        @keyframes slideUp {
                            from {
                                transform: translateY(20px);
                                opacity: 0;
                            }
                            to {
                                transform: translateY(0);
                                opacity: 1;
                            }
                        }
                        .wave-dot {
                            animation: wave 4s ease-in-out infinite;
                        }
                        .cube-3d {
                            transform-style: preserve-3d;
                            animation: rotate3d 12s linear infinite;
                        }                                        .cube-face {
                                            position: absolute;
                                            width: 24px;
                                            height: 24px;
                                            background: linear-gradient(45deg, rgba(59, 130, 246, 0.15), rgba(147, 51, 234, 0.15));
                                            border: 1px solid rgba(59, 130, 246, 0.2);
                                            animation: glow 4s ease-in-out infinite;
                                        }
                                        .cube-face:nth-child(1) { transform: rotateY(0deg) translateZ(12px); }
                                        .cube-face:nth-child(2) { transform: rotateY(90deg) translateZ(12px); }
                                        .cube-face:nth-child(3) { transform: rotateY(180deg) translateZ(12px); }
                                        .cube-face:nth-child(4) { transform: rotateY(-90deg) translateZ(12px); }
                                        .cube-face:nth-child(5) { transform: rotateX(90deg) translateZ(12px); }
                                        .cube-face:nth-child(6) { transform: rotateX(-90deg) translateZ(12px); }
                        .payment-card {
                            animation: slideUp 0.6s ease-out;
                        }
                    `}</style>
                    {paymentMethods.map((method, index) => {
                        const getCardTheme = (brand: string) => {
                            const brandLower = brand.toLowerCase();
                            switch (brandLower) {
                                case 'visa':
                                    return {
                                        gradient: 'from-blue-500/10 via-blue-600/5 to-transparent',
                                        border: 'border-blue-500/20',
                                        ring: method.isDefault ? 'ring-1 ring-blue-500/40' : '',
                                        accent: 'text-blue-300'
                                    };
                                case 'mastercard':
                                    return {
                                        gradient: 'from-red-500/10 via-orange-500/5 to-transparent',
                                        border: 'border-red-500/20',
                                        ring: method.isDefault ? 'ring-1 ring-red-500/40' : '',
                                        accent: 'text-red-300'
                                    };
                                case 'amex':
                                    return {
                                        gradient: 'from-teal-500/10 via-cyan-500/5 to-transparent',
                                        border: 'border-teal-500/20',
                                        ring: method.isDefault ? 'ring-1 ring-teal-500/40' : '',
                                        accent: 'text-teal-300'
                                    };
                                default:
                                    return {
                                        gradient: 'from-purple-500/10 via-blue-500/5 to-transparent',
                                        border: 'border-zinc-600/30',
                                        ring: method.isDefault ? 'ring-1 ring-purple-500/40' : '',
                                        accent: 'text-purple-300'
                                    };
                            }
                        };

                        const getBrandIcon = (brand: string) => {
                            const brandLower = brand.toLowerCase();
                            switch (brandLower) {
                                case 'visa':
                                    return <div className="text-blue-300 font-bold text-lg tracking-wider">VISA</div>;
                                case 'mastercard':
                                    return (
                                        <div className="flex items-center gap-1">
                                            <div className="w-4 h-4 rounded-full bg-red-400"></div>
                                            <div className="w-4 h-4 rounded-full bg-yellow-400 -ml-2"></div>
                                        </div>
                                    );
                                case 'amex':
                                    return <div className="text-teal-300 font-bold text-sm tracking-wider">AMEX</div>;
                                default:
                                    return <CreditCard className="h-5 w-5 text-zinc-300" />;
                            }
                        };

                        const theme = getCardTheme(method.brand || '');
                        
                        return (
                            <div 
                                key={method.id} 
                                className="group w-full max-w-md payment-card"
                                style={{ animationDelay: `${index * 0.1}s` }}
                                onMouseMove={(e) => {
                                    const card = e.currentTarget;
                                    const rect = card.getBoundingClientRect();
                                    const x = e.clientX - rect.left;
                                    const y = e.clientY - rect.top;
                                    
                                    const dotsContainer = card.querySelector('.dots-container');
                                    if (dotsContainer) {
                                        const dots = dotsContainer.querySelectorAll('.wave-dot');
                                        dots.forEach((dot: Element, dotIndex: number) => {
                                            const htmlDot = dot as HTMLElement;
                                            const dotRect = htmlDot.getBoundingClientRect();
                                            const cardRect = card.getBoundingClientRect();
                                            const dotX = dotRect.left - cardRect.left + dotRect.width / 2;
                                            const dotY = dotRect.top - cardRect.top + dotRect.height / 2;
                                            const distance = Math.sqrt(Math.pow(x - dotX, 2) + Math.pow(y - dotY, 2));
                                            const maxDistance = 100;
                                            
                                            if (distance < maxDistance) {
                                                const intensity = 1 - (distance / maxDistance);
                                                const scale = 1 + intensity * 0.8;
                                                const opacity = 0.3 + intensity * 0.7;
                                                const hue = intensity > 0.5 ? '280' : '220';
                                                
                                                htmlDot.style.transform = `scale(${scale})`;
                                                htmlDot.style.opacity = opacity.toString();
                                                htmlDot.style.background = `radial-gradient(circle, hsl(${hue}, 60%, 60%), hsl(${hue}, 60%, 40%))`;
                                                htmlDot.style.boxShadow = `0 0 ${intensity * 20}px hsl(${hue}, 60%, 50%)`;
                                            } else {
                                                htmlDot.style.transform = '';
                                                htmlDot.style.opacity = '';
                                                htmlDot.style.background = '';
                                                htmlDot.style.boxShadow = '';
                                            }
                                        });
                                    }
                                }}
                                onMouseLeave={(e) => {
                                    const card = e.currentTarget;
                                    const dotsContainer = card.querySelector('.dots-container');
                                    if (dotsContainer) {
                                        const dots = dotsContainer.querySelectorAll('.wave-dot');
                                        dots.forEach((dot: Element) => {
                                            const htmlDot = dot as HTMLElement;
                                            htmlDot.style.transform = '';
                                            htmlDot.style.opacity = '';
                                            htmlDot.style.background = '';
                                            htmlDot.style.boxShadow = '';
                                        });
                                    }
                                }}
                            >
                                <div className={`relative overflow-hidden bg-black border ${theme.border} ${theme.ring} rounded-xl transition-all duration-500 hover:border-opacity-60`}>
                                    <div className="pointer-events-none absolute inset-0">
                                        <div className={`absolute inset-0 bg-gradient-to-br ${theme.gradient}`} />
                                        <div className="absolute right-0 top-0 h-32 w-32 bg-blue-500/5 blur-[50px]" />
                                        <div className="absolute bottom-0 left-0 h-32 w-32 bg-purple-500/5 blur-[50px]" />
                                    </div>

                                    <div className="absolute inset-0 pointer-events-none overflow-hidden">
                                        <div 
                                            className="dots-container grid gap-4 p-2" 
                                            style={{ 
                                                gridTemplateColumns: 'repeat(15, minmax(3px, 1fr))',
                                                gridTemplateRows: 'repeat(8, minmax(3px, 1fr))'
                                            }}
                                        >
                                            {Array.from({ length: 120 }, (_, i) => {
                                                const row = Math.floor(i / 15);
                                                const col = i % 15;
                                                const delay = (row * 0.1 + col * 0.05) % 4;
                                                return (
                                                    <div
                                                        key={i}
                                                        className="w-1 h-1 bg-blue-500/30 rounded-full wave-dot transition-all duration-200"
                                                        style={{
                                                            animationDelay: `${delay}s`,
                                                        }}
                                                        data-dot-index={i}
                                                    />
                                                );
                                            })}
                                        </div>
                                    </div>

                                    <div className="relative z-10 p-6 space-y-6">
                                        <div className="flex items-start justify-between">
                                            <div className="space-y-4">
                                                {getBrandIcon(method.brand || '')}
                                            </div>

                                            {method.isDefault && (
                                                <div className="relative" style={{ perspective: '100px' }}>
                                                    <div className="cube-3d relative w-6 h-6">
                                                        <div className="cube-face"></div>
                                                        <div className="cube-face"></div>
                                                        <div className="cube-face"></div>
                                                        <div className="cube-face"></div>
                                                        <div className="cube-face"></div>
                                                        <div className="cube-face"></div>
                                                    </div>
                                                </div>
                                            )}
                                        </div>

                                        <div className="space-y-3">
                                            <div className="font-mono text-xl font-medium text-white tracking-[0.2em]">
                                                •••• •••• •••• {method.last4}
                                            </div>
                                            
                                            <div className="flex items-center justify-between text-sm">
                                                <div className="flex items-center space-x-2 text-zinc-400">
                                                    <Calendar className="w-4 h-4" />
                                                    <span>{method.expiryMonth?.toString().padStart(2, '0')}/{method.expiryYear}</span>
                                                </div>
                                                <div className="flex items-center space-x-1 text-green-400">
                                                    <Shield className="w-4 h-4" />
                                                    <span className="text-xs">Secure</span>
                                                </div>
                                            </div>
                                        </div>

                                        <div className="pt-4 border-t border-zinc-800/50">
                                            <div className="space-y-3">
                                                <div>
                                                    <h4 className="font-medium text-white text-base">
                                                        {method.brand?.toUpperCase()} Card
                                                    </h4>
                                                    <p className="text-sm text-zinc-400">
                                                        {method.isDefault 
                                                            ? "Primary payment method" 
                                                            : "Available for billing"
                                                        }
                                                    </p>
                                                </div>
                                                
                                                <div className="flex flex-wrap gap-2 pt-2">
                                                    {!method.isDefault && (
                                                        <Button
                                                            variant="ghost"
                                                            size="sm"
                                                            onClick={() => handleSetDefault(method.id!)}
                                                            className="px-3 py-1.5 text-sm bg-zinc-900/80 border border-zinc-700 text-zinc-300 hover:text-white hover:bg-zinc-800 hover:border-zinc-600 transition-all duration-200"
                                                        >
                                                            <Star className="w-3 h-3 mr-1.5" />
                                                            Set Default
                                                        </Button>
                                                    )}
                                                    <Button
                                                        variant="ghost"
                                                        size="sm"
                                                        onClick={handleOpenDialog}
                                                        className="px-3 py-1.5 text-sm bg-zinc-900/80 border border-zinc-700 text-zinc-300 hover:text-white hover:bg-zinc-800 hover:border-zinc-600 transition-all duration-200"
                                                    >
                                                        Replace
                                                    </Button>
                                                    <LoadingButton
                                                        isLoading={deleteIsLoading}
                                                        variant="ghost"
                                                        size="sm"
                                                        className="px-3 py-1.5 text-sm bg-red-500/10 border border-red-500/20 text-red-400 hover:text-red-300 hover:bg-red-500/20 hover:border-red-500/30 transition-all duration-200"
                                                        onClick={() => handleDelete(method.id!)}
                                                    >
                                                        Remove
                                                    </LoadingButton>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        );
                    })}
                </div>
            ) : (
                <div 
                    className="relative overflow-hidden bg-black border border-zinc-600/30 rounded-xl"
                    onMouseMove={(e) => {
                        const card = e.currentTarget;
                        const rect = card.getBoundingClientRect();
                        const x = e.clientX - rect.left;
                        const y = e.clientY - rect.top;
                        
                        const dotsContainer = card.querySelector('.empty-dots-container');
                        if (dotsContainer) {
                            const dots = dotsContainer.querySelectorAll('.wave-dot-empty');
                            dots.forEach((dot: Element) => {
                                const htmlDot = dot as HTMLElement;
                                const dotRect = htmlDot.getBoundingClientRect();
                                const cardRect = card.getBoundingClientRect();
                                const dotX = dotRect.left - cardRect.left + dotRect.width / 2;
                                const dotY = dotRect.top - cardRect.top + dotRect.height / 2;
                                const distance = Math.sqrt(Math.pow(x - dotX, 2) + Math.pow(y - dotY, 2));
                                const maxDistance = 120;
                                
                                if (distance < maxDistance) {
                                    const intensity = 1 - (distance / maxDistance);
                                    const scale = 1 + intensity * 1.2;
                                    const opacity = 0.15 + intensity * 0.8;
                                    const hue = intensity > 0.5 ? '280' : '220';
                                    
                                    htmlDot.style.transform = `scale(${scale})`;
                                    htmlDot.style.opacity = opacity.toString();
                                    htmlDot.style.background = `radial-gradient(circle, hsl(${hue}, 60%, 60%), hsl(${hue}, 60%, 40%))`;
                                    htmlDot.style.boxShadow = `0 0 ${intensity * 15}px hsl(${hue}, 60%, 50%)`;
                                } else {
                                    htmlDot.style.transform = '';
                                    htmlDot.style.opacity = '';
                                    htmlDot.style.background = '';
                                    htmlDot.style.boxShadow = '';
                                }
                            });
                        }
                    }}
                    onMouseLeave={(e) => {
                        const card = e.currentTarget;
                        const dotsContainer = card.querySelector('.empty-dots-container');
                        if (dotsContainer) {
                            const dots = dotsContainer.querySelectorAll('.wave-dot-empty');
                            dots.forEach((dot: Element) => {
                                const htmlDot = dot as HTMLElement;
                                htmlDot.style.transform = '';
                                htmlDot.style.opacity = '';
                                htmlDot.style.background = '';
                                htmlDot.style.boxShadow = '';
                            });
                        }
                    }}
                >
                    <div className="pointer-events-none absolute inset-0">
                        <div className="absolute inset-0 bg-gradient-to-br from-purple-500/10 via-blue-500/5 to-transparent" />
                        <div className="absolute right-0 top-0 h-40 w-40 bg-blue-500/5 blur-[60px]" />
                        <div className="absolute bottom-0 left-0 h-40 w-40 bg-purple-500/5 blur-[60px]" />
                    </div>

                    <div className="absolute inset-0 pointer-events-none overflow-hidden">
                        <style>{`
                            @keyframes wave-empty {
                                0%, 100% {
                                    transform: scale(0.8);
                                    opacity: 0.15;
                                }
                                50% {
                                    transform: scale(1);
                                    opacity: 0.3;
                                }
                            }
                            .wave-dot-empty {
                                animation: wave-empty 5s ease-in-out infinite;
                            }
                        `}</style>
                        <div 
                            className="empty-dots-container grid gap-3 p-4" 
                            style={{ 
                                gridTemplateColumns: 'repeat(20, minmax(2px, 1fr))',
                                gridTemplateRows: 'repeat(12, minmax(2px, 1fr))'
                            }}
                        >
                            {Array.from({ length: 240 }, (_, i) => {
                                const row = Math.floor(i / 20);
                                const col = i % 20;
                                const delay = (row * 0.1 + col * 0.05) % 5;
                                return (
                                    <div
                                        key={i}
                                        className="w-0.5 h-0.5 bg-blue-500/25 rounded-full wave-dot-empty transition-all duration-200"
                                        style={{
                                            animationDelay: `${delay}s`,
                                        }}
                                    />
                                );
                            })}
                        </div>
                    </div>

                    <div className="relative z-10 py-12 px-8">
                        <div className="text-center space-y-8">
                            <div className="relative mx-auto w-20 h-20" style={{ perspective: '150px' }}>
                                <style>{`
                                    .empty-cube-3d {
                                        transform-style: preserve-3d;
                                        animation: rotate3d 10s linear infinite;
                                    }
                                    .empty-cube-face {
                                        position: absolute;
                                        width: 40px;
                                        height: 40px;
                                        background: linear-gradient(45deg, rgba(59, 130, 246, 0.1), rgba(147, 51, 234, 0.1));
                                        border: 1px solid rgba(59, 130, 246, 0.2);
                                        animation: glow 3s ease-in-out infinite;
                                    }
                                    .empty-cube-face:nth-child(1) { transform: rotateY(0deg) translateZ(20px); }
                                    .empty-cube-face:nth-child(2) { transform: rotateY(90deg) translateZ(20px); }
                                    .empty-cube-face:nth-child(3) { transform: rotateY(180deg) translateZ(20px); }
                                    .empty-cube-face:nth-child(4) { transform: rotateY(-90deg) translateZ(20px); }
                                    .empty-cube-face:nth-child(5) { transform: rotateX(90deg) translateZ(20px); }
                                    .empty-cube-face:nth-child(6) { transform: rotateX(-90deg) translateZ(20px); }
                                `}</style>
                                <div className="empty-cube-3d relative w-10 h-10 mx-auto">
                                    <div className="empty-cube-face"></div>
                                    <div className="empty-cube-face"></div>
                                    <div className="empty-cube-face"></div>
                                    <div className="empty-cube-face"></div>
                                    <div className="empty-cube-face"></div>
                                    <div className="empty-cube-face"></div>
                                </div>
                                <div className="absolute inset-0 flex items-center justify-center">
                                    <CreditCard className="h-8 w-8 text-zinc-400" />
                                </div>
                            </div>
                            
                            <div className="space-y-4">
                                <h4 className="font-medium text-white text-xl">No Payment Method</h4>
                                <p className="text-zinc-400 max-w-sm mx-auto leading-relaxed">
                                    Add a payment method to enable billing and start deploying your projects
                                </p>
                            </div>
                            
                            <Button 
                                onClick={handleOpenDialog}
                                className="bg-white text-black hover:bg-zinc-200 font-medium px-6 py-3 text-base transition-all duration-300 hover:scale-105"
                            >
                                <Plus className="h-4 w-4 mr-2" /> 
                                Add Payment Method
                            </Button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default PaymentMethods;
