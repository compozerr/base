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
          variables: {
            colorPrimary: '#3b82f6',
            colorBackground: '#09090b',
            colorText: '#ffffff',
            colorDanger: '#ef4444',
            fontFamily: 'system-ui, -apple-system, sans-serif',
            spacingUnit: '4px',
            borderRadius: '8px',
          },
          rules: {
            '.Tab': {
              border: '1px solid #27272a',
              backgroundColor: '#18181b',
              boxShadow: 'none',
            },
            '.Tab:hover': {
              backgroundColor: '#27272a',
              borderColor: '#3f3f46',
            },
            '.Tab--selected': {
              color: '#ffffff',
              backgroundColor: '#18181b',
              borderColor: '#3b82f6',
              boxShadow: '0 0 0 1px #3b82f6',
            },
            '.Input': {
              backgroundColor: '#18181b',
              border: '1px solid #27272a',
              color: '#ffffff',
            },
            '.Input:focus': {
              borderColor: '#3b82f6',
              boxShadow: '0 0 0 1px #3b82f6',
            },
            '.Label': {
              color: '#a1a1aa',
              fontWeight: '500',
            },
            '.p-TabIcon--selected':{
              fill: '#ffffff',
            }
          },
        },
      }}
    >
      {children}
    </Elements>
  );
};

export default StripeProvider;
