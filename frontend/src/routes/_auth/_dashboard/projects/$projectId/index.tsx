import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Formatter } from '@/lib/formatter'
import { getProjectStateFromNumber as getProjectStateFromStateString, ProjectState } from '@/lib/project-state'
import { createFileRoute, getRouteApi } from '@tanstack/react-router'
import { ExternalLink, Globe } from 'lucide-react'
import { UsageGraph } from '@/components/usage-graph'
import StartStopProjectButton from '@/components/project/project-startstop-button'
import { api } from '@/api-client'

export const Route = createFileRoute('/_auth/_dashboard/projects/$projectId/')({
    component: RouteComponent,
})

function RouteComponent() {
    const { projectId } = getRouteApi("/_auth/_dashboard/projects/$projectId").useLoaderData();
    const { data: project } = api.v1.getProjectsProjectId.useQuery({ path: { projectId } }, {
        refetchInterval: (project) => project.state.data?.state === ProjectState.Starting ? 2000 : false,
    })

    const getStateColor = (state: ProjectState) => {
        switch (state) {
            case ProjectState.Running:
                return 'bg-green-500'
            case ProjectState.Starting:
                return 'bg-yellow-500'
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
                        className={getStateColor(getProjectStateFromStateString(project.state))}
                    >
                        {getProjectStateFromStateString(project.state)}
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
                                    <h3 className="text-sm font-medium text-muted-foreground">Status</h3>
                                    <p className="text-sm flex items-center gap-2">
                                        <span
                                            className={`h-2 w-2 rounded-full ${getStateColor(getProjectStateFromStateString(project.state))}`}
                                        ></span>
                                        {getProjectStateFromStateString(project.state)}

                                        {project.id && project.state && <StartStopProjectButton projectId={project.id} state={project.state} variant="ghost" />}
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
                            <div className="space-y-4 mt-4 relative max-h-[220px]">
                                <div className="overflow-y-auto max-h-[220px] min-h-[100px] pr-1">
                                    {project.domains && project.domains?.length > 0 ? (
                                        project.domains.sort((d)=>d === project.primaryDomain ? -1 : 1).map((d) => (
                                            <div key={d} className="flex items-center justify-between mb-4">
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
                                <div className="absolute bottom-0 left-0 right-0 h-8 bg-gradient-to-t from-card to-transparent pointer-events-none" />
                            </div>
                        </div>
                    </CardContent>
                </Card>

                <UsageGraph projectId={project.id!} />
            </div>
        </div>
    )
}
