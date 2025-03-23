
import { useState, useEffect } from "react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import {
    CalendarIcon,
    ChevronDownIcon,
    GitBranchIcon,
    GitCommitIcon,
    MoreVertical,
    RefreshCw,
    SearchIcon,
} from "lucide-react"
import { Input } from "@/components/ui/input"
import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'

interface Deployment {
    id: string
    status: "Ready" | "Building" | "Error" | "Canceled"
    environment: "Production" | "Preview" | "Development"
    branch: string
    commitHash: string
    commitMessage: string
    createdAt: string
    createdTimeAgo: string
    creator: string
    isCurrent?: boolean
}

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/deployments/',
)({
    component: RouteComponent,
})

function RouteComponent() {
    const params = Route.useParams();
    const [deployments, setDeployments] = useState<Deployment[]>([])
    const [loading, setLoading] = useState(true)

    useEffect(() => {
        // Simulate API fetch
        setTimeout(() => {
            setDeployments([
                {
                    id: "p6tawrave",
                    status: "Ready",
                    environment: "Production",
                    branch: "main",
                    commitHash: "8514b61",
                    commitMessage: "Removed ip restriction",
                    createdAt: "14/11/2023",
                    createdTimeAgo: "50s (495d ago)",
                    creator: "John Doe",
                    isCurrent: true,
                },
                {
                    id: "38zh82ytm",
                    status: "Ready",
                    environment: "Production",
                    branch: "main",
                    commitHash: "4b15ad1",
                    commitMessage: "Bug fix",
                    createdAt: "13/11/2023",
                    createdTimeAgo: "49s (496d ago)",
                    creator: "Jane Smith",
                },
                {
                    id: "i1iy7x3gp",
                    status: "Ready",
                    environment: "Production",
                    branch: "main",
                    commitHash: "862fdc3",
                    commitMessage: "Edit documentation",
                    createdAt: "13/11/2023",
                    createdTimeAgo: "50s (496d ago)",
                    creator: "John Doe",
                },
                {
                    id: "k9pqr2zxw",
                    status: "Building",
                    environment: "Preview",
                    branch: "feature/new-api",
                    commitHash: "7d23f89",
                    commitMessage: "Add new API endpoints",
                    createdAt: "12/11/2023",
                    createdTimeAgo: "1m (497d ago)",
                    creator: "Jane Smith",
                },
                {
                    id: "v3mnt7qrs",
                    status: "Error",
                    environment: "Preview",
                    branch: "fix/auth-bug",
                    commitHash: "5a91e2c",
                    commitMessage: "Fix authentication bug",
                    createdAt: "11/11/2023",
                    createdTimeAgo: "45s (498d ago)",
                    creator: "John Doe",
                },
            ])
            setLoading(false)
        }, 1000)
    }, [])

    const getStatusDot = (status: string) => {
        switch (status) {
            case "Ready":
                return <span className="h-2 w-2 rounded-full bg-green-500 mr-2"></span>
            case "Building":
                return <span className="h-2 w-2 rounded-full bg-blue-500 mr-2"></span>
            case "Error":
                return <span className="h-2 w-2 rounded-full bg-red-500 mr-2"></span>
            case "Canceled":
                return <span className="h-2 w-2 rounded-full bg-gray-500 mr-2"></span>
            default:
                return <span className="h-2 w-2 rounded-full bg-gray-500 mr-2"></span>
        }
    }

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div className="flex items-center gap-2">
                    <h2 className="text-3xl font-bold tracking-tight">Deployments</h2>
                    <Button variant="ghost" size="icon" className="rounded-full">
                        <RefreshCw className="h-4 w-4" />
                    </Button>
                </div>
                <div className="flex items-center gap-2">
                    <Button variant="outline">
                        <CalendarIcon className="mr-2 h-4 w-4" />
                        Select Date Range
                    </Button>
                    <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                            <Button variant="outline" className="ml-auto">
                                <MoreVertical className="h-4 w-4" />
                            </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                            <DropdownMenuItem>Create Manual Deployment</DropdownMenuItem>
                            <DropdownMenuItem>View Deployment Settings</DropdownMenuItem>
                            <DropdownMenuSeparator />
                            <DropdownMenuItem>View Deployment Logs</DropdownMenuItem>
                        </DropdownMenuContent>
                    </DropdownMenu>
                </div>
            </div>

            <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <RefreshCw className="h-4 w-4" />
                <span>Continuously generated from</span>
                <span className="flex items-center">
                    <GitBranchIcon className="h-4 w-4 mr-1" />
                    <span className="font-semibold">username/repository</span>
                </span>
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
                        <SelectItem value="preview">Preview</SelectItem>
                        <SelectItem value="development">Development</SelectItem>
                    </SelectContent>
                </Select>

                <Select defaultValue="all">
                    <SelectTrigger className="w-full md:w-[150px]">
                        <SelectValue placeholder="Status" />
                    </SelectTrigger>
                    <SelectContent>
                        <SelectItem value="all">All Status</SelectItem>
                        <SelectItem value="ready">Ready</SelectItem>
                        <SelectItem value="building">Building</SelectItem>
                        <SelectItem value="error">Error</SelectItem>
                    </SelectContent>
                </Select>
            </div>

            {loading ? (
                <div className="space-y-4">
                    {[1, 2, 3].map((i) => (
                        <Skeleton key={i} className="h-[72px] w-full" />
                    ))}
                </div>
            ) : deployments.length > 0 ? (
                <div className="space-y-px">
                    {deployments.map((deployment) => (
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
                                    {getStatusDot(deployment.status)}
                                    <span>{deployment.status}</span>
                                </div>
                                <div className="text-sm text-muted-foreground">{deployment.createdTimeAgo}</div>
                            </div>

                            <div className="flex items-center gap-2 flex-1">
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
                    ))}
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
