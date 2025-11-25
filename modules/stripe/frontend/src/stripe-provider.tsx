import React from 'react';
import { loadStripe } from '@stripe/stripe-js';
import { Elements } from '@stripe/react-stripe-js';

// Use environment variable or fallback to test key for local development
const STRIPE_PUBLISHABLE_KEY = (import.meta as any).env.VITE_STRIPE_PUBLISHABLE_KEY || 'pk_test_placeholder';

// Load Stripe outside of component render to avoid recreating Stripe object on each render
const stripePromise = loadStripe(STRIPE_PUBLISHABLE_KEY);

interface StripeProviderProps {
  children: React.ReactNode;
  clientSecret: string;
}

export const StripeProvider: React.FC<StripeProviderProps> = ({ children, clientSecret }) => {
  return (
    <Elements
      stripe={stripePromise}
      options={{
        clientSecret,
        appearance: {
          theme: 'night',
         
        },
      }}
    >
      {children}
    </Elements>
  );
};

export default StripeProvider;
