
import { ThemeProvider } from '@/components/theme-provider'
import { Outlet, createRootRouteWithContext } from '@tanstack/react-router'
import type { AuthContextType } from '../auth-mock'
import React from 'react'

interface RouterContext {
  auth: AuthContextType
}
export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootComponent,
})

function DevTools() {
  if (import.meta.env.DEV) {
    return <DevToolsDev />
  }
  return <DevToolsProd />
}

const DevToolsDev = React.lazy(async () => {
  const [{ TanStackDevtools }, { TanStackRouterDevtoolsPanel }, queryMod, compozerrMod] =
    await Promise.all([
      import('@tanstack/react-devtools'),
      import('@tanstack/router-devtools'),
      import('@tanstack/react-query-devtools/build/modern/production.js'),
      import('@/components/compozerr-devtools-panel'),
    ])

  function DevToolsDevInner() {
    return (
      <TanStackDevtools
        plugins={[
          { name: 'TanStack Query', render: <queryMod.ReactQueryDevtoolsPanel />, defaultOpen: true },
          { name: 'TanStack Router', render: <TanStackRouterDevtoolsPanel /> },
          { name: 'Compozerr', render: <compozerrMod.CompozerrDevtoolsPanel /> },
        ]}
      />
    )
  }
  return { default: DevToolsDevInner }
})

function DevToolsProd() {
  const [showDevtools, setShowDevtools] = React.useState(false)
  React.useEffect(() => {
    // @ts-expect-error — global toggle for production devtools
    window.toggleDevtools = () => setShowDevtools((old) => !old)
  }, [])

  if (!showDevtools) return null

  return (
    <React.Suspense fallback={null}>
      <LazyProdDevtools />
    </React.Suspense>
  )
}

const LazyProdDevtools = React.lazy(async () => {
  const [{ TanStackDevtools }, { TanStackRouterDevtoolsPanel }, queryMod] = await Promise.all([
    import('@tanstack/react-devtools'),
    import('@tanstack/router-devtools'),
    import('@tanstack/react-query-devtools/build/modern/production.js'),
  ])

  function ProdDevtools() {
    return (
      <TanStackDevtools
        plugins={[
          { name: 'TanStack Query', render: <queryMod.ReactQueryDevtoolsPanel />, defaultOpen: true },
          { name: 'TanStack Router', render: <TanStackRouterDevtoolsPanel /> },
        ]}
      />
    )
  }
  return { default: ProdDevtools }
})

function InnerRootComponent() {
  return (
    <>
      <Outlet />
      <React.Suspense fallback={null}>
        <DevTools />
      </React.Suspense>
    </>
  )
}

function RootComponent() {
  return (
    <ThemeProvider attribute="class" defaultTheme="dark" forcedTheme="dark">
      <InnerRootComponent />
    </ThemeProvider>
  )
}

export type { RouterContext }
