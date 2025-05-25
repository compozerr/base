import { api } from '@/api-client';
import { CopyButton } from '@/components/copy-button';
import { CopyText } from '@/components/copy-text';
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { DeploymentStatus, getDeploymentStatusFromNumber } from '@/lib/deployment-status';
import { getStatusDot } from '@/lib/deployment-status-component';
import { Formatter } from '@/lib/formatter';
import { cn } from '@/lib/utils';
import { createFileRoute, useRouter } from '@tanstack/react-router';
import { ArrowLeft, Calendar, Clock, ExternalLink, GitBranch, GitCommit } from "lucide-react";

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/deployments/$deploymentId/',
)({
    component: RouteComponent,
    loader: async (ctx) => {
        return api.v1.getProjectsProjectIdDeploymentsDeploymentId.fetchQuery({
            parameters: {
                path: {
                    projectId: ctx.params.projectId,
                    deploymentId: ctx.params.deploymentId
                }
            }
        });
    },
})

function RouteComponent() {
    const initialData = Route.useLoaderData();
    const { projectId, deploymentId } = Route.useParams();

    const { data: deployment } = api.v1.getProjectsProjectIdDeploymentsDeploymentId.useQuery(
        { path: { projectId, deploymentId } },
        {
            initialData,
            refetchInterval: (ctx) => getDeploymentStatusFromNumber(ctx.state.data?.status) === DeploymentStatus.Deploying ? 10000 : false
        }
    );

    const { data: logs } = api.v1.getProjectsProjectIdDeploymentsDeploymentIdLogs.useQuery({ path: { projectId, deploymentId } }, {
        enabled: !!deploymentId,
        refetchInterval: () => getDeploymentStatusFromNumber(deployment?.status) === DeploymentStatus.Deploying ? 2000 : false,
    });

    const router = useRouter()

    const goBack = () => {
        router.navigate({ to: `/projects/${projectId}/deployments` })
    }

    const getLogTextColor = (log: string) => {
        if (log.includes("ERROR")) return "text-red-500";
        if (log.includes("WARNING")) return "text-yellow-500";
        if (log.includes("INFO")) return "text-gray-400";
        if (log.includes("SUCCESS")) return "text-green-500";
        return "text-gray-500";
    }

    if (!deployment) {
        return (
            <div className="space-y-6">
                <div className="flex items-center gap-2">
                    <Button variant="ghost" size="icon" onClick={goBack}>
                        <ArrowLeft className="h-4 w-4" />
                    </Button>
                    <h2 className="text-2xl font-bold">Deployment not found</h2>
                </div>
                <p>The deployment you're looking for doesn't exist or you don't have access to it.</p>
                <Button onClick={goBack}>Back to Deployments</Button>
            </div>
        )
    }

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                    <Button variant="ghost" size="icon" onClick={goBack}>
                        <ArrowLeft className="h-4 w-4" />
                    </Button>
                    <h2 className="text-2xl font-bold">Deployment {deployment.id?.substring(0, 8)}</h2>
                    {deployment.isCurrent && (
                        <Badge
                            variant="outline"
                            className="ml-2 text-xs rounded-full px-2 py-0 h-5 bg-primary/10 border-primary/20"
                        >
                            Current
                        </Badge>
                    )}
                </div>
                <div className="flex items-center gap-2">
                    {deployment.url &&
                        <Button variant="outline" className="gap-2" asChild>
                            <a href={"https://" + deployment.url} target="_blank" rel="noopener noreferrer">
                                <ExternalLink className="h-4 w-4" />
                                Visit
                            </a>
                        </Button>
                    }
                </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <Card>
                    <CardHeader>
                        <CardTitle>Deployment Information</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="flex justify-between items-center">
                            <div className="text-sm text-muted-foreground">Status</div>
                            <div className="flex items-center font-medium">
                                {getStatusDot(getDeploymentStatusFromNumber(deployment.status))}
                                {getDeploymentStatusFromNumber(deployment.status)}
                            </div>
                        </div>
                        <div className="flex justify-between items-center">
                            <div className="text-sm text-muted-foreground">Environment</div>
                            <div className="font-medium">{deployment.environment}</div>
                        </div>
                        <div className="flex justify-between items-center">
                            <div className="text-sm text-muted-foreground">Created</div>
                            <div className="flex items-center gap-2 font-medium">
                                <Calendar className="h-4 w-4 text-muted-foreground" />
                                {Formatter.fromDate(deployment.createdAt, "long")}
                            </div>
                        </div>
                        <div className="flex justify-between items-center">
                            <div className="text-sm text-muted-foreground">Duration</div>
                            <div className="flex items-center gap-2 font-medium">
                                <Clock className="h-4 w-4 text-muted-foreground" />
                                {Formatter.fromDuration(deployment.buildDuration!)}
                            </div>
                        </div>
                        <div className="flex justify-between items-center">
                            <div className="text-sm text-muted-foreground">URL</div>
                            <div className="flex items-center gap-2 font-medium">
                                <a
                                    href={"https://" + deployment.url!}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="text-blue-500 hover:underline"
                                >
                                    {deployment.url}
                                </a>
                                <CopyButton value={deployment.url!} />
                            </div>
                        </div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>Git Information</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="flex justify-between items-center">
                            <div className="text-sm text-muted-foreground">Branch</div>
                            <div className="flex items-center gap-2 font-medium">
                                <GitBranch className="h-4 w-4 text-muted-foreground" />
                                {deployment.branch}
                            </div>
                        </div>
                        <div className="flex justify-between items-center">
                            <div className="text-sm text-muted-foreground">Commit</div>
                            <div className="flex items-center gap-2 font-medium">
                                <GitCommit className="h-4 w-4 text-muted-foreground" />
                                <CopyText value={deployment.commitHash!} className='font-mono' >
                                    <span className="font-mono">{deployment.commitHash?.substring(0, 6)}</span>
                                </CopyText>
                            </div>
                        </div>
                        <div className="flex justify-between items-center">
                            <div className="text-sm text-muted-foreground">Message</div>
                            <div className="font-medium truncate pl-3" title={deployment.commitMessage ?? ""}>
                                {deployment.commitMessage}
                            </div>
                        </div>
                        <div className="flex justify-between items-center">
                            <div className="text-sm text-muted-foreground">Author</div>
                            <div className="font-medium">{deployment.author}</div>
                        </div>
                    </CardContent>
                </Card>
            </div>

            <Card>
                <CardHeader className="pb-2">
                    <CardTitle className="text-lg flex justify-between">
                        <span>Build Logs</span>
                        <CopyButton value={logs ?? ""}>
                            <span className='ml-4 text-sm'>Copy</span>
                        </CopyButton>
                    </CardTitle>
                    <CardDescription>Logs from the build process</CardDescription>
                </CardHeader>
                <CardContent>
                    <div className="bg-black text-green-400 font-mono text-sm p-4 rounded-md h-[400px] overflow-y-auto">
                        <div className="whitespace-pre-wrap mb-1">
                            {logs?.split("\n").map((log, index) => (
                                <div key={index} className={cn("whitespace-pre-wrap mb-1", getLogTextColor(log))}>
                                    {log}
                                </div>
                            ))}
                        </div>
                    </div>
                </CardContent>
            </Card>
        </div>
    )
}
