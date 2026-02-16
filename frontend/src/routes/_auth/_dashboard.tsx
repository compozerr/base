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
import { AnimatePresence, motion } from 'framer-motion'
import { ChevronRight } from 'lucide-react'
import { useMemo } from 'react'

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

const routeLabelMap: Record<string, string> = {
  projects: 'Projects',
  settings: 'Settings',
  deployments: 'Deployments',
  'intro-flow': 'Setup',
}

function Breadcrumbs() {
  const routerState = useRouterState()
  const pathname = routerState.location.pathname

  const segments = useMemo(() => {
    const parts = pathname.split('/').filter(Boolean)
    const crumbs: { label: string; path: string }[] = []

    for (let i = 0; i < parts.length; i++) {
      const segment = parts[i]
      if (!segment) continue
      const path = '/' + parts.slice(0, i + 1).join('/')
      const label = routeLabelMap[segment]
      if (label) {
        crumbs.push({ label, path })
      }
    }

    return crumbs
  }, [pathname])

  if (segments.length === 0) return null

  return (
    <nav className="flex items-center gap-1.5 text-sm" aria-label="Breadcrumb">
      {segments.map((segment, idx) => (
        <span key={segment.path} className="flex items-center gap-1.5">
          {idx > 0 && (
            <ChevronRight className="h-3 w-3 text-white/30" />
          )}
          <span
            className={
              idx === segments.length - 1
                ? 'font-medium text-white'
                : 'text-white/50'
            }
          >
            {segment.label}
          </span>
        </span>
      ))}
    </nav>
  )
}

function RouteComponent() {
  const routerState = useRouterState()
  const pathname = routerState.location.pathname

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
              <div className="hidden md:block">
                <Breadcrumbs />
              </div>
            </div>
            {/* Gradient bottom border */}
            <div className="h-px bg-gradient-to-r from-transparent via-white/[0.08] to-transparent" />
          </header>

          {/* Content area */}
          <div className="mx-auto max-w-7xl p-4 pb-20 md:pb-4">
            <AnimatePresence mode="wait">
              <motion.div
                key={pathname}
                initial={{ opacity: 0, y: 6 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0 }}
                transition={{ duration: 0.15, ease: 'easeOut' }}
              >
                <Outlet />
              </motion.div>
            </AnimatePresence>
          </div>
        </main>
      </SidebarProvider>

      {/* Mobile bottom nav */}
      <MobileBottomNav />
    </div>
  )
}
