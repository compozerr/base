import React, { useState } from 'react';
import { CardElement, useStripe, useElements } from '@stripe/react-stripe-js';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { StripeCardElementOptions } from '@stripe/stripe-js';

// Custom styles for the Stripe Card Element
const cardElementStyle: StripeCardElementOptions = {
    style: {

        base: {
            fontSize: '16px',
            color: 'white',
            iconColor: '#aab7c4',
            fontFamily: 'system-ui, -apple-system, sans-serif',
            fontSmoothing: 'antialiased',
            '::placeholder': {
                color: '#aab7c4',
            },
        },
        invalid: {
            color: 'text-destructive',
            iconColor: 'text-destructive',
        },
        empty: {
            color: '#aab7c4',
            iconColor: '#aab7c4',
        },
        // complete: {
        //     color: 'text-success',
        //     iconColor: 'text-success',
        // },
    },
    hidePostalCode: true,
};

interface StripeElementsFormProps {
    onSuccess: (paymentMethodId: string) => Promise<void> | void;
    onError: (error: string) => Promise<void> | void;
    onCancel: () => Promise<void> | void;
    shouldReplace: boolean;
    clientSecret: string;
}

export const StripeElementsForm: React.FC<StripeElementsFormProps> = ({
    onSuccess,
    onError,
    onCancel,
    shouldReplace = false,
    clientSecret
}) => {
    const stripe = useStripe();
    const elements = useElements();
    const [isLoading, setIsLoading] = useState(false);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();

        if (!stripe || !elements) {
            // Stripe.js has not yet loaded.
            // Make sure to disable form submission until Stripe.js has loaded.
            return;
        }

        setIsLoading(true);
        setErrorMessage(null);

        const cardElement = elements.getElement(CardElement);

        if (!cardElement) {
            setIsLoading(false);
            setErrorMessage("Card element not found");
            return;
        }

        try {
            const { error, setupIntent } = await stripe.confirmCardSetup(
                clientSecret,
                {
                    payment_method: {
                        card: cardElement,
                    }
                }
            );

            if (error) {
                setErrorMessage(error.message || "An error occurred with your card");
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
            <div className="space-y-3">
                <div className="space-y-1">
                    <Label htmlFor="card-element">Card Details</Label>
                    <div className="rounded-md border p-3 focus-within:ring-1 focus-within:ring-ring">
                        <CardElement
                            id="card-element"
                            options={cardElementStyle}
                        />
                    </div>
                </div>

                {errorMessage && (
                    <div className="text-destructive text-sm bg-destructive/10 p-2 rounded">
                        {errorMessage}
                    </div>
                )}
            </div>

            <div className="flex justify-end gap-3">
                <Button
                    type="button"
                    variant="outline"
                    onClick={onCancel}
                    disabled={isLoading}
                >
                    Cancel
                </Button>
                <Button
                    type="submit"
                    variant="default"
                    disabled={!stripe || isLoading}
                    className="min-w-[100px]"
                >
                    {isLoading ? "Processing..." : shouldReplace ? "Replace Card" : "Add Card"}
                </Button>
            </div>
        </form>
    );
};

export default StripeElementsForm;
