
import { ThemeProvider } from '@/components/theme-provider'
import { Outlet, createRootRouteWithContext, useRouterState } from '@tanstack/react-router'
import { TanStackRouterDevtools } from '@tanstack/router-devtools'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import type { AuthContextType } from '../auth-mock'
import React from 'react'
import { LoaderIcon } from 'lucide-react'

interface RouterContext {
  auth: AuthContextType
}
export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootComponent,
})

const ReactQueryDevtoolsProduction = React.lazy(() =>
  import('@tanstack/react-query-devtools/build/modern/production.js').then(
    (d) => ({
      default: d.ReactQueryDevtools,
    }),
  ),
)

function InnerRootComponent() {
  const [showDevtools, setShowDevtools] = React.useState(false);
  React.useEffect(() => {
    // @ts-expect-error
    window.toggleDevtools = () => setShowDevtools((old) => !old)
  }, []);
  return (
    <>
      <Outlet />
      <ReactQueryDevtools initialIsOpen />
      {showDevtools && (
        <>
          <TanStackRouterDevtools position="bottom-left" />
          <React.Suspense fallback={null}>
            <ReactQueryDevtoolsProduction />
          </React.Suspense>
        </>
      )}
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
