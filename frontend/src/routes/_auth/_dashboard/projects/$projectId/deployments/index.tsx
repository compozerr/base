import { api } from "@/api-client"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { useTimeAgo } from '@/hooks/useTimeAgo'
import { DeploymentStatus, getDeploymentStatusFromNumber } from "@/lib/deployment-status"
import { createFileRoute, useRouter } from '@tanstack/react-router'
import { motion } from "framer-motion"
import {
    CalendarIcon,
    ChevronDownIcon,
    GitBranchIcon,
    GitCommitIcon,
    MoreVertical,
    RefreshCw,
    SearchIcon,
} from "lucide-react"
import { useState } from "react"

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/deployments/',
)({
    component: RouteComponent,
    loader: (ctx) => api.v1.getProjectsProjectIdDeployments.fetchQuery({ parameters: { path: { projectId: ctx.params.projectId } } })
})

function RouteComponent() {
    const router = useRouter();
    const deployments = Route.useLoaderData();

    const getStatusDot = (status: DeploymentStatus) => {
        switch (status) {
            case DeploymentStatus.Completed:
                return <span className="h-2 w-2 rounded-full bg-green-500 mr-2"></span>
            case DeploymentStatus.Deploying:
                return <span className="h-2 w-2 rounded-full bg-blue-500 mr-2"></span>
            case DeploymentStatus.Failed:
                return <span className="h-2 w-2 rounded-full bg-red-500 mr-2"></span>
            case DeploymentStatus.Queued:
                return <span className="h-2 w-2 rounded-full bg-gray-500 mr-2"></span>
            default:
                return <span className="h-2 w-2 rounded-full bg-gray-500 mr-2"></span>
        }
    }

    const [rotation, setRotation] = useState(0);

    const handleClick = () => {
        setRotation((prev) => prev + 360);
    };

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
                <div className="relative flex-1">
                    <SearchIcon className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                    <Input placeholder="All Branches..." className="pl-8 pr-10" />
                    <ChevronDownIcon className="absolute right-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                </div>

                <Select defaultValue="all">
                    <SelectTrigger className="w-full md:w-[200px]">
                        <SelectValue placeholder="All Environments" />
                    </SelectTrigger>
                    <SelectContent>
                        <SelectItem value="all">All Environments</SelectItem>
                        <SelectItem value="production">Production</SelectItem>
                        {/* <SelectItem value="preview">Preview</SelectItem>
                        <SelectItem value="development">Development</SelectItem> */}
                    </SelectContent>
                </Select>

                <Select defaultValue="all">
                    <SelectTrigger className="w-full md:w-[150px]">
                        <SelectValue placeholder="Status" />
                    </SelectTrigger>
                    <SelectContent>
                        <SelectItem value="all">All Status</SelectItem>
                        <SelectItem value="completed">Completed</SelectItem>
                        <SelectItem value="deploying">Deploying</SelectItem>
                        <SelectItem value="failed">Failed</SelectItem>
                    </SelectContent>
                </Select>
            </div>

            {deployments.length > 0 ? (
                <div className="space-y-px">
                    {deployments.map((deployment) => {
                        const timeAgo = useTimeAgo(deployment.createdAt!);
                        return (
                            <div key={deployment.id} className="flex items-center justify-between py-4 px-4 border-b hover:bg-muted/50">
                                <div className="flex-1 min-w-0">
                                    <div className="flex items-center">
                                        <div className="min-w-0 flex-1">
                                            <div className="flex items-center gap-2">
                                                <span className="font-mono text-sm">{deployment.id}</span>
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
                                            <div className="text-sm text-muted-foreground">{deployment.environment}</div>
                                        </div>
                                    </div>
                                </div>

                                <div className="flex items-center gap-2 flex-1">
                                    <div className="flex items-center">
                                        {getStatusDot(getDeploymentStatusFromNumber(deployment.status))}
                                        <span>{deployment.status}</span>
                                    </div>
                                    <div className="text-sm text-muted-foreground">{timeAgo}</div>
                                </div>

                                <div className="flex items-center gap-2 flex-1 flex-col">
                                    <div className="flex items-center gap-1">
                                        <GitBranchIcon className="h-4 w-4 text-muted-foreground" />
                                        <span>{deployment.branch}</span>
                                    </div>
                                    <div className="flex items-center gap-1 text-sm text-muted-foreground">
                                        <GitCommitIcon className="h-4 w-4" />
                                        <span className="font-mono">{deployment.commitHash}</span>
                                        <span>{deployment.commitMessage}</span>
                                    </div>
                                </div>

                                <div className="flex items-center gap-4">
                                    <div className="text-sm text-muted-foreground text-right">
                                        <div>{deployment.createdAt}</div>
                                        <div>by {deployment.creator}</div>
                                    </div>

                                    <DropdownMenu>
                                        <DropdownMenuTrigger asChild>
                                            <Button variant="ghost" size="icon">
                                                <MoreVertical className="h-4 w-4" />
                                            </Button>
                                        </DropdownMenuTrigger>
                                        <DropdownMenuContent align="end">
                                            <DropdownMenuItem>Visit</DropdownMenuItem>
                                            <DropdownMenuItem>View Build Logs</DropdownMenuItem>
                                            <DropdownMenuItem>Inspect</DropdownMenuItem>
                                            <DropdownMenuSeparator />
                                            <DropdownMenuItem>Promote to Production</DropdownMenuItem>
                                            <DropdownMenuItem className="text-destructive">Delete Deployment</DropdownMenuItem>
                                        </DropdownMenuContent>
                                    </DropdownMenu>
                                </div>
                            </div>
                        )
                    })}
                </div>
            ) : (
                <div className="text-center py-10">
                    <p>No deployments found</p>
                    <Button variant="outline" className="mt-4">
                        Create your first deployment
                    </Button>
                </div>
            )}
        </div>
    )
}
