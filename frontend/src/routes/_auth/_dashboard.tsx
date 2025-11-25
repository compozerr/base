import {
  createFileRoute,
  Outlet,
  redirect
} from '@tanstack/react-router'

import { AppSidebar } from '@/components/app-sidebar'
import { SidebarProvider, SidebarTrigger } from '@/components/ui/sidebar'
import StripeAddPaymentMethodBanner from '@repo/stripe/stripe-add-payment-method-banner'
import { api } from '@/api-client'
import { LocalStorage } from '@/lib/storage'
import { tryCatchify } from '@/lib/try-catchifier'

export const Route = createFileRoute('/_auth/_dashboard')({
  component: RouteComponent,
  beforeLoad: async ({ location }) => {
    if (location.pathname === '/intro-flow') return;

    const introCompleted = LocalStorage.getItem('introFlowCompleted')

    const { data, error } = await tryCatchify(() => api.v1.getStripePaymentMethodsUser.fetchQuery({
      parameters: undefined,
      staleTime: 5 * 60 * 1000, // 5 minutes
    }));

    if (error) {
      console.error('Error fetching payment methods:', error);
      return;
    }

    const hasPaymentMethod = (data.paymentMethods?.length ?? 0) > 0

    if (!introCompleted && !hasPaymentMethod) {
      throw redirect({
        to: '/intro-flow',
        search: {
          redirect: location.pathname
        }
      })
    }
  }
})

function RouteComponent() {
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
