import { createFileRoute, Link, Outlet, useMatchRoute } from '@tanstack/react-router'
import { cn } from '@/lib/utils'

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/settings',
)({
    component: RouteComponent,
})

const tabs = [
    { label: 'General', to: '/projects/$projectId/settings/general' as const },
    { label: 'Domains', to: '/projects/$projectId/settings/domains' as const },
]

function RouteComponent() {
    const { projectId } = Route.useParams()
    const matchRoute = useMatchRoute()

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-2xl font-semibold tracking-tight">Project Settings</h2>
                <p className="text-sm text-muted-foreground mt-1">
                    Manage your project configuration.
                </p>
            </div>

            <div className="flex gap-1 border-b border-zinc-800">
                {tabs.map((tab) => {
                    const isActive = matchRoute({ to: tab.to, params: { projectId } })
                    return (
                        <Link
                            key={tab.to}
                            to={tab.to}
                            params={{ projectId }}
                            className={cn(
                                'px-4 py-2 text-sm font-medium transition-colors border-b-2 -mb-px',
                                isActive
                                    ? 'border-foreground text-foreground'
                                    : 'border-transparent text-muted-foreground hover:text-foreground'
                            )}
                        >
                            {tab.label}
                        </Link>
                    )
                })}
            </div>

            <Outlet />
        </div>
    )
}
