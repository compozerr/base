import {
  createFileRoute,
  Outlet
} from '@tanstack/react-router'

import { AppSidebar } from '@/components/app-sidebar'
import { SidebarProvider, SidebarTrigger } from '@/components/ui/sidebar'
import StripeAddPaymentMethodBanner from '@repo/stripe/stripe-add-payment-method-banner'

export const Route = createFileRoute('/_auth/_dashboard')({
  component: RouteComponent,
})

function RouteComponent() {

  return (
    <>
      <div className="min-h-screen bg-black text-white">
        <SidebarProvider>
          <AppSidebar />
          <main className='w-full'>
            <StripeAddPaymentMethodBanner />
            <section className='flex flex-row justify-between sticky top-0 bg-black z-10 pb-3'>
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
