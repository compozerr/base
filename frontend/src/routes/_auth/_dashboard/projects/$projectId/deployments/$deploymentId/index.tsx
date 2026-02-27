import { api, apiBaseUrl } from '@/api-client';
import { CopyButton } from '@/components/copy-button';
import { CopyText } from '@/components/copy-text';
import LoadingButton from '@/components/loading-button';
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { useDuration } from '@/hooks/use-duration';
import { DeploymentStatus } from '@/lib/deployment-status';
import { Formatter } from '@/lib/formatter';
import { cn } from '@/lib/utils';
import { useMutation } from '@tanstack/react-query';
import { createFileRoute, useRouter } from '@tanstack/react-router';
import { motion } from 'framer-motion';
import {
    ArrowLeft,
    Calendar,
    ChevronRight,
    CircleAlert,
    CircleCheck,
    CircleDot,
    Clock,
    ExternalLink,
    GitBranch,
    GitCommit,
    Info,
    TriangleAlert,
    User,
} from "lucide-react";
import { useRef, useEffect } from 'react';

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

function getStatusBadgeClasses(status: DeploymentStatus) {
    switch (status) {
        case DeploymentStatus.Completed:
            return "bg-emerald-500/10 text-emerald-500 border-emerald-500/20";
        case DeploymentStatus.Deploying:
            return "bg-amber-500/10 text-amber-500 border-amber-500/20";
        case DeploymentStatus.Queued:
            return "bg-white/[0.06] text-white border-white/20";
        case DeploymentStatus.Failed:
            return "bg-red-500/10 text-red-500 border-red-500/20";
        case DeploymentStatus.Cancelled:
            return "bg-zinc-500/10 text-zinc-400 border-zinc-500/20";
        default:
            return "bg-zinc-500/10 text-zinc-400 border-zinc-500/20";
    }
}

function getStatusDotColor(status: DeploymentStatus) {
    switch (status) {
        case DeploymentStatus.Completed:
            return "bg-emerald-500";
        case DeploymentStatus.Deploying:
            return "bg-amber-500";
        case DeploymentStatus.Queued:
            return "bg-white";
        case DeploymentStatus.Failed:
            return "bg-red-500";
        case DeploymentStatus.Cancelled:
            return "bg-zinc-500";
        default:
            return "bg-zinc-500";
    }
}

function RouteComponent() {
    const initialData = Route.useLoaderData();
    const { projectId, deploymentId } = Route.useParams();

    const { data: deployment, refetch } = api.v1.getProjectsProjectIdDeploymentsDeploymentId.useQuery(
        { path: { projectId, deploymentId } },
        {
            initialData,
            refetchInterval: (ctx) => [DeploymentStatus.Deploying, DeploymentStatus.Queued].includes(ctx.state.data?.status as DeploymentStatus) ? 10000 : false
        }
    );

    const { data: logs } = api.v1.getProjectsProjectIdDeploymentsDeploymentIdLogs.useQuery({ path: { projectId, deploymentId } }, {
        enabled: !!deploymentId,
        refetchInterval: () => deployment?.status as DeploymentStatus === DeploymentStatus.Deploying ? 2000 : false,
    });

    const router = useRouter()

    const goBack = () => {
        router.navigate({ to: `/projects/${projectId}/deployments` })
    }

    const logContainerRef = useRef<HTMLDivElement>(null);

    // Auto-scroll logs when deploying
    useEffect(() => {
        if (deployment?.status === DeploymentStatus.Deploying && logContainerRef.current) {
            logContainerRef.current.scrollTop = logContainerRef.current.scrollHeight;
        }
    }, [logs, deployment?.status]);

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

    const status = deployment.status as DeploymentStatus;
    const isDeploying = status === DeploymentStatus.Deploying;
    const isQueued = status === DeploymentStatus.Queued;
    const buildDuration = useDuration(deployment.buildDuration!, isDeploying);

    const { mutateAsync: redeployAsync, isPending: isRedeploying } = api.v1.postProjectsProjectIdDeploymentsDeploymentIdRedeploy.useMutation();

    const handleRedeployAsync = async () => {
        await redeployAsync({
            path: {
                deploymentId: deploymentId,
                projectId: deploymentId
            }
        });
        goBack();
    };

    const { mutateAsync: cancelDeploymentAsync, isPending: isCancelling } = useMutation({
        mutationFn: async () => {
            const response = await fetch(`${apiBaseUrl}/v1/projects/${projectId}/deployments/${deploymentId}/cancel`, {
                method: 'POST',
                credentials: 'include',
            });
            if (!response.ok) throw new Error('Failed to cancel deployment');
            return response.json();
        }
    });

    const handleCancelAsync = async () => {
        await cancelDeploymentAsync();
        refetch();
    };

    return (
        <div className="space-y-6">
            {/* Breadcrumb + back */}
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <button onClick={goBack} className="hover:text-foreground transition-colors">
                    Deployments
                </button>
                <ChevronRight className="h-3.5 w-3.5" />
                <span className="text-foreground font-mono">{deployment.id?.substring(0, 8)}</span>
            </div>

            {/* Header */}
            <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                <div className="flex items-center gap-3">
                    <Button variant="ghost" size="icon" className="h-8 w-8 rounded-full" onClick={goBack}>
                        <ArrowLeft className="h-4 w-4" />
                    </Button>
                    <h2 className="text-2xl font-bold font-mono">{deployment.id?.substring(0, 8)}</h2>
                    <Badge
                        variant="outline"
                        className={cn("rounded-full text-xs px-2.5 py-0.5", getStatusBadgeClasses(status))}
                    >
                        <span className="relative flex h-2 w-2 mr-1.5">
                            <span className={cn("block h-2 w-2 rounded-full", getStatusDotColor(status))} />
                            {isDeploying && (
                                <span className={cn("absolute inset-0 h-2 w-2 rounded-full animate-ping opacity-40", getStatusDotColor(status))} />
                            )}
                        </span>
                        {deployment.status}
                    </Badge>
                    {deployment.isCurrent && (
                        <Badge className="bg-emerald-500/10 text-emerald-500 border-emerald-500/20 hover:bg-emerald-500/20 rounded-full text-xs px-2.5 py-0.5">
                            Current
                        </Badge>
                    )}
                </div>
                <div className="flex items-center gap-2">
                    {(isDeploying || isQueued) && (
                        <LoadingButton
                            isLoading={isCancelling}
                            onClick={handleCancelAsync}
                            variant="destructive"
                            size="sm"
                        >
                            Cancel
                        </LoadingButton>
                    )}
                    <LoadingButton isLoading={isRedeploying} onClick={handleRedeployAsync} variant="outline">
                        Redeploy
                    </LoadingButton>
                </div>
            </div>

            {/* Info cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Deployment Info */}
                <div className="rounded-xl border bg-card p-5 space-y-4">
                    <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">Deployment Info</h3>
                    <div className="space-y-3">
                        <InfoRow label="Status">
                            <Badge
                                variant="outline"
                                className={cn("rounded-full text-xs px-2.5 py-0.5", getStatusBadgeClasses(status))}
                            >
                                {deployment.status}
                            </Badge>
                        </InfoRow>
                        <InfoRow label="Environment">
                            <span className="font-medium">{deployment.environment}</span>
                        </InfoRow>
                        <InfoRow label="Created">
                            <div className="flex items-center gap-2">
                                <Calendar className="h-3.5 w-3.5 text-muted-foreground" />
                                <span className="font-medium">{Formatter.fromDate(deployment.createdAt, "long")}</span>
                            </div>
                        </InfoRow>
                        <InfoRow label="Duration">
                            <div className="flex items-center gap-2">
                                <motion.div
                                    animate={isDeploying ? { rotate: 360 } : {}}
                                    transition={isDeploying ? { duration: 2, repeat: Infinity, ease: "linear" } : {}}
                                >
                                    <Clock className="h-3.5 w-3.5 text-muted-foreground" />
                                </motion.div>
                                <span className={cn("font-medium font-mono", isDeploying && "text-amber-500")}>
                                    {isQueued ? "Waiting..." : buildDuration}
                                </span>
                            </div>
                        </InfoRow>
                        <InfoRow label="URL">
                            <div className="flex items-center gap-2">
                                <a
                                    href={"https://" + deployment.url!}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="text-white hover:text-zinc-300 hover:underline transition-colors text-sm truncate max-w-[200px]"
                                >
                                    {deployment.url}
                                </a>
                                <ExternalLink className="h-3 w-3 text-white flex-shrink-0" />
                                <CopyButton value={deployment.url!} className="p-1" iconClassName="h-3 w-3" />
                            </div>
                        </InfoRow>
                    </div>
                </div>

                {/* Git Info */}
                <div className="rounded-xl border bg-card p-5 space-y-4">
                    <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">Git Info</h3>
                    <div className="space-y-3">
                        <InfoRow label="Branch">
                            <div className="flex items-center gap-2">
                                <GitBranch className="h-3.5 w-3.5 text-muted-foreground" />
                                <span className="font-medium">{deployment.branch}</span>
                            </div>
                        </InfoRow>
                        <InfoRow label="Commit">
                            <div className="flex items-center gap-2">
                                <GitCommit className="h-3.5 w-3.5 text-muted-foreground" />
                                <CopyText value={deployment.commitHash!} className="font-mono">
                                    <span className="font-mono text-sm font-medium hover:text-white transition-colors">{deployment.commitHash?.substring(0, 7)}</span>
                                </CopyText>
                            </div>
                        </InfoRow>
                        <InfoRow label="Message">
                            <span className="font-medium text-sm truncate max-w-[250px]" title={deployment.commitMessage ?? ""}>
                                {deployment.commitMessage}
                            </span>
                        </InfoRow>
                        <InfoRow label="Author">
                            <div className="flex items-center gap-2">
                                <User className="h-3.5 w-3.5 text-muted-foreground" />
                                <span className="font-medium">{deployment.author}</span>
                            </div>
                        </InfoRow>
                    </div>
                </div>
            </div>

            {/* Build Logs */}
            <div className="rounded-xl border bg-card overflow-hidden">
                <div className="flex items-center justify-between px-5 py-3 border-b">
                    <div>
                        <h3 className="text-sm font-semibold">Build Logs</h3>
                        <p className="text-xs text-muted-foreground">Output from the build process</p>
                    </div>
                    <CopyButton
                        value={logs?.flatMap((log) => `${log.timestamp} ${log.level} ${log.message}`).join("\n") ?? ""}
                        className="flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground"
                    >
                        <span>Copy</span>
                    </CopyButton>
                </div>
                <div
                    ref={logContainerRef}
                    className="bg-zinc-950 font-mono text-sm p-4 h-[440px] overflow-y-auto scrollbar-thin"
                >
                    {logs?.map((log, index) => (
                        <LogEntry key={index} {...log} />
                    ))}
                    {isDeploying && (
                        <div className="flex items-center gap-2 mt-1 text-zinc-300">
                            <motion.span
                                animate={{ opacity: [1, 0.3, 1] }}
                                transition={{ duration: 1.5, repeat: Infinity }}
                                className="inline-block"
                            >
                                _
                            </motion.span>
                        </div>
                    )}
                </div>
            </div>
        </div>
    )
}

function InfoRow({ label, children }: { label: string; children: React.ReactNode }) {
    return (
        <div className="flex justify-between items-center">
            <span className="text-sm text-muted-foreground">{label}</span>
            <div className="flex items-center">{children}</div>
        </div>
    );
}

function LogEntry(logEntry: {
    timestamp?: string | null;
    level?: string | null;
    message?: string | null;
}) {
    const getLogTextColor = () => {
        const logLevel = logEntry.level;
        if (logLevel === "Error") return "text-red-400";
        if (logLevel === "Warning") return "text-amber-400";
        if (logLevel === "Info") return "text-zinc-400";
        if (logLevel === "Success") return "text-emerald-400";
        return "text-zinc-500";
    }

    const getIcon = () => {
        switch (logEntry.level) {
            case "Error":
                return <CircleAlert className="h-3.5 w-3.5 min-h-3.5 min-w-3.5 text-red-400" />;
            case "Warning":
                return <TriangleAlert className="h-3.5 w-3.5 min-h-3.5 min-w-3.5 text-amber-400" />;
            case "Info":
                return <Info className="h-3.5 w-3.5 min-h-3.5 min-w-3.5 text-zinc-500" />;
            case "Success":
                return <CircleCheck className="h-3.5 w-3.5 min-h-3.5 min-w-3.5 text-emerald-400" />;
            default:
                return <CircleDot className="h-3.5 w-3.5 min-h-3.5 min-w-3.5 text-zinc-600" />;
        }
    }

    return (
        <div className="flex items-start gap-2 py-0.5 hover:bg-white/[0.02] rounded px-1 -mx-1">
            <span className="text-zinc-600 whitespace-nowrap text-xs leading-5 select-none">
                {logEntry.timestamp && Formatter.fromDate(new Date(new Date(logEntry.timestamp).getTime() - new Date(logEntry.timestamp).getTimezoneOffset() * 60000), "long")}
            </span>
            <span className="leading-5 flex-shrink-0">{getIcon()}</span>
            <span className={cn("whitespace-pre-wrap leading-5 text-xs", getLogTextColor())}>
                {logEntry.message}
            </span>
        </div>
    );
}
