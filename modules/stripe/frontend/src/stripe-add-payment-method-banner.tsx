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
        <div className="w-full h-12 flex flex-row items-center justify-center bg-slate-500">
            <Link to={"/settings"} search={
                { addPaymentMethod: true }
            }>Add a payment method - no costs involved</Link>
        </div>
    );
};

export default StripeAddPaymentMethodBanner;
