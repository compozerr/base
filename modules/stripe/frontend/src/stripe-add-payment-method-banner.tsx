import React, { useState } from 'react';
import { api } from '@/api-client';
import { Link } from '@tanstack/react-router';

interface StripeAddPaymentMethodBannerProps {
}

export const StripeAddPaymentMethodBanner: React.FC<StripeAddPaymentMethodBannerProps> = () => {
    const { data: paymentMethodsData, isLoading, error, refetch } = api.v1.getStripePaymentMethodsUser.useQuery();

    const hasPaymentMethodOrLoading = (paymentMethodsData?.paymentMethods?.length ?? 0 > 0) || isLoading;

    if (hasPaymentMethodOrLoading) return null;

    return (
        <Link 
            to={"/settings"} 
            search={{ addPaymentMethod: true }}
            className="w-full bg-zinc-900 hover:bg-zinc-800 transition-colors duration-200 block"
        >
            <div className="px-6 py-4 flex flex-row items-center justify-center">
                <div className="flex items-center space-x-3">
                    <div className="w-8 h-8 bg-zinc-800 rounded-full flex items-center justify-center">
                        <svg className="w-4 h-4 text-zinc-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
                        </svg>
                    </div>
                    <span className="text-white text-sm font-medium">Set up billing to get started</span>
                </div>
            </div>
        </Link>
    );
};

export default StripeAddPaymentMethodBanner;
