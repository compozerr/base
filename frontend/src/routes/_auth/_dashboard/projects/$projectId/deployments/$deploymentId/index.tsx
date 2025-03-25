import * as React from 'react'
import { createFileRoute, useRouter } from '@tanstack/react-router'
import { api } from '@/api-client';
import { useState, useEffect } from "react"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { ArrowLeft, Calendar, Clock, Copy, ExternalLink, GitBranch, GitCommit, MoreVertical } from "lucide-react"
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

interface DeploymentDetails {
    id: string
    status: "Ready" | "Building" | "Error" | "Canceled"
    environment: "Production" | "Preview" | "Development"
    branch: string
    commitHash: string
    commitMessage: string
    createdAt: string
    createdTimeAgo: string
    creator: string
    url: string
    duration: string
    region: string
    framework: string
    nodeVersion: string
    isCurrent?: boolean
    logs: {
        build: string[]
        runtime: string[]
    }
}

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/deployments/$deploymentId/',
)({
    component: RouteComponent,
    loader(ctx) {
        return api.v1.getProjectsProjectIdDeploymentsDeploymentId.fetchQuery({ parameters: { path: { projectId: ctx.params.projectId, deploymentId: ctx.params.deploymentId } } });
    },
})

function RouteComponent() {
    const params = Route.useParams();
    // const deployment = Route.useLoaderData();

    const router = useRouter()
    const deploymentId = params.deploymentId as string

    const [deployment, setDeployment] = useState<DeploymentDetails | null>(null)
    const [loading, setLoading] = useState(true)

    useEffect(() => {
        // Simulate API fetch
        setTimeout(() => {
            setDeployment({
                id: deploymentId,
                status: "Ready",
                environment: "Production",
                branch: "main",
                commitHash: "8514b61",
                commitMessage: "Removed ip restriction",
                createdAt: "14/11/2023 10:45 AM",
                createdTimeAgo: "50s (495d ago)",
                creator: "John Doe",
                url: `https://${deploymentId}.vercel.app`,
                duration: "1m 23s",
                region: "sfo1",
                framework: "Next.js",
                nodeVersion: "18.x",
                isCurrent: true,
                logs: {
                    build: [
                        "10:45:01 AM: Build started",
                        "10:45:05 AM: Installing dependencies",
                        "10:45:30 AM: npm install completed",
                        "10:45:35 AM: Running build command",
                        "10:46:10 AM: Creating an optimized production build...",
                        "10:46:15 AM: Compiled successfully",
                        "10:46:20 AM: Build completed",
                        "10:46:24 AM: Deployment ready",
                    ],
                    runtime: [
                        "10:46:25 AM: Deployment is live",
                        "10:47:30 AM: GET /api/users 200 in 120ms",
                        "10:48:15 AM: GET /api/products 200 in 85ms",
                        "10:50:22 AM: POST /api/auth 201 in 230ms",
                        "10:52:45 AM: GET /api/dashboard 200 in 150ms",
                    ],
                },
            })
            setLoading(false)
        }, 1000)
    }, [deploymentId])

    const getStatusDot = (status: string) => {
        switch (status) {
            case "Ready":
                return <span className="h-3 w-3 rounded-full bg-green-500 mr-2"></span>
            case "Building":
                return <span className="h-3 w-3 rounded-full bg-blue-500 mr-2"></span>
            case "Error":
                return <span className="h-3 w-3 rounded-full bg-red-500 mr-2"></span>
            case "Canceled":
                return <span className="h-3 w-3 rounded-full bg-gray-500 mr-2"></span>
            default:
                return <span className="h-3 w-3 rounded-full bg-gray-500 mr-2"></span>
        }
    }

    const copyToClipboard = (text: string) => {
        navigator.clipboard
            .writeText(text)
            .then(() => {
                // Could add a toast notification here
                console.log("Copied to clipboard")
            })
            .catch((err) => {
                console.error("Failed to copy: ", err)
            })
    }

    if (loading) {
        return (
            <div className="space-y-6">
                <div className="flex items-center gap-2">
                    <Button variant="ghost" size="icon" onClick={() => router.navigate({})}>
                        <ArrowLeft className="h-4 w-4" />
                    </Button>
                    <Skeleton className="h-8 w-48" />
                </div>
                <Skeleton className="h-[200px] w-full" />
                <Skeleton className="h-[400px] w-full" />
            </div>
        )
    }

    if (!deployment) {
        return (
            <div className="space-y-6">
                <div className="flex items-center gap-2">
                    <Button variant="ghost" size="icon" onClick={() => router.back()}>
                        <ArrowLeft className="h-4 w-4" />
                    </Button>
                    <h2 className="text-2xl font-bold">Deployment not found</h2>
                </div>
                <p>The deployment you're looking for doesn't exist or you don't have access to it.</p>
                <Button onClick={() => router.back()}>Back to Deployments</Button>
            </div>
        )
    }

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                    <Button variant="ghost" size="icon" onClick={() => router.back()}>
                        <ArrowLeft className="h-4 w-4" />
                    </Button>
                    <h2 className="text-2xl font-bold">Deployment {deployment.id}</h2>
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
                    <Button variant="outline" className="gap-2" asChild>
                        <a href={deployment.url} target="_blank" rel="noopener noreferrer">
                            <ExternalLink className="h-4 w-4" />
                            Visit
                        </a>
                    </Button>
                    <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                            <Button variant="outline">
                                <MoreVertical className="h-4 w-4" />
                            </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                            <DropdownMenuItem>Promote to Production</DropdownMenuItem>
                            <DropdownMenuItem>Redeploy</DropdownMenuItem>
                            <DropdownMenuSeparator />
                            <DropdownMenuItem className="text-destructive">Delete Deployment</DropdownMenuItem>
                        </DropdownMenuContent>
                    </DropdownMenu>
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
                                {getStatusDot(deployment.status)}
                                {deployment.status}
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
                                {deployment.createdAt}
                            </div>
                        </div>
                        <div className="flex justify-between items-center">
                            <div className="text-sm text-muted-foreground">Duration</div>
                            <div className="flex items-center gap-2 font-medium">
                                <Clock className="h-4 w-4 text-muted-foreground" />
                                {deployment.duration}
                            </div>
                        </div>
                        <div className="flex justify-between items-center">
                            <div className="text-sm text-muted-foreground">URL</div>
                            <div className="flex items-center gap-2 font-medium">
                                <a
                                    href={deployment.url}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="text-blue-500 hover:underline"
                                >
                                    {deployment.url}
                                </a>
                                <Button variant="ghost" size="icon" onClick={() => copyToClipboard(deployment.url)}>
                                    <Copy className="h-4 w-4" />
                                </Button>
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
                                <span className="font-mono">{deployment.commitHash}</span>
                                <Button variant="ghost" size="icon" onClick={() => copyToClipboard(deployment.commitHash)}>
                                    <Copy className="h-4 w-4" />
                                </Button>
                            </div>
                        </div>
                        <div className="flex justify-between items-center">
                            <div className="text-sm text-muted-foreground">Message</div>
                            <div className="font-medium">{deployment.commitMessage}</div>
                        </div>
                        <div className="flex justify-between items-center">
                            <div className="text-sm text-muted-foreground">Creator</div>
                            <div className="font-medium">{deployment.creator}</div>
                        </div>
                    </CardContent>
                </Card>
            </div>

            <Card>
                <CardHeader>
                    <CardTitle>Build Configuration</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                        <div className="flex flex-col">
                            <div className="text-sm text-muted-foreground">Framework</div>
                            <div className="font-medium">{deployment.framework}</div>
                        </div>
                        <div className="flex flex-col">
                            <div className="text-sm text-muted-foreground">Node.js Version</div>
                            <div className="font-medium">{deployment.nodeVersion}</div>
                        </div>
                        <div className="flex flex-col">
                            <div className="text-sm text-muted-foreground">Region</div>
                            <div className="font-medium">{deployment.region}</div>
                        </div>
                    </div>
                </CardContent>
            </Card>

            <Tabs defaultValue="build" className="w-full">
                <TabsList>
                    <TabsTrigger value="build">Build Logs</TabsTrigger>
                    <TabsTrigger value="runtime">Runtime Logs</TabsTrigger>
                </TabsList>
                <TabsContent value="build" className="mt-4">
                    <Card>
                        <CardHeader className="pb-2">
                            <CardTitle className="text-lg flex justify-between">
                                <span>Build Logs</span>
                                <Button variant="ghost" size="sm" onClick={() => copyToClipboard(deployment.logs.build.join("\n"))}>
                                    <Copy className="h-4 w-4 mr-2" />
                                    Copy
                                </Button>
                            </CardTitle>
                            <CardDescription>Logs from the build process</CardDescription>
                        </CardHeader>
                        <CardContent>
                            <div className="bg-black text-green-400 font-mono text-sm p-4 rounded-md h-[400px] overflow-y-auto">
                                {deployment.logs.build.map((log, index) => (
                                    <div key={index} className="whitespace-pre-wrap mb-1">
                                        {log}
                                    </div>
                                ))}
                            </div>
                        </CardContent>
                    </Card>
                </TabsContent>
                <TabsContent value="runtime" className="mt-4">
                    <Card>
                        <CardHeader className="pb-2">
                            <CardTitle className="text-lg flex justify-between">
                                <span>Runtime Logs</span>
                                <Button variant="ghost" size="sm" onClick={() => copyToClipboard(deployment.logs.runtime.join("\n"))}>
                                    <Copy className="h-4 w-4 mr-2" />
                                    Copy
                                </Button>
                            </CardTitle>
                            <CardDescription>Logs from the running application</CardDescription>
                        </CardHeader>
                        <CardContent>
                            <div className="bg-black text-green-400 font-mono text-sm p-4 rounded-md h-[400px] overflow-y-auto">
                                {deployment.logs.runtime.map((log, index) => (
                                    <div key={index} className="whitespace-pre-wrap mb-1">
                                        {log}
                                    </div>
                                ))}
                            </div>
                        </CardContent>
                    </Card>
                </TabsContent>
            </Tabs>
        </div>
    )
}
