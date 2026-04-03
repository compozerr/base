import { createFileRoute, Outlet } from '@tanstack/react-router'

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/settings',
)({
    component: RouteComponent,
})

function RouteComponent() {
    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-2xl font-semibold tracking-tight">Project Settings</h2>
                <p className="text-sm text-muted-foreground mt-1">
                    Manage your project configuration.
                </p>
            </div>

            <Outlet />
        </div>
    )
}
