import {
  createFileRoute,
  Outlet,
  redirect,
  useRouterState,
} from '@tanstack/react-router'

import { AppSidebar } from '@/components/app-sidebar'
import { MobileBottomNav } from '@/components/mobile-bottom-nav'
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
    <div className="min-h-screen bg-black text-white">
      <SidebarProvider>
        {/* Sidebar: hidden on mobile via the shadcn Sidebar component (md:block) */}
        <AppSidebar />

        <main className="w-full">
          <StripeAddPaymentMethodBanner />

          {/* Header */}
          <header className="sticky top-0 z-50 bg-black/80 backdrop-blur-md">
            <div className="flex items-center gap-3 px-4 py-3">
              {/* Sidebar trigger: desktop only */}
              <div className="hidden md:block">
                <SidebarTrigger />
              </div>
              {/* Slot for route-level header content (breadcrumbs, tabs, etc.) */}
              <div id="dashboard-header-slot" />
            </div>
            {/* Gradient bottom border */}
            <div className="h-px bg-gradient-to-r from-transparent via-white/[0.08] to-transparent" />
          </header>

          {/* Content area */}
          <div className="mx-auto max-w-7xl p-4 pb-20 md:pb-4">
            <Outlet />
          </div>
        </main>
      </SidebarProvider>

      {/* Mobile bottom nav */}
      <MobileBottomNav />
    </div>
  )
}
