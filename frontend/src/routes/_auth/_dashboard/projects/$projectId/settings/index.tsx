import * as React from 'react'
import { createFileRoute, redirect } from '@tanstack/react-router'

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/settings/',
)({
    beforeLoad: ({ params }) => {
        return redirect({
            to: '/projects/$projectId/settings/general',
            params
        })
    }
})
