import { api } from '@/api-client';

export const usePaymentMethod = () => {
    const { data: paymentMethodsData, isLoading } = api.v1.getStripePaymentMethodsUser.useQuery();

    const hasPaymentMethodOrLoading = (paymentMethodsData?.paymentMethods?.length ?? 0 > 0) || isLoading;

    return { hasPaymentMethod: hasPaymentMethodOrLoading, paymentMethods: paymentMethodsData?.paymentMethods };
}