import { createFileRoute, getRouteApi, Link } from '@tanstack/react-router'
import { api } from '@/api-client'
import { Badge } from '@/components/ui/badge'
import LoadingButton from '@/components/loading-button'
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
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select'
import { useTimeAgo } from '@/hooks/useTimeAgo'
import { useModal } from '@/hooks/use-modal'
import { useToast } from '@/hooks/use-toast'
import { cn } from '@/lib/utils'
import { motion } from 'framer-motion'
import { formatDistanceToNow } from 'date-fns'
import {
    AlertTriangle,
    Clock,
    Cpu,
    GitBranch,
    Globe,
    HardDrive,
    MemoryStick,
    Plus,
    RefreshCw,
    Rocket,
    Server as ServerIcon,
    Trash2,
    Zap,
} from 'lucide-react'
import { useRegisterActions } from '@/lib/command-palette/action-registry'
import { useEffect, useState } from 'react'

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/environments/',
)({
    component: RouteComponent,
})

// --- Page ---

function RouteComponent() {
    const { projectId } = getRouteApi(
        '/_auth/_dashboard/projects/$projectId',
    ).useLoaderData()

    const { data, refetch, isLoading, error } =
        api.v1.getProjectsProjectIdEnvironments.useQuery(
            { path: { projectId } },
            { refetchInterval: 30_000 },
        )
    const { data: tiersData } = api.v1.getServersTiers.useQuery()
    const tiers = tiersData?.tiers ?? []

    const [rotation, setRotation] = useState(0)
    const handleRefresh = () => {
        refetch()
        setRotation((prev) => prev + 360)
    }

    const environments = data?.environments ?? []
    const productionEnv = environments.find((e) => e.type === 'Production')
    const previewEnvs = environments.filter((e) => e.type === 'Preview')

    const totalMonthlyCost = environments.reduce(
        (sum, e) => sum + (e.monthlyPrice ?? 0),
        0,
    )

    const [createDialogOpen, setCreateDialogOpen] = useState(false)

    const invalidate = () =>
        api.v1.getProjectsProjectIdEnvironments.invalidateQueries({
            parameters: { path: { projectId } },
        })

    useRegisterActions([
        {
            id: "create-new-environment",
            name: "Create new environment",
            icon: Plus,
            group: "action",
            perform: () => setCreateDialogOpen(true),
        },
        {
            id: "refresh-environments",
            name: "Refresh environments",
            icon: RefreshCw,
            group: "action",
            perform: handleRefresh,
        },
    ], [])

    return (
        <div className="space-y-8">
            {/* Header */}
            <div className="flex flex-col gap-4 sm:flex-row sm:justify-between sm:items-end">
                <div>
                    <div className="flex items-center gap-3">
                        <h2 className="text-3xl font-bold tracking-tight">
                            Environments
                        </h2>
                        <Button
                            variant="ghost"
                            size="icon"
                            className="rounded-full h-8 w-8"
                            onClick={handleRefresh}
                            asChild
                        >
                            <motion.div
                                animate={{ rotate: rotation }}
                                transition={{
                                    duration: 0.5,
                                    ease: 'easeInOut',
                                }}
                            >
                                <RefreshCw className="h-4 w-4" />
                            </motion.div>
                        </Button>
                    </div>
                    {totalMonthlyCost > 0 && (
                        <p className="text-sm text-muted-foreground mt-1">
                            Total: ${totalMonthlyCost.toFixed(0)}/month across{' '}
                            {environments.length} environment
                            {environments.length !== 1 ? 's' : ''}
                        </p>
                    )}
                </div>
                <CreateEnvironmentDialog
                    projectId={projectId}
                    usedBranches={environments.flatMap((e) => e.branches ?? [])}
                    productionServerId={productionEnv?.server?.id ?? null}
                    onCreated={invalidate}
                    open={createDialogOpen}
                    onOpenChange={setCreateDialogOpen}
                />
            </div>

            {/* Loading */}
            {isLoading && (
                <div className="flex items-center justify-center py-16">
                    <div className="animate-spin h-6 w-6 border-2 border-muted-foreground border-t-transparent rounded-full" />
                </div>
            )}

            {/* Error */}
            {!!error && !isLoading && (
                <div className="flex flex-col items-center justify-center py-12 text-center">
                    <p className="text-sm text-red-400 mb-2">
                        Failed to load environments
                    </p>
                    <Button
                        variant="outline"
                        size="sm"
                        onClick={() => refetch()}
                    >
                        Try again
                    </Button>
                </div>
            )}

            {/* Production */}
            {productionEnv && (
                <motion.div
                    initial={{ opacity: 0, y: 8 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.25 }}
                >
                    <EnvironmentCard
                        environment={productionEnv}
                        projectId={projectId}
                        tiers={tiers}
                        onChanged={invalidate}
                    />
                </motion.div>
            )}

            {/* Previews */}
            {previewEnvs.length > 0 && (
                <div className="space-y-3">
                    <h3 className="text-sm font-medium text-muted-foreground uppercase tracking-wider">
                        Other Environments
                    </h3>
                    <div className="space-y-2">
                        {previewEnvs.map((env, i) => (
                            <motion.div
                                key={env.id}
                                initial={{ opacity: 0, y: 8 }}
                                animate={{ opacity: 1, y: 0 }}
                                transition={{
                                    duration: 0.25,
                                    delay: i * 0.06,
                                }}
                            >
                                <EnvironmentCard
                                    environment={env}
                                    projectId={projectId}
                                    tiers={tiers}
                                    onChanged={invalidate}
                                />
                            </motion.div>
                        ))}
                    </div>
                </div>
            )}

            {/* Empty state */}
            {!isLoading && !error && previewEnvs.length === 0 && (
                <motion.div
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    transition={{ duration: 0.3, delay: 0.15 }}
                    className="flex flex-col items-center justify-center py-16 text-center"
                >
                    <div className="rounded-2xl bg-zinc-800/50 p-5 mb-5">
                        <Rocket className="h-10 w-10 text-zinc-500" />
                    </div>
                    <p className="text-lg font-medium mb-1">
                        No additional environments
                    </p>
                    <p className="text-sm text-muted-foreground max-w-sm">
                        Create an environment to deploy and test a different
                        branch on its own server with its own URL.
                    </p>
                </motion.div>
            )}
        </div>
    )
}

// --- Environment Card ---

type EnvironmentDto = NonNullable<
    NonNullable<
        Awaited<ReturnType<typeof api.v1.getProjectsProjectIdEnvironments>>
    >['data']
>['environments'] extends (infer U)[] | null
    ? U
    : never

type TierDto = NonNullable<
    NonNullable<
        Awaited<ReturnType<typeof api.v1.getServersTiers>>
    >['data']
>['tiers'] extends (infer U)[] | null
    ? U
    : never

function EnvironmentCard({
    environment: env,
    projectId,
    tiers,
    onChanged,
}: {
    environment: EnvironmentDto
    projectId: string
    tiers: TierDto[]
    onChanged: () => void
}) {
    const modal = useModal()
    const { toast } = useToast()
    const isProduction = env.type === 'Production'

    const previewUrl = env.url ? `https://${env.url}` : null

    const tier = env.serverTierId
        ? tiers.find((t) => t.id?.value === env.serverTierId)
        : undefined

    const { mutateAsync: deleteEnv, isPending: isDeleting } =
        api.v1.deleteProjectsProjectIdEnvironmentsEnvironmentId.useMutation({
            path: { projectId, environmentId: env.id },
        })

    const { mutateAsync: deployAsync, isPending: isDeploying } =
        api.v1.postProjectsProjectIdDeploymentsDeployFromLatestCommit.useMutation(
            { path: { projectId }, query: { environmentId: env.id } },
            {
                onSuccess: () => {
                    toast({
                        title: 'Deployment started',
                        description: `Deploying latest commit to "${env.name ?? env.id}".`,
                    })
                    onChanged()
                },
            },
        )

    const handleDelete = async () => {
        const name = env.name ?? env.id
        const confirmed = await modal.confirm(
            'Delete Environment',
            `Delete "${name}"? This will stop the server and cancel its subscription. You'll receive a prorated credit for the remaining time.`,
            { confirmText: 'Delete', variant: 'destructive' },
        )
        if (!confirmed) return
        await deleteEnv()
        toast({
            title: 'Environment deleted',
            description: `"${name}" has been removed.`,
        })
        onChanged()
    }

    return (
        <Link
            to="/projects/$projectId/environments/$environmentId"
            params={{ projectId, environmentId: env.id }}
            className={cn(
                'block group rounded-xl border backdrop-blur-sm transition-all cursor-pointer',
                isProduction
                    ? 'border-emerald-500/20 bg-emerald-950/10 hover:border-emerald-500/35'
                    : 'border-zinc-800 bg-zinc-900/40 hover:border-zinc-700',
            )}
        >
            {/* Main row */}
            <div className="flex flex-col gap-4 p-5 sm:flex-row sm:items-start sm:justify-between">
                {/* Left: info */}
                <div className="flex-1 min-w-0 space-y-3">
                    {/* Name + badges */}
                    <div className="flex flex-wrap items-center gap-2">
                        <h3 className="text-base font-semibold truncate">
                            {env.name ?? (isProduction ? 'Production' : env.id)}
                        </h3>

                        <Badge
                            className={cn(
                                'border-0 text-[11px] font-medium',
                                isProduction
                                    ? 'bg-emerald-500/15 text-emerald-400'
                                    : 'bg-sky-500/12 text-sky-400',
                            )}
                        >
                            {env.type}
                        </Badge>

                        {env.serverTierId && (
                            <Badge
                                variant="outline"
                                className="text-[11px] border-zinc-700 text-zinc-400 font-mono"
                            >
                                {env.serverTierId}
                            </Badge>
                        )}

                        {env.billingStatus && env.billingStatus !== 'Active' && (
                            <Badge
                                className={cn(
                                    'border-0 text-[10px] font-medium',
                                    env.billingStatus === 'Trialing' && 'bg-amber-500/15 text-amber-400',
                                    env.billingStatus === 'Suspended' && 'bg-red-500/15 text-red-400',
                                    env.billingStatus === 'Cancelled' && 'bg-zinc-500/15 text-zinc-400',
                                )}
                            >
                                {env.billingStatus === 'Trialing' ? 'Trial' : env.billingStatus}
                            </Badge>
                        )}
                    </div>

                    {/* Meta row */}
                    <div className="flex flex-wrap items-center gap-x-4 gap-y-1.5 text-[13px] text-muted-foreground">
                        {env.branches && env.branches.length > 0 && (
                            <span className="flex items-center gap-1.5">
                                <GitBranch className="h-3.5 w-3.5 shrink-0" />
                                <span className="font-mono text-xs">
                                    {env.branches.join(', ')}
                                </span>
                            </span>
                        )}

                        {previewUrl && (
                            <a
                                href={previewUrl}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="flex items-center gap-1.5 hover:text-foreground transition-colors"
                                onClick={(e) => e.stopPropagation()}
                            >
                                <Globe className="h-3.5 w-3.5 shrink-0" />
                                <span className="truncate max-w-[220px]">
                                    {env.url}
                                </span>
                            </a>
                        )}

                        {env.lastDeploymentAtUtc && (
                            <span className="flex items-center gap-1.5">
                                <Clock className="h-3.5 w-3.5 shrink-0" />
                                <LastDeploy date={env.lastDeploymentAtUtc} />
                            </span>
                        )}

                        <span className="flex items-center gap-1.5">
                            <ServerIcon className="h-3.5 w-3.5 shrink-0" />
                            {env.server ? (
                                <>
                                    <span className="font-mono text-xs">{env.server.hostName}</span>
                                    {env.server.locationIsoCountryCode && (
                                        <span className="text-xs uppercase">
                                            ({env.server.locationIsoCountryCode})
                                        </span>
                                    )}
                                </>
                            ) : (
                                <span className="italic text-xs">Not yet allocated</span>
                            )}
                        </span>

                        {env.healthStatus === 'Unhealthy' && (
                            <span className="inline-flex items-center gap-1 text-xs text-red-700">
                                <AlertTriangle className="h-3 w-3" />
                                Not responding
                                {env.lastHealthCheckAtUtc && (
                                    <span className="text-muted-foreground">
                                        · checked {formatDistanceToNow(new Date(env.lastHealthCheckAtUtc))} ago
                                    </span>
                                )}
                            </span>
                        )}
                    </div>

                    {/* Auto-deploy status */}
                    {env.effectiveAutoDeploy && (
                        <Badge
                            variant="outline"
                            className="text-[11px] border-zinc-700 text-zinc-400"
                        >
                            Auto-deploy
                        </Badge>
                    )}
                </div>

                {/* Right: cost + actions */}
                <div className="flex items-center gap-3 shrink-0 sm:flex-col sm:items-end sm:gap-2">
                    {/* Price */}
                    {env.monthlyPrice != null && env.monthlyPrice > 0 && (
                        <div className="text-right">
                            <span className="text-xl font-bold tabular-nums">
                                ${env.monthlyPrice.toFixed(0)}
                            </span>
                            <span className="text-xs text-muted-foreground ml-0.5">
                                /mo
                            </span>
                        </div>
                    )}

                    {/* Actions */}
                    <div className="flex items-center gap-1.5">
                        <LoadingButton
                            variant="outline"
                            size="sm"
                            className="h-8 text-xs"
                            isLoading={isDeploying}
                            onClick={(e) => {
                                e.stopPropagation()
                                e.preventDefault()
                                deployAsync()
                            }}
                        >
                            <Rocket className="h-3.5 w-3.5 mr-1" />
                            Deploy
                        </LoadingButton>
                        {!isProduction && (
                            <Button
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8 text-muted-foreground hover:text-red-400 hover:bg-red-500/10"
                                onClick={(e) => { e.stopPropagation(); e.preventDefault(); handleDelete() }}
                                disabled={isDeleting}
                                title="Delete environment"
                            >
                                <Trash2 className="h-3.5 w-3.5" />
                            </Button>
                        )}
                    </div>
                </div>
            </div>

            {/* Tier specs bar */}
            {env.serverTierId && tier && (
                <div className="border-t border-zinc-800/60 px-5 py-2.5 flex items-center gap-4 text-[11px] text-zinc-500">
                    <TierSpecBadge
                        icon={MemoryStick}
                        label={`${tier.ramGb} GB`}
                    />
                    <TierSpecBadge
                        icon={Cpu}
                        label={`${tier.cores} Core${tier.cores !== 1 ? 's' : ''}`}
                    />
                    <TierSpecBadge
                        icon={HardDrive}
                        label={`${tier.diskGb} GB`}
                    />
                </div>
            )}
        </Link>
    )
}

function TierSpecBadge({
    icon: Icon,
    label,
}: {
    icon: typeof MemoryStick
    label: string
}) {
    return (
        <span className="flex items-center gap-1">
            <Icon className="h-3 w-3" />
            {label}
        </span>
    )
}

function LastDeploy({ date }: { date: string }) {
    const timeAgo = useTimeAgo(date)
    return <span>{timeAgo}</span>
}

// --- Create Environment Dialog ---

function branchToEnvName(branch: string): string {
    const parts = branch.split('/')
    const last = parts.at(-1) ?? branch
    return last
        .replace(/[^a-zA-Z0-9-]/g, '-')
        .replace(/-+/g, '-')
        .replace(/^-|-$/g, '')
        .toLowerCase()
}

const MANUAL_BRANCH = '__manual__'

function CreateEnvironmentDialog({
    projectId,
    usedBranches,
    productionServerId,
    onCreated,
    open,
    onOpenChange,
}: {
    projectId: string
    usedBranches: string[]
    productionServerId: string | null | undefined
    onCreated: () => void
    open: boolean
    onOpenChange: (open: boolean) => void
}) {
    const setOpen = onOpenChange
    const [name, setName] = useState('')
    const [nameManuallyEdited, setNameManuallyEdited] = useState(false)
    const [branch, setBranch] = useState('')
    const [isManualBranch, setIsManualBranch] = useState(false)
    const [selectedTier, setSelectedTier] = useState('T0')
    const [selectedServerId, setSelectedServerId] = useState<string | null>(null)
    const [branchMode, setBranchMode] = useState<'link' | 'later'>('link')
    const { toast } = useToast()

    const { data: branchesData } = api.v1.getProjectsProjectIdEnvironmentsBranches.useQuery(
        { path: { projectId } },
        { enabled: open, staleTime: 60_000 },
    )
    const branches = branchesData?.branches ?? []

    const { data: serversData } = api.v1.getServers.useQuery(
        undefined,
        { enabled: open, staleTime: 60_000 },
    )
    const servers = serversData?.servers ?? []

    // Default the picker to the project's production env's server when the
    // dialog opens — the common case ("same server as prod") then needs no
    // clicks. Tracking the prior productionServerId lets a re-open after the
    // user changed projects pick up the new default.
    const [defaultedFrom, setDefaultedFrom] = useState<string | null>(null)
    useEffect(() => {
        if (!open) return
        if (productionServerId && defaultedFrom !== productionServerId) {
            setSelectedServerId(productionServerId)
            setDefaultedFrom(productionServerId)
        }
    }, [open, productionServerId, defaultedFrom])

    const handleBranchSelect = (value: string) => {
        if (value === MANUAL_BRANCH) {
            setIsManualBranch(true)
            setBranch('')
            return
        }
        setIsManualBranch(false)
        setBranch(value)
        if (!nameManuallyEdited) {
            setName(branchToEnvName(value))
        }
    }

    const { data: tiersData } = api.v1.getServersTiers.useQuery()
    const tiers = tiersData?.tiers ?? []

    const selectedTierData = tiers.find((t) => t.id?.value === selectedTier)
    const monthlyPrice = selectedTierData?.price?.value ?? 5

    const { mutateAsync: createEnvironment, isPending } =
        api.v1.postProjectsProjectIdEnvironments.useMutation({
            path: { projectId },
        })

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        try {
            await createEnvironment({
                name,
                branch: branchMode === 'link' ? branch : null,
                type: 'Preview',
                tier: selectedTier,
                // Null lets the backend allocator pick (its "best server in
                // location" heuristic). The picker sends an explicit ServerId
                // when the user actively picked one.
                serverId: selectedServerId,
            })
            toast({
                title: 'Environment created',
                description: `"${name}" is being provisioned on ${selectedTier}.`,
            })
            setOpen(false)
            resetForm()
            onCreated()
        } catch (err) {
            toast({
                title: 'Failed to create environment',
                description:
                    err instanceof Error ? err.message : 'Unknown error',
                variant: 'destructive',
            })
        }
    }

    const resetForm = () => {
        setName('')
        setBranch('')
        setNameManuallyEdited(false)
        setIsManualBranch(false)
        setSelectedTier('T0')
        setBranchMode('link')
        setSelectedServerId(productionServerId ?? null)
        setDefaultedFrom(productionServerId ?? null)
    }

    return (
        <Dialog
            open={open}
            onOpenChange={(v) => {
                onOpenChange(v)
                if (!v) resetForm()
            }}
        >
            <DialogTrigger asChild>
                <Button size="sm">
                    <Plus className="h-4 w-4 mr-1.5" />
                    New Environment
                </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-xl">
                <DialogHeader>
                    <DialogTitle>Create Environment</DialogTitle>
                    <DialogDescription>
                        Each environment runs on its own server and is billed
                        separately.
                    </DialogDescription>
                </DialogHeader>

                <form onSubmit={handleSubmit} className="space-y-5">
                    {/* Branch + Name */}
                    <div className="space-y-4">
                        <div className="space-y-4">
                            <div className="space-y-2">
                                <Label>Branch</Label>
                                <div className="flex gap-2">
                                    <Button
                                        type="button"
                                        variant={branchMode === 'link' ? 'default' : 'outline'}
                                        size="sm"
                                        className="flex-1"
                                        onClick={() => setBranchMode('link')}
                                    >
                                        <GitBranch className="h-3.5 w-3.5 mr-1.5" />
                                        Link to branch
                                    </Button>
                                    <Button
                                        type="button"
                                        variant={branchMode === 'later' ? 'default' : 'outline'}
                                        size="sm"
                                        className="flex-1"
                                        onClick={() => {
                                            setBranchMode('later')
                                            setBranch('')
                                            setIsManualBranch(false)
                                        }}
                                    >
                                        Specify later
                                    </Button>
                                </div>
                            </div>

                            {branchMode === 'link' && (
                                <div className="space-y-2">
                                    {branches.length > 0 && !isManualBranch ? (
                                        <Select
                                            value={branch}
                                            onValueChange={handleBranchSelect}
                                        >
                                            <SelectTrigger>
                                                <SelectValue placeholder="Select a branch" />
                                            </SelectTrigger>
                                            <SelectContent>
                                                {branches.map((b) => {
                                                    const isUsed = usedBranches.includes(b)
                                                    return (
                                                        <SelectItem
                                                            key={b}
                                                            value={b}
                                                            disabled={isUsed}
                                                        >
                                                            <span
                                                                className={cn(
                                                                    'font-mono text-xs',
                                                                    isUsed && 'text-muted-foreground line-through',
                                                                )}
                                                            >
                                                                {b}
                                                                {isUsed && ' (in use)'}
                                                            </span>
                                                        </SelectItem>
                                                    )
                                                })}
                                                <SelectItem value={MANUAL_BRANCH}>
                                                    <span className="text-muted-foreground">
                                                        Enter manually...
                                                    </span>
                                                </SelectItem>
                                            </SelectContent>
                                        </Select>
                                    ) : (
                                        <div className="flex gap-2">
                                            <Input
                                                placeholder="feature/my-branch"
                                                value={branch}
                                                onChange={(e) => {
                                                    setBranch(e.target.value)
                                                    if (!nameManuallyEdited && e.target.value) {
                                                        setName(branchToEnvName(e.target.value))
                                                    }
                                                }}
                                                required
                                            />
                                            {branches.length > 0 && (
                                                <Button
                                                    type="button"
                                                    variant="outline"
                                                    size="sm"
                                                    className="shrink-0"
                                                    onClick={() => {
                                                        setIsManualBranch(false)
                                                        setBranch('')
                                                    }}
                                                >
                                                    <GitBranch className="h-3.5 w-3.5" />
                                                </Button>
                                            )}
                                        </div>
                                    )}
                                </div>
                            )}
                        </div>
                        <div className="space-y-2">
                            <Label htmlFor="env-name">Name</Label>
                            <Input
                                id="env-name"
                                placeholder="staging"
                                value={name}
                                onChange={(e) => {
                                    setName(e.target.value)
                                    setNameManuallyEdited(true)
                                }}
                                required
                            />
                        </div>
                    </div>

                    {/* Tier selector */}
                    <div className="space-y-2.5">
                        <Label>Server Tier</Label>
                        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-2">
                            {tiers.map((t) => {
                                const isSelected =
                                    selectedTier === t.id.value
                                return (
                                    <button
                                        key={t.id.value!}
                                        type="button"
                                        onClick={() =>
                                            setSelectedTier(t.id.value!)
                                        }
                                        className={cn(
                                            'relative flex flex-col gap-2 rounded-lg border p-3 text-left transition-all',
                                            isSelected
                                                ? 'border-primary bg-primary/5 ring-1 ring-primary'
                                                : 'border-zinc-800 bg-zinc-900/30 hover:border-zinc-700',
                                        )}
                                    >
                                        <span className="font-mono text-xs font-semibold">
                                            {t.id.value}
                                        </span>

                                        <div className="space-y-0.5 text-[10px] text-muted-foreground">
                                            <div className="flex items-center gap-1">
                                                <MemoryStick className="h-2.5 w-2.5" />
                                                {t.ramGb} GB
                                            </div>
                                            <div className="flex items-center gap-1">
                                                <Cpu className="h-2.5 w-2.5" />
                                                {t.cores} Core
                                                {t.cores !== 1 ? 's' : ''}
                                            </div>
                                            <div className="flex items-center gap-1">
                                                <HardDrive className="h-2.5 w-2.5" />
                                                {t.diskGb} GB
                                            </div>
                                        </div>

                                        <div className="flex items-baseline gap-0.5">
                                            <span className="text-sm font-bold">
                                                ${t.price.value}
                                            </span>
                                            <span className="text-[10px] text-muted-foreground">
                                                /mo
                                            </span>
                                        </div>

                                        {t.promotionalText && (
                                            <Badge
                                                variant="outline"
                                                className="text-[9px] border-amber-500/30 text-amber-500 bg-amber-500/10 px-1 py-0"
                                            >
                                                {t.promotionalText}
                                            </Badge>
                                        )}
                                    </button>
                                )
                            })}
                        </div>
                    </div>

                    {/* Server picker */}
                    {servers.length > 0 && (
                        <div className="space-y-2">
                            <Label htmlFor="env-server">Server</Label>
                            <Select
                                value={selectedServerId ?? ''}
                                onValueChange={(v) => setSelectedServerId(v || null)}
                            >
                                <SelectTrigger id="env-server">
                                    <SelectValue placeholder="Auto-allocate (best server in location)" />
                                </SelectTrigger>
                                <SelectContent>
                                    {servers.map((s) => (
                                        <SelectItem key={s.id} value={s.id}>
                                            <span className="flex items-center gap-2">
                                                <span className="font-mono text-xs">{s.hostName}</span>
                                                {s.locationIsoCountryCode && (
                                                    <span className="text-[10px] uppercase text-muted-foreground">
                                                        {s.locationIsoCountryCode}
                                                    </span>
                                                )}
                                                {s.id === productionServerId && (
                                                    <span className="text-[10px] text-emerald-400">
                                                        same as production
                                                    </span>
                                                )}
                                            </span>
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                            <p className="text-xs text-muted-foreground">
                                Each environment runs on one server. Leaving this
                                empty lets the backend pick the lightest-loaded
                                server in the project's location.
                            </p>
                        </div>
                    )}

                    {/* Cost summary + submit */}
                    <DialogFooter className="flex-col sm:flex-row gap-3 pt-2 border-t border-zinc-800">
                        <div className="flex items-center gap-2 text-sm text-muted-foreground mr-auto">
                            <Zap className="h-4 w-4 text-amber-500" />
                            <span>
                                This environment costs{' '}
                                <span className="text-foreground font-semibold">
                                    ${monthlyPrice.toFixed(2)}/month
                                </span>
                            </span>
                        </div>
                        <div className="flex gap-2">
                            <Button
                                type="button"
                                variant="outline"
                                onClick={() => setOpen(false)}
                            >
                                Cancel
                            </Button>
                            <Button
                                type="submit"
                                disabled={
                                    isPending || !name.trim() || (branchMode === 'link' && !branch.trim())
                                }
                            >
                                {isPending
                                    ? 'Creating...'
                                    : `Create — $${monthlyPrice}/mo`}
                            </Button>
                        </div>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    )
}
