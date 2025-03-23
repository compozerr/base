import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/_auth/_dashboard/projects/$projectId/')({
    component: RouteComponent,
    beforeLoad: async ({ params }) => {
        console.log({ params });
    }
})

function RouteComponent() {
    return 'Hello /_auth/_dashboard/projects/$projectId/!'
}
