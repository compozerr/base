import { api } from '@/api-client';
import { cn } from '@/lib/utils';
import { createFileRoute, Link, Outlet, useRouterState } from '@tanstack/react-router'
import { Settings, KeyRound, Globe, LucideIcon } from 'lucide-react';
import { useMemo } from 'react';

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/settings',
)({
    component: RouteComponent,
})

function SettingsTab({ id, label, icon: Icon, active, params }: {
    id: 'general' | 'environment' | 'domains'
    label: string
    icon: LucideIcon
    active: boolean
    params: { projectId: string }
}) {
    const routes = {
        general: '/projects/$projectId/settings/general',
        environment: '/projects/$projectId/settings/environment',
        domains: '/projects/$projectId/settings/domains',
    } as const;

    return (
        <Link
            to={routes[id]}
            params={params}
            viewTransition
            className={cn(
                'relative flex items-center gap-2 px-4 py-2.5 text-sm font-medium transition-colors whitespace-nowrap border-b-2',
                active
                    ? 'border-primary text-foreground'
                    : 'border-transparent text-muted-foreground hover:text-foreground hover:border-zinc-700',
            )}
        >
            <Icon className="h-4 w-4" />
            {label}
        </Link>
    )
}

function RouteComponent() {
    const params = Route.useParams();

    const { location: { pathname } } = useRouterState();

    const { data: project } = api.v1.getProjectsProjectId.useQuery({ path: { projectId: params.projectId } });

    const tabRoute = useMemo(() => pathname.split('/').pop(), [pathname]);

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-2xl font-semibold tracking-tight">Project Settings</h2>
                <p className="text-sm text-muted-foreground mt-1">
                    Manage your project configuration, environment, and domains.
                </p>
            </div>

            <div className="border-b border-zinc-800">
                <nav className="flex items-center gap-1 overflow-x-auto scrollbar-none -mb-px">
                    <SettingsTab id="general" label="General" icon={Settings} active={tabRoute === 'general'} params={params} />
                    {project?.type !== "N8n" && (
                        <SettingsTab id="environment" label="Environment" icon={KeyRound} active={tabRoute === 'environment'} params={params} />
                    )}
                    <SettingsTab id="domains" label="Domains" icon={Globe} active={tabRoute === 'domains'} params={params} />
                </nav>
            </div>

            <Outlet />
        </div>
    )
}
