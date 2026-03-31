import { createFileRoute, getRouteApi } from '@tanstack/react-router'
import { apiBaseUrl } from '@/api-client'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useTimeAgo } from '@/hooks/useTimeAgo'
import { useModal } from '@/hooks/use-modal'
import { cn } from '@/lib/utils'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import {
    GitBranchIcon,
    Globe,
    Plus,
    RefreshCw,
    Rocket,
    Trash2,
} from 'lucide-react'
import { useState } from 'react'

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/environments/',
)({
    component: RouteComponent,
})

interface EnvironmentDto {
    id: string
    name: string
    type: 'Production' | 'Preview'
    branches: string[]
    autoDeploy: boolean
    pullRequestNumber: number | null
    subdomain: string | null
    createdAtUtc: string
    lastDeploymentAtUtc: string | null
}

interface GetEnvironmentsResponse {
    environments: EnvironmentDto[]
}

function useEnvironments(projectId: string) {
    return useQuery<GetEnvironmentsResponse>({
        queryKey: ['environments', projectId],
        queryFn: async () => {
            const res = await fetch(
                `${apiBaseUrl}/v1/projects/${encodeURIComponent(projectId)}/environments`,
                { credentials: 'include' },
            )
            if (!res.ok) throw new Error('Failed to fetch environments')
            return res.json()
        },
        refetchInterval: 30000,
    })
}

function RouteComponent() {
    const { projectId } = getRouteApi('/_auth/_dashboard/projects/$projectId').useLoaderData()
    const { data, refetch, isLoading } = useEnvironments(projectId)
    const queryClient = useQueryClient()

    const [rotation, setRotation] = useState(0)
    const handleRefresh = () => {
        refetch()
        setRotation((prev) => prev + 360)
    }

    const environments = data?.environments ?? []
    const productionEnv = environments.find((e) => e.type === 'Production')
    const previewEnvs = environments.filter((e) => e.type === 'Preview')

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex flex-col gap-4 sm:flex-row sm:justify-between sm:items-center">
                <div className="flex items-center gap-3">
                    <h2 className="text-3xl font-bold tracking-tight">Environments</h2>
                    <Button
                        variant="ghost"
                        size="icon"
                        className="rounded-full h-8 w-8"
                        onClick={handleRefresh}
                        asChild
                    >
                        <motion.div
                            animate={{ rotate: rotation }}
                            transition={{ duration: 0.5, ease: 'easeInOut' }}
                        >
                            <RefreshCw className="h-4 w-4" />
                        </motion.div>
                    </Button>
                </div>
                <CreatePreviewDialog
                    projectId={projectId}
                    onCreated={() => {
                        queryClient.invalidateQueries({ queryKey: ['environments', projectId] })
                    }}
                />
            </div>

            {/* Loading state */}
            {isLoading && (
                <div className="flex items-center justify-center py-16">
                    <div className="animate-spin h-6 w-6 border-2 border-muted-foreground border-t-transparent rounded-full" />
                </div>
            )}

            {/* Production environment */}
            {productionEnv && (
                <motion.div
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    transition={{ duration: 0.2 }}
                >
                    <EnvironmentCard
                        environment={productionEnv}
                        projectId={projectId}
                    />
                </motion.div>
            )}

            {/* Preview environments */}
            {previewEnvs.length > 0 && (
                <div className="space-y-4">
                    <h3 className="text-lg font-semibold text-muted-foreground">
                        Preview Environments
                    </h3>
                    <div className="space-y-2">
                        {previewEnvs.map((env, index) => (
                            <motion.div
                                key={env.id}
                                initial={{ opacity: 0 }}
                                animate={{ opacity: 1 }}
                                transition={{ duration: 0.2, delay: index * 0.05 }}
                            >
                                <EnvironmentCard
                                    environment={env}
                                    projectId={projectId}
                                    onDeleted={() => {
                                        queryClient.invalidateQueries({
                                            queryKey: ['environments', projectId],
                                        })
                                    }}
                                />
                            </motion.div>
                        ))}
                    </div>
                </div>
            )}

            {/* Empty state for previews */}
            {!isLoading && previewEnvs.length === 0 && (
                <div className="flex flex-col items-center justify-center py-12 text-center">
                    <div className="rounded-full bg-muted p-4 mb-4">
                        <Rocket className="h-8 w-8 text-muted-foreground" />
                    </div>
                    <p className="text-lg font-medium mb-1">No preview environments</p>
                    <p className="text-sm text-muted-foreground mb-4">
                        Create a preview environment to test changes on a separate branch,
                        or they will be created automatically from pull requests.
                    </p>
                </div>
            )}
        </div>
    )
}

function EnvironmentCard({
    environment,
    projectId,
    onDeleted,
}: {
    environment: EnvironmentDto
    projectId: string
    onDeleted?: () => void
}) {
    const modal = useModal()
    const isProduction = environment.type === 'Production'

    const { mutateAsync: deleteEnvironment, isPending: isDeleting } = useMutation({
        mutationFn: async () => {
            const res = await fetch(
                `${apiBaseUrl}/v1/projects/${encodeURIComponent(projectId)}/environments/${encodeURIComponent(environment.id)}`,
                { method: 'DELETE', credentials: 'include' },
            )
            if (!res.ok) {
                const text = await res.text()
                throw new Error(text || 'Failed to delete environment')
            }
            return res.json()
        },
    })

    const handleDelete = async () => {
        const confirmed = await modal.confirm(
            'Delete Environment',
            `Are you sure you want to delete "${environment.name}"? This will remove the preview environment and its configuration.`,
            {
                confirmText: 'Delete',
                cancelText: 'Cancel',
            },
        )
        if (confirmed) {
            await deleteEnvironment()
            onDeleted?.()
        }
    }

    return (
        <div
            className={cn(
                'rounded-xl border bg-zinc-900/50 backdrop-blur-sm p-5 transition-colors',
                isProduction
                    ? 'border-emerald-500/30 hover:border-emerald-500/50'
                    : 'border-zinc-800 hover:border-zinc-700',
            )}
        >
            <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                {/* Left side: name, badge, branch */}
                <div className="flex flex-col gap-2">
                    <div className="flex items-center gap-2.5">
                        <h3 className="text-base font-semibold">{environment.name}</h3>
                        <Badge
                            className={cn(
                                'border-0 text-xs',
                                isProduction
                                    ? 'bg-emerald-500/15 text-emerald-400'
                                    : 'bg-blue-500/15 text-blue-400',
                            )}
                        >
                            {environment.type}
                        </Badge>
                        {environment.autoDeploy && (
                            <Badge variant="outline" className="text-xs border-zinc-700 text-zinc-400">
                                Auto-deploy
                            </Badge>
                        )}
                    </div>

                    <div className="flex flex-wrap items-center gap-3 text-sm text-muted-foreground">
                        {environment.branches.map((branch) => (
                            <span key={branch} className="flex items-center gap-1.5">
                                <GitBranchIcon className="h-3.5 w-3.5" />
                                {branch}
                            </span>
                        ))}

                        {environment.subdomain && (
                            <span className="flex items-center gap-1.5">
                                <Globe className="h-3.5 w-3.5" />
                                {environment.subdomain}
                            </span>
                        )}

                        {environment.pullRequestNumber && (
                            <span className="text-xs text-zinc-500">
                                PR #{environment.pullRequestNumber}
                            </span>
                        )}
                    </div>

                    {environment.lastDeploymentAtUtc && (
                        <LastDeploymentTime date={environment.lastDeploymentAtUtc} />
                    )}
                </div>

                {/* Right side: actions */}
                <div className="flex items-center gap-2 shrink-0">
                    {!isProduction && (
                        <Button
                            variant="ghost"
                            size="sm"
                            className="text-red-400 hover:text-red-300 hover:bg-red-500/10"
                            onClick={handleDelete}
                            disabled={isDeleting}
                        >
                            <Trash2 className="h-3.5 w-3.5 mr-1.5" />
                            Delete
                        </Button>
                    )}
                </div>
            </div>
        </div>
    )
}

function LastDeploymentTime({ date }: { date: string }) {
    const timeAgo = useTimeAgo(date)
    return (
        <span className="text-xs text-zinc-500">
            Last deployed {timeAgo}
        </span>
    )
}

function CreatePreviewDialog({
    projectId,
    onCreated,
}: {
    projectId: string
    onCreated: () => void
}) {
    const [open, setOpen] = useState(false)
    const [name, setName] = useState('')
    const [branch, setBranch] = useState('')

    const { mutateAsync: createEnvironment, isPending } = useMutation({
        mutationFn: async () => {
            const res = await fetch(
                `${apiBaseUrl}/v1/projects/${encodeURIComponent(projectId)}/environments`,
                {
                    method: 'POST',
                    credentials: 'include',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        name,
                        branch,
                        type: 1, // Preview
                    }),
                },
            )
            if (!res.ok) {
                const text = await res.text()
                throw new Error(text || 'Failed to create environment')
            }
            return res.json()
        },
    })

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        await createEnvironment()
        setOpen(false)
        setName('')
        setBranch('')
        onCreated()
    }

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                <Button size="sm">
                    <Plus className="h-4 w-4 mr-1.5" />
                    Create Preview
                </Button>
            </DialogTrigger>
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>Create Preview Environment</DialogTitle>
                    <DialogDescription>
                        Create a new preview environment linked to a branch. Pushes to this
                        branch will automatically trigger deployments.
                    </DialogDescription>
                </DialogHeader>
                <form onSubmit={handleSubmit} className="space-y-4">
                    <div className="space-y-2">
                        <Label htmlFor="env-name">Environment Name</Label>
                        <Input
                            id="env-name"
                            placeholder="e.g., feature-auth"
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            required
                        />
                    </div>
                    <div className="space-y-2">
                        <Label htmlFor="env-branch">Branch</Label>
                        <Input
                            id="env-branch"
                            placeholder="e.g., feat/auth-redesign"
                            value={branch}
                            onChange={(e) => setBranch(e.target.value)}
                            required
                        />
                    </div>
                    <DialogFooter>
                        <Button
                            type="button"
                            variant="outline"
                            onClick={() => setOpen(false)}
                        >
                            Cancel
                        </Button>
                        <Button type="submit" disabled={isPending || !name || !branch}>
                            {isPending ? 'Creating...' : 'Create'}
                        </Button>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    )
}
