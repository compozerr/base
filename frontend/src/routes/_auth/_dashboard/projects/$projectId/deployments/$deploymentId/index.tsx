import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/deployments/$deploymentId/',
)({
    component: RouteComponent,
})

function RouteComponent() {
    const params = Route.useParams();
    return `Hello /_auth/_dashboard/projects/${params.projectId}/deployments/${params.deploymentId}/!`
}
