import {
  createFileRoute,
  Outlet,
  useNavigate
} from '@tanstack/react-router'
import * as React from 'react'

import { AppSidebar } from '@/components/app-sidebar'
import { SidebarProvider, SidebarTrigger } from '@/components/ui/sidebar'
import StripeAddPaymentMethodBanner from '@repo/stripe/stripe-add-payment-method-banner'
import { api } from '@/api-client'

export const Route = createFileRoute('/_auth/_dashboard')({
  component: RouteComponent,
})

function RouteComponent() {
  const navigate = useNavigate()
  const { data: paymentMethodsData, isLoading } = api.v1.getStripePaymentMethodsUser.useQuery();

  const hasPaymentMethod = (paymentMethodsData?.paymentMethods?.length ?? 0) > 0 && !isLoading

  React.useEffect(() => {
    const introCompleted = localStorage.getItem('introFlowCompleted')
    
    if (!introCompleted && !hasPaymentMethod) {
      navigate({
        to: '/intro-flow',
      })
      return
    }
  }, [navigate, paymentMethodsData])

  return (
    <>
      <div className="min-h-screen bg-black text-white">
        <SidebarProvider>
          <AppSidebar />
          <main className='w-full'>
            <StripeAddPaymentMethodBanner />
            <section className='flex flex-row justify-between sticky top-0 bg-black z-50 pb-3'>
              <SidebarTrigger className='mt-4 ml-4' />
            </section>
            <main className='p-4 w-full'>
              <Outlet />
            </main>
          </main>
        </SidebarProvider>
      </div>
    </>
  )
}
