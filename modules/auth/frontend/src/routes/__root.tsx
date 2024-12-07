import * as React from 'react'

import { createRootRoute, createRootRouteWithContext, Outlet } from "@tanstack/react-router";
import { AuthState } from '../auth/auth-state';

interface RouterContext {
    auth: AuthState
}

export const Route = createRootRouteWithContext<RouterContext>()({
    component: () => <Outlet />,
    beforeLoad: async () => {

    }
})