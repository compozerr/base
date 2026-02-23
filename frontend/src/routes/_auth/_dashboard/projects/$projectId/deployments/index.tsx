import { api } from "@/api-client"
import InfiniteScrollContainer from "@/components/infinite-scroll-container"
import LoadingButton from "@/components/loading-button"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { useDuration } from '@/hooks/use-duration'
import { useTimeAgo } from '@/hooks/useTimeAgo'
import { DeploymentStatus } from "@/lib/deployment-status"
import { DeploymentStatusFilter } from "@/lib/deployment-status-filter"
import { Formatter } from "@/lib/formatter"
import { getLink } from "@/links"
import { createFileRoute, useRouter } from '@tanstack/react-router'
import { motion } from "framer-motion"
import {
    Clock,
    GitBranchIcon,
    GitCommitIcon,
    PackageIcon,
    RefreshCw,
    RocketIcon,
} from "lucide-react"
import { useMemo, useState } from "react"

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/deployments/',
)({
    component: RouteComponent,
    loader: (ctx) => api.v1.getProjectsProjectIdDeployments.prefetchQuery({ parameters: { path: { projectId: ctx.params.projectId } } })
})

const filterOptions = [
    { label: "All", value: DeploymentStatusFilter.All },
    { label: "Completed", value: DeploymentStatusFilter.Completed },
    { label: "Deploying", value: DeploymentStatusFilter.Deploying },
    { label: "Queued", value: DeploymentStatusFilter.Queued },
    { label: "Failed", value: DeploymentStatusFilter.Failed },
    { label: "Cancelled", value: DeploymentStatusFilter.Cancelled },
] as const;

function getStatusColor(status: DeploymentStatus) {
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

function getStatusTextColor(status: DeploymentStatus) {
    switch (status) {
        case DeploymentStatus.Completed:
            return "text-emerald-500";
        case DeploymentStatus.Deploying:
            return "text-amber-500";
        case DeploymentStatus.Queued:
            return "text-white";
        case DeploymentStatus.Failed:
            return "text-red-500";
        case DeploymentStatus.Cancelled:
            return "text-zinc-500";
        default:
            return "text-zinc-500";
    }
}

function isActiveStatus(status: DeploymentStatus) {
    return status === DeploymentStatus.Deploying || status === DeploymentStatus.Queued;
}

function RouteComponent() {
    const router = useRouter();
    const { projectId } = Route.useParams();

    const [filter, setFilter] = useState<DeploymentStatusFilter>(DeploymentStatusFilter.All);

    const { data: deploymentsData, refetch, fetchNextPage } = api.v1.getProjectsProjectIdDeployments.useInfiniteQuery(
        {
            path: { projectId }, query: {
                deploymentStatus: filter,
            }
        },
        {
            refetchInterval(ctx) {
                const hasActive = ctx.state.data?.pages.flatMap(page => page.items ?? []).some(p => [DeploymentStatus.Deploying, DeploymentStatus.Queued].includes(p.status as DeploymentStatus));
                return hasActive ? 10000 : 30000;
            },
            getNextPageParam: (lastPage) => {
                if (!lastPage) return undefined;
                const currentPage = lastPage.page ?? 1;
                const total = lastPage.totalCount ?? 0;
                const pageSize = lastPage.pageSize ?? 20;
                const totalPages = Math.ceil(total / pageSize);
                if (currentPage < totalPages) {
                    return {
                        query: {
                            page: currentPage + 1,
                            pageSize: pageSize,
                        }
                    };
                }
                return undefined;
            },
            initialPageParam: {
                query: {
                    page: 1
                }
            }
        }
    );

    const deployments = useMemo(() => deploymentsData?.pages.flatMap((page) => page.items ?? []) || [], [deploymentsData]);

    const [rotation, setRotation] = useState(0);

    const handleClick = () => {
        refetch();
        setRotation((prev) => prev + 360);
    };

    const { mutateAsync: deployFromLatestCommitAsync, isPending: isDeployFromLatestCommitPending } = api.v1.postProjectsProjectIdDeploymentsDeployFromLatestCommit.useMutation();

    const handleDeployFromLatestCommitAsync = async () => {
        await deployFromLatestCommitAsync({ path: { projectId } });
        refetch();
    }

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex flex-col gap-4 sm:flex-row sm:justify-between sm:items-center">
                <div className="flex items-center gap-3">
                    <h2 className="text-3xl font-bold tracking-tight">Deployments</h2>
                    <Button
                        variant="ghost"
                        size="icon"
                        className="rounded-full h-8 w-8"
                        onClick={() => {
                            router.invalidate();
                        }}
                        asChild
                    >
                        <motion.div
                            animate={{ rotate: rotation }}
                            transition={{ duration: 0.5, ease: "easeInOut" }}
                            onClick={handleClick}
                        >
                            <RefreshCw className="h-4 w-4" />
                        </motion.div>
                    </Button>
                </div>
                <LoadingButton
                    isLoading={isDeployFromLatestCommitPending}
                    onClick={handleDeployFromLatestCommitAsync}
                >
                    <RocketIcon className="h-4 w-4 mr-2" />
                    Deploy from Latest Commit
                </LoadingButton>
            </div>

            {/* Segmented filter */}
            <div className="flex flex-wrap gap-1 p-1 rounded-lg bg-muted/50 w-fit">
                {filterOptions.map((option) => (
                    <button
                        key={option.label}
                        onClick={() => setFilter(option.value)}
                        className={`px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
                            filter === option.value
                                ? "bg-background text-foreground shadow-sm"
                                : "text-muted-foreground hover:text-foreground"
                        }`}
                    >
                        {option.label}
                    </button>
                ))}
            </div>

            {/* Deployment list */}
            {deployments?.length || 0 > 0 ? (
                <InfiniteScrollContainer onBottomReached={fetchNextPage} className="space-y-2">
                    {deployments!.map((deployment, index) => (
                        <motion.div
                            key={deployment.id}
                            initial={{ opacity: 0 }}
                            animate={{ opacity: 1 }}
                            transition={{ duration: 0.2, delay: index * 0.03 }}
                        >
                            <DeploymentCard
                                deployment={deployment}
                                projectId={projectId}
                                router={router}
                            />
                        </motion.div>
                    ))}
                </InfiniteScrollContainer>
            ) : (
                <div className="flex flex-col items-center justify-center py-16 text-center">
                    <div className="rounded-full bg-muted p-4 mb-4">
                        <PackageIcon className="h-8 w-8 text-muted-foreground" />
                    </div>
                    <p className="text-lg font-medium mb-1">No deployments found</p>
                    <p className="text-sm text-muted-foreground mb-4">
                        {filter === DeploymentStatusFilter.All
                            ? "Get started by deploying your first build."
                            : "No deployments match the selected filter."}
                    </p>
                    {filter === DeploymentStatusFilter.All && (
                        <Button variant="outline" onClick={() => open(getLink('addNewService'), '_blank')}>
                            Create your first deployment
                        </Button>
                    )}
                </div>
            )}
        </div>
    )
}

function DeploymentCard({ deployment, projectId, router }: {
    deployment: {
        id?: string | null;
        environment?: string | null;
        isCurrent?: boolean | null;
        status?: string | null;
        createdAt?: string | null;
        branch?: string | null;
        commitHash?: string | null;
        commitMessage?: string | null;
        author?: string | null;
        buildDuration?: string | null;
    };
    projectId: string;
    router: ReturnType<typeof useRouter>;
}) {
    const timeAgo = useTimeAgo(deployment.createdAt!);
    const status = deployment.status as DeploymentStatus;
    const active = isActiveStatus(status);
    const isDeploying = status === DeploymentStatus.Deploying;
    const isQueued = status === DeploymentStatus.Queued;
    const duration = useDuration(deployment.buildDuration!, isDeploying);

    return (
        <div
            className={`group relative rounded-lg border bg-card p-4 transition-colors hover:bg-accent/50 cursor-pointer ${
                active ? "border-white/20" : "border-border"
            }`}
            onClick={() => {
                router.navigate({ to: `/projects/${projectId}/deployments/${deployment.id}` })
            }}
        >
            {/* Active deployment pulse border overlay */}
            {active && (
                <div className="absolute inset-0 rounded-lg border border-white/10 animate-pulse pointer-events-none" />
            )}

            <div className="flex items-center gap-4">
                {/* Status dot + deployment ID */}
                <div className="flex items-center gap-3 min-w-[140px]">
                    <div className="relative">
                        <span className={`block h-3 w-3 rounded-full ${getStatusColor(status)}`} />
                        {active && (
                            <span className={`absolute inset-0 h-3 w-3 rounded-full ${getStatusColor(status)} animate-ping opacity-40`} />
                        )}
                    </div>
                    <div className="flex flex-col">
                        <span className="font-mono text-sm font-medium">{deployment.id?.substring(0, 8)}</span>
                        <span className={`text-xs font-medium ${getStatusTextColor(status)}`}>{deployment.status}</span>
                    </div>
                </div>

                {/* Current badge */}
                {deployment.isCurrent && deployment.environment === "Production" && (
                    <Badge className="bg-emerald-500/10 text-emerald-500 border-emerald-500/20 hover:bg-emerald-500/20 rounded-full text-xs px-2.5 py-0.5">
                        Current
                    </Badge>
                )}

                {/* Branch + commit info */}
                <div className="flex-1 min-w-0 hidden sm:flex flex-col gap-0.5 px-2">
                    <div className="flex items-center gap-1.5">
                        <GitBranchIcon className="h-3.5 w-3.5 text-muted-foreground flex-shrink-0" />
                        <span className="text-sm truncate max-w-[200px]">{deployment.branch}</span>
                    </div>
                    <div className="flex items-center gap-1.5 text-muted-foreground">
                        <GitCommitIcon className="h-3.5 w-3.5 flex-shrink-0" />
                        <span className="font-mono text-xs">{deployment.commitHash?.substring(0, 7)}</span>
                        <span className="text-xs truncate max-w-[240px]">{deployment.commitMessage}</span>
                    </div>
                </div>

                {/* Time + duration + author */}
                <div className="flex flex-col items-end text-right ml-auto flex-shrink-0">
                    <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
                        <span>{timeAgo}</span>
                        <span className="text-zinc-600">·</span>
                        <Clock className="h-3 w-3" />
                        <span className="font-mono text-xs">
                            {isQueued ? "—" : duration}
                        </span>
                    </div>
                    <span className="text-xs text-muted-foreground">by {deployment.author}</span>
                </div>
            </div>

            {/* Mobile: branch/commit (visible on small screens) */}
            <div className="flex flex-col gap-0.5 mt-3 sm:hidden">
                <div className="flex items-center gap-1.5">
                    <GitBranchIcon className="h-3.5 w-3.5 text-muted-foreground flex-shrink-0" />
                    <span className="text-sm truncate">{deployment.branch}</span>
                </div>
                <div className="flex items-center gap-1.5 text-muted-foreground">
                    <GitCommitIcon className="h-3.5 w-3.5 flex-shrink-0" />
                    <span className="font-mono text-xs">{deployment.commitHash?.substring(0, 7)}</span>
                    <span className="text-xs truncate">{deployment.commitMessage}</span>
                </div>
            </div>
        </div>
    );
}
