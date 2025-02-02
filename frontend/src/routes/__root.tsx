
import { ThemeProvider } from '@/components/theme-provider'
import { Outlet, createRootRouteWithContext } from '@tanstack/react-router'
import { TanStackRouterDevtools } from '@tanstack/router-devtools'
import type { AuthContextType } from '../auth-mock'

interface RouterContext {
  auth: AuthContextType
}
export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootComponent,
})


function InnerRootComponent() {
  return (
    <>
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
