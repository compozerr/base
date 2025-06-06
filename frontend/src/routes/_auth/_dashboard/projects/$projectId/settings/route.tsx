import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { createFileRoute, Link, Outlet, useParams, useRouterState } from '@tanstack/react-router'
import { useMemo } from 'react';

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/settings',
)({
    component: RouteComponent,
})

function RouteComponent() {
    const params = Route.useParams();

    const { location: { pathname } } = useRouterState();

    const tabRoute = useMemo(() => pathname.split('/').pop(), [pathname]);

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-3xl font-bold tracking-tight">Project Settings</h2>
                <p className="text-muted-foreground">
                    Manage your project settings and configuration.
                </p>
            </div>

            <Tabs className="w-full" value={tabRoute}>
                <TabsList className="grid w-full grid-cols-3 lg:w-auto">
                    <TabsTrigger value="general" asChild>
                        <Link to="/projects/$projectId/settings/general" params={params} viewTransition>General</Link>
                    </TabsTrigger>
                    <TabsTrigger value="environment" asChild>
                        <Link to="/projects/$projectId/settings/environment" params={params} viewTransition>Environment Variables</Link>
                    </TabsTrigger>
                    <TabsTrigger value="domains" asChild>
                        <Link to="/projects/$projectId/settings/domains" params={params} viewTransition>Domains</Link>
                    </TabsTrigger>
                </TabsList>

                <Outlet />
            </Tabs>
        </div>
    )
}
