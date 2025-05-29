import { api } from "@/api-client"
import InfiniteScrollContainer from "@/components/infinite-scroll-container"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { useTimeAgo } from '@/hooks/useTimeAgo'
import { DeploymentStatus, getDeploymentStatusFromNumber } from "@/lib/deployment-status"
import { getStatusDot } from "@/lib/deployment-status-component"
import { DeploymentStatusFilter } from "@/lib/deployment-status-filter"
import { Formatter } from "@/lib/formatter"
import { getLink } from "@/links"
import { createFileRoute, useRouter } from '@tanstack/react-router'
import { motion } from "framer-motion"
import {
    GitBranchIcon,
    GitCommitIcon,
    RefreshCw
} from "lucide-react"
import { useMemo, useState } from "react"

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/deployments/',
)({
    component: RouteComponent,
    loader: (ctx) => api.v1.getProjectsProjectIdDeployments.prefetchQuery({ parameters: { path: { projectId: ctx.params.projectId } } })
})

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

    function DeploymentRow({ deployment, projectId, router }: { deployment: any, projectId: string, router: any }) {
        const timeAgo = useTimeAgo(deployment.createdAt!);
        return (
            <div key={deployment.id} className={`flex items-center justify-between py-4 px-4 border-b bg-muted/50 hover:bg-muted/70 hover:cursor-pointer`}
                onClick={() => {
                    router.navigate({ to: `/projects/${projectId}/deployments/${deployment.id}` })
                }}>
                <div className="flex-1 min-w-0">
                    <div className="flex items-center">
                        <div className="min-w-0 flex-1 flex flex-row gap-2">
                            <div className="flex items-start flex-col">
                                <span className="font-mono text-sm">{deployment.id?.substring(0, 8)}</span>
                                <div className="text-sm text-muted-foreground">{deployment.environment}</div>
                            </div>
                            {deployment.environment === "Production" && (
                                <div className="flex items-center">
                                    {deployment.isCurrent && (
                                        <Badge
                                            variant="outline"
                                            className="text-xs rounded-full px-2 py-0 h-5 bg-primary/10 border-primary/20"
                                        >
                                            Current
                                        </Badge>
                                    )}
                                </div>
                            )}
                        </div>
                    </div>
                </div>

                <div className="flex items-center gap-2 flex-1">
                    <div className="flex items-center">
                        {getStatusDot(getDeploymentStatusFromNumber(deployment.status))}
                        <span>{getDeploymentStatusFromNumber(deployment.status)}</span>
                    </div>
                    <div className="text-sm text-muted-foreground">{timeAgo}</div>
                </div>

                <div className="flex items-start flex-1 flex-col px-3">
                    <div className="flex items-center gap-1">
                        <GitBranchIcon className="h-4 w-4 py-[2px] text-muted-foreground" />
                        <span>{deployment.branch}</span>
                    </div>
                    <div className="flex items-center gap-1 text-sm text-muted-foreground">
                        <GitCommitIcon className="h-4 w-4" />
                        <span className="font-mono">{deployment.commitHash?.substring(0, 6)}</span>
                        <span className="truncate w-[200px]">{deployment.commitMessage}</span>
                    </div>
                </div>

                <div className="flex items-center gap-4">
                    <div className="text-sm text-muted-foreground text-right flex flex-col">
                        <div>{Formatter.fromDate(deployment.createdAt, "long")}</div>
                        <div>by {deployment.author}</div>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div className="flex items-center gap-2">
                    <h2 className="text-3xl font-bold tracking-tight">Deployments</h2>
                    <Button
                        variant="ghost"
                        size="icon"
                        className="rounded-full"
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
            </div>

            <div className="flex flex-col md:flex-row gap-2">
                {/* <div className="relative flex-1">
                    <SearchIcon className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                    <Input placeholder="All Branches..." className="pl-8 pr-10" />
                    <ChevronDownIcon className="absolute right-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                </div> */}

                {/* <Select defaultValue="all">
                    <SelectTrigger className="w-full md:w-[200px]">
                        <SelectValue placeholder="All Environments" />
                    </SelectTrigger>
                    <SelectContent>
                        <SelectItem value="all">All Environments</SelectItem>
                        <SelectItem value="production">Production</SelectItem>
                       
                    </SelectContent>
                </Select> */}

                <Select defaultValue="all" onValueChange={(value) => {
                    switch (value) {
                        case "all":
                            setFilter(DeploymentStatusFilter.All);
                            break;
                        case "completed":
                            setFilter(DeploymentStatusFilter.Completed);
                            break;
                        case "deploying":
                            setFilter(DeploymentStatusFilter.Deploying);
                            break;
                        case "queued":
                            setFilter(DeploymentStatusFilter.Queued);
                            break;
                        case "failed":
                            setFilter(DeploymentStatusFilter.Failed);
                            break;
                        case "cancelled":
                            setFilter(DeploymentStatusFilter.Cancelled);
                            break;
                        default:
                            setFilter(DeploymentStatusFilter.All);
                            break;
                    }
                }
                }>
                    <SelectTrigger className="w-full md:w-[150px]">
                        <SelectValue placeholder="Status" />
                    </SelectTrigger>
                    <SelectContent>
                        <SelectItem value="all">All Status</SelectItem>
                        <SelectItem value="completed">Completed</SelectItem>
                        <SelectItem value="deploying">Deploying</SelectItem>
                        <SelectItem value="queued">Queued</SelectItem>
                        <SelectItem value="failed">Failed</SelectItem>
                        <SelectItem value="cancelled">Cancelled</SelectItem>
                    </SelectContent>
                </Select>
            </div>

            {deployments?.length || 0 > 0 ? (
                <div className="space-y-px">
                    <InfiniteScrollContainer onBottomReached={fetchNextPage}>
                        {deployments!.map((deployment) => (
                            <DeploymentRow key={deployment.id} deployment={deployment} projectId={projectId} router={router} />
                        ))}
                    </InfiniteScrollContainer>
                </div>
            ) : (
                <div className="text-center py-10">
                    <p>No deployments found</p>
                    {filter === DeploymentStatusFilter.All && <Button variant="outline" className="mt-4" onClick={() => open(getLink('addNewService'), '_blank')}>
                        Create your first deployment
                    </Button>}
                </div>
            )}
        </div>
    )
}
