import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Formatter } from '@/lib/formatter'
import { getProjectStateFromNumber, ProjectState } from '@/lib/project-state'
import { createFileRoute } from '@tanstack/react-router'
import { ExternalLink, Globe } from 'lucide-react'
import { Route as RootRoute } from './route'

export const Route = createFileRoute('/_auth/_dashboard/projects/$projectId/')({
    component: RouteComponent,
})

function RouteComponent() {
    const project = RootRoute.useLoaderData()

    const getStateColor = (state: ProjectState) => {
        switch (state) {
            case ProjectState.Running:
                return 'bg-green-500'
            case ProjectState.Building:
                return 'bg-gray-500'
            case ProjectState.Stopped:
                return 'bg-red-500'
            default:
                return 'bg-gray-500'
        }
    }

    if (!project) {
        return (
            <div className="text-center py-10">
                <p>No project data available</p>
            </div>
        )
    }

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                    <h1 className="text-3xl font-bold">{project.name}</h1>
                    <Badge
                        className={getStateColor(getProjectStateFromNumber(project.state))}
                    >
                        {getProjectStateFromNumber(project.state)}
                    </Badge>
                </div>
                {project.primaryDomain ? (
                    <div className="flex items-center gap-2 text-sm">
                        <Globe className="h-4 w-4" />
                        <a
                            href={`https://${project.primaryDomain}`}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="hover:underline"
                        >
                            {project.primaryDomain}
                        </a>
                    </div>
                ) : null}
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <Card>
                    <CardContent className="pt-6">
                        <div className="space-y-2">
                            <h2 className="text-xl font-semibold">Project Details</h2>
                            <div className="grid grid-cols-1 gap-4 mt-4">
                                <div>
                                    <h3 className="text-sm font-medium text-muted-foreground">
                                        Project ID
                                    </h3>
                                    <p className="text-sm font-mono">{project.id}</p>
                                </div>
                                <div>
                                    <h3 className="text-sm font-medium text-muted-foreground">
                                        Repository
                                    </h3>
                                    <p className="text-sm ">
                                        <a
                                            className="flex flex-row items-center"
                                            href={`https://github.com/${project.repoName}`}
                                            target="_blank"
                                        >
                                            {project.repoName} <ExternalLink height={15} />
                                        </a>{' '}
                                    </p>
                                </div>
                                <div>
                                    <h3 className="text-sm font-medium text-muted-foreground">
                                        Start Date
                                    </h3>
                                    <p className="text-sm">
                                        {Formatter.fromDate(project.startDate)}
                                    </p>
                                </div>
                            </div>
                        </div>
                    </CardContent>
                </Card>

                <Card>
                    <CardContent className="pt-6">
                        <div className="space-y-2">
                            <h2 className="text-xl font-semibold">Resource Usage</h2>
                            <div className="grid grid-cols-1 gap-4 mt-4">
                                <div>
                                    <h3 className="text-sm font-medium text-muted-foreground">
                                        vCPU Hours
                                    </h3>
                                    <p className="text-2xl font-bold">
                                        {project.vCpuHours?.toFixed(2)}
                                    </p>
                                    <p className="text-xs text-muted-foreground">
                                        Total usage since project creation
                                    </p>
                                </div>
                                <div>
                                    <h3 className="text-sm font-medium text-muted-foreground">
                                        Status
                                    </h3>
                                    <p className="text-sm flex items-center gap-2">
                                        <span
                                            className={`h-2 w-2 rounded-full ${getStateColor(getProjectStateFromNumber(project.state))}`}
                                        ></span>
                                        {getProjectStateFromNumber(project.state)}
                                    </p>
                                </div>
                            </div>
                        </div>
                    </CardContent>
                </Card>

                <Card>
                    <CardContent className="pt-6">
                        <div className="space-y-2">
                            <h2 className="text-xl font-semibold">Recent Activity</h2>
                            <div className="space-y-4 mt-4">
                                <div className="flex items-center justify-between">
                                    <p className="text-sm">Last deployment</p>
                                    <p className="text-sm text-muted-foreground">2 hours ago</p>
                                </div>
                                <div className="flex items-center justify-between">
                                    <p className="text-sm">Last commit</p>
                                    <p className="text-sm text-muted-foreground">Yesterday</p>
                                </div>
                                <div className="flex items-center justify-between">
                                    <p className="text-sm">Last settings change</p>
                                    <p className="text-sm text-muted-foreground">3 days ago</p>
                                </div>
                            </div>
                        </div>
                    </CardContent>
                </Card>

                <Card>
                    <CardContent className="pt-6">
                        <div className="space-y-2">
                            <h2 className="text-xl font-semibold">Domains</h2>
                            <div className="space-y-4 mt-4">
                                {project.domains && project.domains?.length > 0 ? (
                                    project.domains.map((d) => (
                                        <div className="flex items-center justify-between">
                                            <div>
                                                <p className="text-sm font-medium">{d}</p>
                                            </div>
                                            {project.primaryDomain === d ? (
                                                <Badge
                                                    variant="outline"
                                                    className="text-green-500 border-green-500"
                                                >
                                                    Primary
                                                </Badge>
                                            ) : null}
                                        </div>
                                    ))
                                ) : (
                                    <span>No domains</span>
                                )}
                            </div>
                        </div>
                    </CardContent>
                </Card>
            </div>
        </div>
    )
}
