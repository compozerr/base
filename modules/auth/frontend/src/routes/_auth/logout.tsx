import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { useAuth } from '../../auth'

export const Route = createFileRoute('/_auth/logout')({
    component: RouteComponent,
})

function RouteComponent() {
    const auth = useAuth()
    auth.logout()
    return 'Logging out...'
}
