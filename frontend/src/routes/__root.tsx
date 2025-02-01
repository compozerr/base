import * as React from 'react'

import { Link, Outlet, createRootRouteWithContext } from '@tanstack/react-router'
import { TanStackRouterDevtools } from '@tanstack/router-devtools'
import type { AuthContextType } from '../auth-mock'
import { ThemeProvider } from '@/components/theme-provider'
import Navbar from '@/components/navbar'

interface RouterContext {
  auth: AuthContextType
}
export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootComponent,
})


function InnerRootComponent() {
  return (
    <>
      <Navbar />

      <Outlet />
      <TanStackRouterDevtools position="bottom-right" />
    </>
  )
}

function RootComponent() {
  return (
    <ThemeProvider attribute="class" defaultTheme="system" enableSystem>
      <InnerRootComponent />
    </ThemeProvider>
  )
}

export type { RouterContext }