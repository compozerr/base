import React, { useState } from 'react';
import { useStripe, useElements, PaymentElement } from '@stripe/react-stripe-js';
import { Button } from '@/components/ui/button';
import { CreditCard, Wallet, Shield } from 'lucide-react';

interface StripeElementsFormProps {
    onSuccess: (paymentMethodId: string) => Promise<void> | void;
    onError: (error: string) => Promise<void> | void;
    onCancel: () => Promise<void> | void;
    shouldReplace: boolean;
}

export const StripeElementsForm: React.FC<StripeElementsFormProps> = ({
    onSuccess,
    onError,
    onCancel,
    shouldReplace = false,
}) => {
    const stripe = useStripe();
    const elements = useElements();
    const [isLoading, setIsLoading] = useState(false);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();

        if (!stripe || !elements) {
            return;
        }

        setIsLoading(true);
        setErrorMessage(null);

        try {
            const { error, setupIntent } = await stripe.confirmSetup({
                elements,
                redirect: 'if_required',
                confirmParams: {
                    return_url: window.location.origin,
                },
            });

            if (error) {
                setErrorMessage(error.message || "An error occurred");
                await onError(error.message || "Payment method setup failed");
            } else if (setupIntent && setupIntent.status === 'succeeded') {
                await onSuccess(setupIntent.payment_method as string);
            }
        } catch (err) {
            const errorMessage = err instanceof Error ? err.message : "An unknown error occurred";
            setErrorMessage(errorMessage);
            await onError(errorMessage);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <form onSubmit={handleSubmit} className="space-y-6">
            <style>{`
                @keyframes borderGlow {
                    0% {
                        background-position: 0% 50%;
                    }
                    50% {
                        background-position: 100% 50%;
                    }
                    100% {
                        background-position: 0% 50%;
                    }
                }
                .animated-border-button {
                    position: relative;
                    background: linear-gradient(135deg, rgba(59, 130, 246, 0.2), rgba(147, 51, 234, 0.2));
                }
                .animated-border-button::before {
                    content: '';
                    position: absolute;
                    inset: -1px;
                    border-radius: 0.375rem;
                    padding: 1px;
                    background: linear-gradient(
                        90deg,
                        rgba(59, 130, 246, 0.3),
                        rgba(147, 51, 234, 0.3),
                        rgba(59, 130, 246, 0.5),
                        rgba(147, 51, 234, 0.3),
                        rgba(59, 130, 246, 0.3)
                    );
                    background-size: 200% 100%;
                    -webkit-mask: linear-gradient(#fff 0 0) content-box, linear-gradient(#fff 0 0);
                    -webkit-mask-composite: xor;
                    mask-composite: exclude;
                    animation: borderGlow 3s linear infinite;
                    pointer-events: none;
                }
            `}</style>
            {/* Header */}
            <div className="space-y-3">
                <div className="flex items-center gap-3">
                    <div className="flex items-center justify-center w-10 h-10 rounded-full bg-gradient-to-br from-blue-500/20 to-purple-500/20 border border-blue-500/30">
                        <Wallet className="w-5 h-5 text-blue-400" />
                    </div>
                    <div>
                        <h3 className="font-medium text-white">Payment Details</h3>
                        <p className="text-sm text-zinc-400">Add card or digital wallet</p>
                    </div>
                </div>
            </div>

            {/* Payment Element Container */}
            <div className="relative group">
                <div className="absolute inset-0 bg-gradient-to-br from-blue-500/5 to-purple-500/5 rounded-xl blur-sm group-hover:blur-md transition-all duration-300" />
                <div className="relative rounded-xl border border-zinc-700/50 bg-black/50 p-4 backdrop-blur-sm">
                    <PaymentElement
                        options={{
                            layout: {
                                type: 'tabs',
                                defaultCollapsed: false,
                            },
                            terms: {
                                card: 'never',
                            },
                            wallets: {
                                applePay: 'auto',
                                googlePay: 'auto',
                            },
                        }}
                    />
                </div>
            </div>

            {/* Security Badge */}
            <div className="flex items-center gap-2 text-sm text-zinc-400 bg-zinc-900/50 border border-zinc-800/50 rounded-lg p-3">
                <Shield className="w-4 h-4 text-green-400" />
                <span>Secured by Stripe • Your payment details are encrypted</span>
            </div>

            {/* Error Message */}
            {errorMessage && (
                <div className="relative overflow-hidden rounded-lg border border-red-500/20 bg-red-500/5 p-4">
                    <div className="absolute inset-0 bg-gradient-to-r from-red-500/10 to-transparent" />
                    <div className="relative flex items-start gap-3">
                        <div className="flex-shrink-0 w-5 h-5 rounded-full bg-red-500/20 flex items-center justify-center mt-0.5">
                            <div className="w-2 h-2 rounded-full bg-red-400" />
                        </div>
                        <div className="flex-1">
                            <p className="text-sm font-medium text-red-400">Payment Error</p>
                            <p className="text-sm text-red-300/80 mt-1">{errorMessage}</p>
                        </div>
                    </div>
                </div>
            )}

            {/* Action Buttons */}
            <div className="flex gap-3 pt-2">
                <Button
                    type="button"
                    variant="outline"
                    onClick={onCancel}
                    disabled={isLoading}
                    className="flex-1 bg-zinc-900/80 border-zinc-700 text-zinc-300 hover:bg-zinc-800 hover:text-white hover:border-zinc-600 transition-all duration-200"
                >
                    Cancel
                </Button>
                <Button
                    type="submit"
                    disabled={!stripe || isLoading}
                    variant="ghost"
                    className="flex-1 animated-border-button text-white font-medium transition-all duration-200"
                >
                    {isLoading ? (
                        <span className="flex items-center gap-2">
                            <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                            Processing...
                        </span>
                    ) : (
                        <span className="flex items-center gap-2">
                            <CreditCard className="w-4 h-4" />
                            {shouldReplace ? "Replace Payment Method" : "Add Payment Method"}
                        </span>
                    )}
                </Button>
            </div>
        </form>
    );
};

export default StripeElementsForm;
