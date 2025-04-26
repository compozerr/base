import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Formatter } from '@/lib/formatter'
import { getProjectStateFromNumber, ProjectState } from '@/lib/project-state'
import { createFileRoute } from '@tanstack/react-router'
import { ExternalLink, Globe } from 'lucide-react'
import { Route as RootRoute } from './route'
import { UsageGraph, UsagePointType } from '@/components/usage-graph'

export const Route = createFileRoute('/_auth/_dashboard/projects/$projectId/')({
    component: RouteComponent,
})

// Mock data for demonstration
const mockUsageData = {
    points: {
        [UsagePointType.CPU]: Array.from({ length: 24 }, (_, i) => ({
            timestamp: new Date(Date.now() - (23 - i) * 3600000),
            value: Math.random() * 100,
        })),
        [UsagePointType.Ram]: Array.from({ length: 24 }, (_, i) => ({
            timestamp: new Date(Date.now() - (23 - i) * 3600000),
            value: Math.random() * 1024,
        })),
        [UsagePointType.DiskRead]: Array.from({ length: 24 }, (_, i) => ({
            timestamp: new Date(Date.now() - (23 - i) * 3600000),
            value: Math.random() * 50,
        })),
        [UsagePointType.DiskWrite]: Array.from({ length: 24 }, (_, i) => ({
            timestamp: new Date(Date.now() - (23 - i) * 3600000),
            value: Math.random() * 30,
        })),
        [UsagePointType.NetworkIn]: Array.from({ length: 24 }, (_, i) => ({
            timestamp: new Date(Date.now() - (23 - i) * 3600000),
            value: Math.random() * 500,
        })),
        [UsagePointType.NetworkOut]: Array.from({ length: 24 }, (_, i) => ({
            timestamp: new Date(Date.now() - (23 - i) * 3600000),
            value: Math.random() * 300,
        })),
    },
    usageSpan: "day",
}


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
                <Card className="h-fit">
                    <CardContent className="pt-6">
                        <div className="space-y-2">
                            <h2 className="text-xl font-semibold">Project Details</h2>
                            <div className="grid grid-cols-1 gap-4 mt-4">
                                <div>
                                    <h3 className="text-sm font-medium text-muted-foreground">Project ID</h3>
                                    <p className="text-sm font-mono">{project.id}</p>
                                </div>
                                <div>
                                    <h3 className="text-sm font-medium text-muted-foreground">Repository</h3>
                                    <p className="text-sm ">
                                        <a
                                            className="flex flex-row items-center gap-1"
                                            href={`https://github.com/${project.repoName}`}
                                            target="_blank"
                                            rel="noreferrer"
                                        >
                                            {project.repoName} <ExternalLink className="h-3.5 w-3.5" />
                                        </a>
                                    </p>
                                </div>
                                <div>
                                    <h3 className="text-sm font-medium text-muted-foreground">Start Date</h3>
                                    <p className="text-sm">{Formatter.fromDate(project.startDate)}</p>
                                </div>
                                <div>
                                    <h3 className="text-sm font-medium text-muted-foreground">Status</h3>
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

                <UsageGraph usageData={mockUsageData} />


            </div>
        </div>
    )
}
