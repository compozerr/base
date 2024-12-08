import * as React from 'react'

import { Link, Outlet, createRootRouteWithContext } from '@tanstack/react-router'
import { TanStackRouterDevtools } from '@tanstack/router-devtools'
import type { AuthContextType } from '../auth-mock'

interface RouterContext {
  auth: AuthContextType
}
export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootComponent,
})

function RootComponent() {
  return (
    <>
      <div className="p-2 flex gap-2 text-lg">
        <Link
          to="/"
          activeProps={{
            className: 'font-bold',
          }}
          activeOptions={{ exact: true }}
        >
          Home
        </Link>{' '}
        <Link
          to="/about"
          activeProps={{
            className: 'font-bold',
          }}
        >
          About
        </Link>
        <Link
          to="/using-module-component"
          activeProps={{
            className: 'font-bold',
          }}
        >
          Module
        </Link>
        <Link
          to="/example"
          activeProps={{
            className: 'font-bold',
          }}
        >
          Example
        </Link>
      </div>
      <hr />

      <div className='p-2'>
        <Outlet />
      </div>
      <TanStackRouterDevtools position="bottom-right" />
    </>
  )
}

export type { RouterContext }