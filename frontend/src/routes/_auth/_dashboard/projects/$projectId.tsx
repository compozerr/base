import * as React from 'react'
import { createFileRoute, Link, Outlet, useLocation } from '@tanstack/react-router'
import { cn } from '@/lib/utils'
import { api } from '@/api-client'

export const Route = createFileRoute('/_auth/_dashboard/projects/$projectId')({
    component: RouteComponent,
    loader: (ctx) => api.v1.getProjectsProjectId.fetchQuery({ parameters: { path: { projectId: ctx.params.projectId } } }),
})

function RouteComponent() {
    const { pathname } = useLocation()
    const { projectId } = Route.useParams();
    const project = Route.useLoaderData();

    const tabs = [
        { name: "Overview", href: `/projects/${projectId}` },
        { name: "Deployments", href: `/projects/${projectId}/deployments` },
        { name: "Settings", href: `/projects/${projectId}/settings` },
    ]

    return (
        <div className="flex flex-col min-h-screen">
            <div className="border-b">
                <div className="container flex h-16 items-center px-4">
                    <h1 className="text-lg font-semibold mr-8">{project.name}</h1>
                    <nav className="flex items-center space-x-4 lg:space-x-6">
                        {tabs.map((tab) => (
                            <Link
                                key={tab.name}
                                to={tab.href}
                                className={cn(
                                    "text-sm font-medium transition-colors hover:text-primary",
                                    pathname === tab.href ? "border-b-2 border-primary" : "text-muted-foreground",
                                )}
                            >
                                {tab.name}
                            </Link>
                        ))}
                    </nav>
                </div>
            </div>
            <div className="container py-6 px-4">
                <Outlet />
            </div>
        </div>
    )
}
