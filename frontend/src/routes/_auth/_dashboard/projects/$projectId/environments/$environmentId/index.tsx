import { createFileRoute, getRouteApi, Link } from '@tanstack/react-router'
import { api, apiBaseUrl } from '@/api-client'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
    Card,
    CardContent,
    CardDescription,
    CardFooter,
    CardHeader,
    CardTitle,
} from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import LoadingButton from '@/components/loading-button'
import { useToast } from '@/hooks/use-toast'
import { useModal } from '@/hooks/use-modal'
import { useTimeAgo } from '@/hooks/useTimeAgo'
import { cn } from '@/lib/utils'
import { useMutation, useQuery } from '@tanstack/react-query'
import {
    ArrowLeft,
    Check,
    Clock,
    Cpu,
    GitBranch,
    Globe,
    HardDrive,
    MemoryStick,
    Rocket,
    Trash2,
    Upload,
    FileText,
    Info,
    PlusCircle,
    Undo,
    ExternalLink,
    MoreVertical,
    Star,
    Server,
    Network,
    Database,
    EyeOff,
    ChevronDown,
} from 'lucide-react'
import { Fragment, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { SystemType } from '../../../../../../../lib/system-type'
import { Alert, AlertDescription } from '@/components/ui/alert'
import VerifyDnsDialog from '../../settings/!components/verify-dns-dialog'
import AreYouSureDialog from '@/components/are-you-sure-dialog'
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { CopyButton } from '@/components/copy-button'
import { CopyText } from '@/components/copy-text'
import {
    Tooltip,
    TooltipContent,
    TooltipProvider,
    TooltipTrigger,
} from '@/components/ui/tooltip'
import { components } from '@/generated'
import { useAppForm } from '@/components/form/use-app-form'
import { z } from 'zod'

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/environments/$environmentId/',
)({
    component: EnvironmentDetailPage,
})

// --- Types ---

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

type DeploymentItem = {
    id: string
    status: string
    environment: string
    branch: string
    commitHash: string
    commitMessage: string
    createdAt: string
    author: string
    isCurrent: boolean
    buildDuration: string
}

type DomainDto = NonNullable<Awaited<ReturnType<typeof api.v1.getProjectsProjectIdDomains.fetchQuery>>["domains"]>[number]
type ServiceDto = NonNullable<NonNullable<Awaited<ReturnType<typeof api.v1.getProjectsProjectIdServices.fetchQuery>>>["services"]>[number]

// ServiceDto already includes allocatedTcpPort and tcpHost
type ServiceWithTcp = ServiceDto

// --- Main Page ---

function EnvironmentDetailPage() {
    const { projectId, environmentId } = Route.useParams()
    const { toast } = useToast()
    const modal = useModal()

    // Fetch environment data from list endpoint, filter client-side
    const { data: envsData, refetch: refetchEnvs } =
        api.v1.getProjectsProjectIdEnvironments.useQuery(
            { path: { projectId } },
            { refetchInterval: 30_000 },
        )
    const { data: tiersData } = api.v1.getServersTiers.useQuery()
    const tiers = tiersData?.tiers ?? []

    const env = useMemo(
        () => envsData?.environments?.find((e) => e.id === environmentId) ?? null,
        [envsData, environmentId],
    )

    const isProduction = env?.type === 'Production'
    const branch = env?.branches?.[0] ?? 'main'
    const previewUrl = env?.url ? `https://${env.url}` : null

    // --- Deploy ---
    const { mutateAsync: deployAsync, isPending: isDeploying } = useMutation({
        mutationFn: async () => {
            const url = new URL(
                `${apiBaseUrl}/v1/projects/${encodeURIComponent(projectId)}/deployments/deploy-from-latest-commit`,
            )
            url.searchParams.set('environmentId', environmentId)
            const res = await fetch(url, { method: 'POST', credentials: 'include' })
            if (!res.ok) throw new Error((await res.text()) || 'Failed to deploy')
        },
        onSuccess: () => {
            toast({ title: 'Deployment started', description: `Deploying latest commit.` })
            refetchEnvs()
        },
    })

    // --- Delete ---
    const { mutateAsync: deleteEnv, isPending: isDeleting } =
        api.v1.deleteProjectsProjectIdEnvironmentsEnvironmentId.useMutation({
            path: { projectId, environmentId },
        })

    const handleDelete = async () => {
        const name = env?.name ?? environmentId
        const confirmed = await modal.confirm(
            'Delete Environment',
            `Delete "${name}"? This will stop the server and cancel its subscription.`,
            { confirmText: 'Delete', variant: 'destructive' },
        )
        if (!confirmed) return
        await deleteEnv()
        toast({ title: 'Environment deleted', description: `"${name}" has been removed.` })
        // Navigate back to environments list
        window.history.back()
    }

    if (!env) {
        return (
            <div className="flex items-center justify-center py-16">
                <div className="animate-spin h-6 w-6 border-2 border-muted-foreground border-t-transparent rounded-full" />
            </div>
        )
    }

    const invalidateEnvs = () =>
        api.v1.getProjectsProjectIdEnvironments.invalidateQueries({
            parameters: { path: { projectId } },
        })

    return (
        <div className="space-y-8">
            {/* Header */}
            <div>
                <Link
                    to="/projects/$projectId/environments"
                    params={{ projectId }}
                    className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors mb-4"
                >
                    <ArrowLeft className="h-3.5 w-3.5" />
                    Environments
                </Link>

                <div className="flex flex-col gap-4 sm:flex-row sm:justify-between sm:items-start">
                    <div className="space-y-2">
                        <div className="flex items-center gap-2 flex-wrap">
                            <h2 className="text-2xl font-bold tracking-tight">
                                {env.name ?? (isProduction ? 'Production' : environmentId)}
                            </h2>
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
                        </div>
                        <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-muted-foreground">
                            {env.branches && env.branches.length > 0 && (
                                <span className="flex items-center gap-1.5">
                                    <GitBranch className="h-3.5 w-3.5" />
                                    <span className="font-mono text-xs">{env.branches.join(', ')}</span>
                                </span>
                            )}
                            {previewUrl && (
                                <a
                                    href={previewUrl}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    className="flex items-center gap-1.5 hover:text-foreground transition-colors"
                                >
                                    <Globe className="h-3.5 w-3.5" />
                                    <span className="truncate max-w-[260px]">
                                        {env?.url}
                                    </span>
                                </a>
                            )}
                        </div>
                    </div>

                    <div className="flex items-center gap-2">
                        <LoadingButton
                            variant="outline"
                            size="sm"
                            isLoading={isDeploying}
                            onClick={() => deployAsync()}
                        >
                            <Rocket className="h-3.5 w-3.5 mr-1" />
                            Deploy
                        </LoadingButton>
                        {!isProduction && (
                            <Button
                                variant="ghost"
                                size="sm"
                                className="text-muted-foreground hover:text-red-400 hover:bg-red-500/10"
                                onClick={handleDelete}
                                disabled={isDeleting}
                            >
                                <Trash2 className="h-3.5 w-3.5 mr-1" />
                                Delete
                            </Button>
                        )}
                    </div>
                </div>
            </div>

            {/* Sections */}
            <RecentDeploymentsSection projectId={projectId} environmentId={environmentId} />
            <ServerTierSection projectId={projectId} environmentId={environmentId} env={env} tiers={tiers} onChanged={invalidateEnvs} />
            <AutoDeploySection projectId={projectId} environmentId={environmentId} env={env} />
            <EnvironmentVariablesSection projectId={projectId} branch={branch} />
            <DomainsSection projectId={projectId} />
        </div>
    )
}

// ============================================================
// Section: Recent Deployments
// ============================================================

function RecentDeploymentsSection({ projectId, environmentId }: { projectId: string; environmentId: string }) {
    const { data, isLoading } = useQuery({
        queryKey: ['environment-deployments', projectId, environmentId],
        queryFn: async () => {
            const res = await fetch(
                `${apiBaseUrl}/v1/projects/${encodeURIComponent(projectId)}/environments/${encodeURIComponent(environmentId)}/deployments?pageSize=5`,
                { credentials: 'include' },
            )
            if (!res.ok) return { items: [], totalCount: 0 }
            return res.json() as Promise<{ items: DeploymentItem[]; totalCount: number }>
        },
        refetchInterval: 15_000,
    })

    const deployments = data?.items ?? []

    return (
        <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
            <CardHeader className="pb-3">
                <CardTitle className="text-lg">Recent Deployments</CardTitle>
                <CardDescription>Latest deployments for this environment.</CardDescription>
            </CardHeader>
            <CardContent>
                {isLoading && (
                    <div className="flex justify-center py-8">
                        <div className="animate-spin h-5 w-5 border-2 border-muted-foreground border-t-transparent rounded-full" />
                    </div>
                )}
                {!isLoading && deployments.length === 0 && (
                    <p className="text-sm text-muted-foreground text-center py-8">
                        No deployments yet for this environment.
                    </p>
                )}
                {deployments.length > 0 && (
                    <div className="divide-y divide-zinc-800/50">
                        {deployments.map((d) => (
                            <DeploymentRow key={d.id} deployment={d} projectId={projectId} />
                        ))}
                    </div>
                )}
            </CardContent>
            {(data?.totalCount ?? 0) > 5 && (
                <CardFooter className="border-t border-zinc-800 pt-4">
                    <Link
                        to="/projects/$projectId/deployments"
                        params={{ projectId }}
                        className="text-sm text-muted-foreground hover:text-foreground transition-colors"
                    >
                        View all {data?.totalCount} deployments
                    </Link>
                </CardFooter>
            )}
        </Card>
    )
}

function DeploymentRow({ deployment: d, projectId }: { deployment: DeploymentItem; projectId: string }) {
    const timeAgo = useTimeAgo(d.createdAt)

    const statusColor: Record<string, string> = {
        Completed: 'bg-emerald-500',
        Failed: 'bg-red-500',
        InProgress: 'bg-amber-500 animate-pulse',
        Queued: 'bg-zinc-500',
        Cancelled: 'bg-zinc-600',
    }

    return (
        <Link
            to="/projects/$projectId/deployments"
            params={{ projectId }}
            className="flex items-center gap-3 py-3 px-1 hover:bg-zinc-800/30 rounded transition-colors -mx-1"
        >
            <div className={cn('h-2 w-2 rounded-full shrink-0', statusColor[d.status] ?? 'bg-zinc-500')} />
            <div className="flex-1 min-w-0">
                <p className="text-sm truncate">{d.commitMessage}</p>
                <div className="flex items-center gap-2 text-xs text-muted-foreground mt-0.5">
                    <span className="font-mono">{d.commitHash?.slice(0, 7)}</span>
                    <span>{d.author}</span>
                </div>
            </div>
            <div className="text-xs text-muted-foreground shrink-0 flex items-center gap-1">
                <Clock className="h-3 w-3" />
                {timeAgo}
            </div>
            {d.isCurrent && (
                <Badge variant="outline" className="text-[10px] border-emerald-500/30 text-emerald-500 shrink-0">
                    Current
                </Badge>
            )}
        </Link>
    )
}

// ============================================================
// Section: Server Tier
// ============================================================

function ServerTierSection({
    projectId,
    environmentId,
    env,
    tiers,
    onChanged,
}: {
    projectId: string
    environmentId: string
    env: EnvironmentDto
    tiers: TierDto[]
    onChanged: () => void
}) {
    const { toast } = useToast()
    const [selectedTier, setSelectedTier] = useState<string | null>(null)
    const [saving, setSaving] = useState(false)

    useEffect(() => {
        if (env.serverTierId) setSelectedTier(env.serverTierId)
    }, [env.serverTierId])

    const handleSave = async () => {
        if (!selectedTier) return
        setSaving(true)
        try {
            const res = await fetch(
                `${apiBaseUrl}/v1/projects/${encodeURIComponent(projectId)}/environments/${encodeURIComponent(environmentId)}/tier`,
                {
                    method: 'PUT',
                    credentials: 'include',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ tier: selectedTier }),
                },
            )
            if (!res.ok) throw new Error((await res.text()) || 'Failed to change tier')
            toast({ title: 'Tier changed', description: `Server tier changed to ${selectedTier}. A new deployment has started.` })
            onChanged()
        } catch (err) {
            toast({ title: 'Error', description: err instanceof Error ? err.message : 'Unknown error', variant: 'destructive' })
        } finally {
            setSaving(false)
        }
    }

    const hasChanges = selectedTier && selectedTier !== env.serverTierId

    return (
        <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
            <CardHeader className="pb-4">
                <CardTitle className="text-lg">Server Tier</CardTitle>
                <CardDescription>Select the server resources for this environment.</CardDescription>
            </CardHeader>
            <CardContent>
                <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                    {tiers.map((t) => {
                        const isCurrent = env.serverTierId === t.id.value
                        const isSelected = selectedTier === t.id.value
                        return (
                            <button
                                key={t.id.value!}
                                type="button"
                                onClick={() => setSelectedTier(t.id.value!)}
                                className={cn(
                                    'relative flex flex-col gap-3 rounded-lg border p-4 text-left transition-all',
                                    isSelected
                                        ? 'border-primary bg-primary/5 ring-1 ring-primary'
                                        : 'border-zinc-800 bg-zinc-900/30 hover:border-zinc-700',
                                )}
                            >
                                <div className="flex items-center justify-between">
                                    <span className="font-mono text-sm font-medium">{t.id.value}</span>
                                    {isCurrent && <Badge variant="secondary" className="text-xs">Current</Badge>}
                                </div>
                                <div className="flex items-center gap-3 text-xs text-muted-foreground">
                                    <span className="flex items-center gap-1"><MemoryStick className="h-3 w-3" />{t.ramGb}GB</span>
                                    <span className="flex items-center gap-1"><Cpu className="h-3 w-3" />{t.cores} Core{t.cores !== 1 ? 's' : ''}</span>
                                    <span className="flex items-center gap-1"><HardDrive className="h-3 w-3" />{t.diskGb}GB</span>
                                </div>
                                <div className="flex items-baseline gap-1">
                                    <span className="text-lg font-semibold">${t.price.value}</span>
                                    <span className="text-xs text-muted-foreground">/month</span>
                                </div>
                                {t.promotionalText && (
                                    <Badge variant="outline" className="w-fit text-xs border-amber-500/30 text-amber-500 bg-amber-500/10">
                                        {t.promotionalText}
                                    </Badge>
                                )}
                            </button>
                        )
                    })}
                </div>
            </CardContent>
            <CardFooter className="border-t border-zinc-800 pt-6">
                <LoadingButton isLoading={saving} disabled={!hasChanges} onClick={handleSave} className="w-full sm:w-auto">
                    Save Changes
                </LoadingButton>
            </CardFooter>
        </Card>
    )
}

// ============================================================
// Section: Auto-Deploy
// ============================================================

function AutoDeploySection({
    projectId,
    environmentId,
    env,
}: {
    projectId: string
    environmentId: string
    env: EnvironmentDto
}) {
    const { toast } = useToast()
    const [autoDeploy, setAutoDeploy] = useState(env.autoDeploy ?? false)

    useEffect(() => {
        setAutoDeploy(env.autoDeploy ?? false)
    }, [env.autoDeploy])

    const handleChange = async (checked: boolean) => {
        setAutoDeploy(checked)
        try {
            const res = await fetch(
                `${apiBaseUrl}/v1/projects/${encodeURIComponent(projectId)}/environments/${encodeURIComponent(environmentId)}/auto-deploy`,
                {
                    method: 'PUT',
                    credentials: 'include',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ autoDeploy: checked }),
                },
            )
            if (!res.ok) throw new Error((await res.text()) || 'Failed to update auto-deploy')
            toast({ title: 'Auto Deploy changed', description: `Auto Deploy is now ${checked ? 'enabled' : 'disabled'}.` })
        } catch (err) {
            setAutoDeploy(!checked) // revert
            toast({ title: 'Error', description: err instanceof Error ? err.message : 'Unknown error', variant: 'destructive' })
        }
    }

    return (
        <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
            <CardContent className="p-6">
                <div className="flex items-center justify-between">
                    <div className="space-y-0.5">
                        <Label className="text-base font-medium">Auto Deploy</Label>
                        <p className="text-sm text-muted-foreground">
                            Automatically deploy when you push to{' '}
                            <code className="font-mono text-xs bg-zinc-800 px-1.5 py-0.5 rounded">
                                {env.branches?.[0] ?? 'this branch'}
                            </code>.
                        </p>
                    </div>
                    <Switch checked={autoDeploy} onCheckedChange={handleChange} />
                </div>
            </CardContent>
        </Card>
    )
}

// ============================================================
// Section: Environment Variables
// ============================================================

type EnvironmentVar = {
    systemType: SystemType
    key: string
    value: string
    isNew: boolean
    isDeleting: boolean
    isGenerated: boolean
}

function EnvironmentVariablesSection({ projectId, branch }: { projectId: string; branch: string }) {
    const { toast } = useToast()

    const { data, error } = api.v1.getProjectsProjectIdEnvironment.useQuery(
        { path: { projectId }, query: { branch } },
    )

    const { mutate: saveVars } = api.v1.putProjectsProjectIdEnvironment.useMutation(
        { path: { projectId }, query: { branch } },
    )

    const config = data?.configuration
    const availableSystemTypes = (config?.allowedSystemTypes ?? ['Frontend', 'Backend']) as SystemType[]
    const defaultSystemType = (config?.defaultSystemType ?? 'Frontend') as SystemType
    const environmentFiles = config?.environmentFiles ?? []

    const [variables, setVariables] = useState<EnvironmentVar[]>([])
    const [selectedSystemType, setSelectedSystemType] = useState<SystemType>(defaultSystemType)
    const [newEnvKey, setNewEnvKey] = useState('')
    const [newEnvValue, setNewEnvValue] = useState('')
    const [isSaving, setIsSaving] = useState(false)
    const [isDragging, setIsDragging] = useState(false)
    const fileInputRef = useRef<HTMLInputElement>(null)
    const inputRef = useRef<HTMLInputElement>(null)

    useEffect(() => {
        setVariables(
            data?.variables?.map((item) => ({
                systemType: item.systemType as SystemType,
                key: item.key!,
                value: item.value!,
                isNew: false,
                isDeleting: false,
                isGenerated: !!item.isGenerated,
            })).sort((a) => (a.isGenerated ? -1 : 1)) ?? [],
        )
    }, [data])

    const addVar = () => {
        if (!newEnvKey.trim()) return
        const existing = variables.find((v) => v.key === newEnvKey)
        if (existing) {
            existing.value = newEnvValue
            existing.isGenerated = false
            existing.isNew = true
        } else {
            variables.push({ systemType: selectedSystemType, key: newEnvKey, value: newEnvValue, isNew: true, isDeleting: false, isGenerated: false })
        }
        setNewEnvKey('')
        setNewEnvValue('')
        setVariables([...variables])
        inputRef.current?.focus()
    }

    const toggleRemove = (idx: number) => {
        const filtered = variables.filter((v) => v.systemType === selectedSystemType)
        if (filtered[idx]?.isNew) {
            filtered.splice(idx, 1)
        } else {
            filtered[idx] = { ...filtered[idx]!, isDeleting: !filtered[idx]!.isDeleting }
        }
        setVariables([...variables.filter((v) => v.systemType !== selectedSystemType), ...filtered])
    }

    const handleEnvFile = (file: File) => {
        const reader = new FileReader()
        reader.onload = (e) => {
            const content = e.target?.result as string
            if (!content) return
            const newVars: EnvironmentVar[] = []
            content.split('\n').forEach((line) => {
                if (line.trim() && !line.startsWith('#')) {
                    const parts = line.split('=')
                    if (parts.length >= 2) {
                        const key = parts[0]?.trim()
                        const value = parts.slice(1).join('=').trim()
                        if (key) newVars.push({ systemType: selectedSystemType, key, value, isNew: true, isDeleting: false, isGenerated: false })
                    }
                }
            })
            if (newVars.length > 0) setVariables((prev) => [...prev, ...newVars])
        }
        reader.readAsText(file)
    }

    const saveChanges = () => {
        setIsSaving(true)
        saveVars(
            { variables: variables.filter((v) => !v.isGenerated && !v.isDeleting) },
            {
                onSuccess: () => {
                    toast({ title: 'Saved', description: 'Environment variables updated.' })
                    api.v1.getProjectsProjectIdEnvironment.invalidateQueries({ parameters: { path: { projectId }, query: { branch } } })
                },
                onSettled: () => setIsSaving(false),
            },
        )
    }

    const systemTypeHasChanges = (st: SystemType) => variables.filter((v) => v.systemType === st).some((v) => v.isNew || v.isDeleting)
    const hasAnyChanges = availableSystemTypes.some(systemTypeHasChanges)
    const variablesToShow = variables.filter((v) => v.systemType === selectedSystemType)

    return (
        <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
            <CardHeader>
                <CardTitle className="text-lg">Environment Variables</CardTitle>
                <CardDescription>Manage environment variables for this environment.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
                <Alert className="border-zinc-800 bg-zinc-900/30">
                    <FileText className="h-4 w-4" />
                    <AlertDescription className="text-sm">
                        Variables stored in{' '}
                        {environmentFiles.map((file, i) => (
                            <Fragment key={file.filePath}>
                                <code className="font-mono text-xs bg-zinc-800 px-1.5 py-0.5 rounded">{file.filePath}</code>
                                {i < environmentFiles.length - 1 && ' and '}
                            </Fragment>
                        ))}
                        {environmentFiles.length === 0 && <code className="font-mono text-xs bg-zinc-800 px-1.5 py-0.5 rounded">.env</code>}
                    </AlertDescription>
                </Alert>

                {availableSystemTypes.length > 1 && (
                    <div className="flex items-center gap-1 p-1 rounded-lg bg-zinc-900/50 border border-zinc-800 w-fit">
                        {availableSystemTypes.map((st) => (
                            <button
                                key={st}
                                type="button"
                                onClick={() => setSelectedSystemType(st)}
                                className={cn(
                                    'relative px-4 py-1.5 text-sm font-medium rounded-md transition-all',
                                    selectedSystemType === st ? 'bg-zinc-800 text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
                                )}
                            >
                                {st}
                                {systemTypeHasChanges(st) && <span className="absolute top-1 right-1 w-1.5 h-1.5 rounded-full bg-amber-500" />}
                            </button>
                        ))}
                    </div>
                )}

                {/* Drop zone */}
                <div
                    className={cn(
                        'rounded-lg border-2 border-dashed p-8 text-center cursor-pointer transition-all',
                        isDragging ? 'border-primary/50 bg-primary/5' : 'border-zinc-800 hover:border-zinc-700',
                    )}
                    onDragOver={(e) => { e.preventDefault(); setIsDragging(true) }}
                    onDragLeave={() => setIsDragging(false)}
                    onDrop={(e) => {
                        e.preventDefault()
                        setIsDragging(false)
                        const file = e.dataTransfer.files?.[0]
                        if (file?.name.endsWith('.env')) handleEnvFile(file)
                    }}
                >
                    <input type="file" className="hidden" accept=".env" ref={fileInputRef}
                        onChange={(e) => { const f = e.target.files?.[0]; if (f?.name.endsWith('.env')) handleEnvFile(f) }}
                    />
                    <label htmlFor="" onClick={() => fileInputRef.current?.click()} className="cursor-pointer">
                        <div className="flex flex-col items-center gap-2">
                            <div className="rounded-full bg-zinc-800 p-3"><Upload className="h-5 w-5 text-muted-foreground" /></div>
                            <p className="text-sm font-medium">Upload .env file</p>
                            <p className="text-xs text-muted-foreground max-w-sm">Drag and drop or click to browse.</p>
                        </div>
                    </label>
                </div>

                {/* Variables list */}
                <div className="space-y-2">
                    {variablesToShow.length > 0 ? (
                        <div className="rounded-lg border border-zinc-800 overflow-hidden divide-y divide-zinc-800/50">
                            {variablesToShow.map((v, i) => (
                                <div key={i} className={cn('flex items-center gap-3 px-4 py-3 transition-colors', v.isDeleting && 'bg-red-500/5', v.isNew && !v.isDeleting && 'bg-green-500/5')}>
                                    <div className="flex-1 min-w-0 grid grid-cols-1 sm:grid-cols-2 gap-2">
                                        <span className={cn('font-mono text-sm truncate', v.isDeleting ? 'line-through text-red-500/70' : 'text-muted-foreground')}>{v.key}</span>
                                        <span className={cn('font-mono text-sm truncate', v.isDeleting ? 'line-through text-red-500/70' : 'text-foreground')}>{v.value}</span>
                                    </div>
                                    <div className="flex items-center gap-2 shrink-0">
                                        {v.isGenerated ? (
                                            <Badge variant="outline" className="text-xs border-zinc-700 text-muted-foreground"><Info className="h-3 w-3 mr-1" />auto</Badge>
                                        ) : (
                                            <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => toggleRemove(i)}>
                                                {v.isDeleting ? <Undo className="h-4 w-4 text-muted-foreground" /> : <Trash2 className="h-4 w-4 text-destructive" />}
                                            </Button>
                                        )}
                                    </div>
                                </div>
                            ))}
                        </div>
                    ) : (
                        <div className="flex items-center justify-center py-8 text-sm text-muted-foreground">No variables configured.</div>
                    )}
                </div>

                {/* Add variable */}
                <form onSubmit={(e) => { e.preventDefault(); addVar() }}>
                    <div className="rounded-lg border border-dashed border-zinc-800 p-4">
                        <h4 className="text-sm font-medium mb-3">Add New Variable</h4>
                        <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3">
                            <Input ref={inputRef} placeholder="VARIABLE_NAME" maxLength={255} value={newEnvKey} onChange={(e) => setNewEnvKey(e.target.value)} className="font-mono text-sm flex-1" />
                            <Input placeholder="value" value={newEnvValue} maxLength={20480} onChange={(e) => setNewEnvValue(e.target.value)} className="font-mono text-sm flex-1" />
                            <Button type="submit" variant="outline" size="icon" className="h-10 w-10 shrink-0"><PlusCircle className="h-4 w-4" /></Button>
                        </div>
                    </div>
                </form>
            </CardContent>
            <CardFooter className="border-t border-zinc-800 pt-6">
                <LoadingButton isLoading={isSaving} onClick={saveChanges} disabled={!hasAnyChanges} className="w-full sm:w-auto">
                    Save Changes
                </LoadingButton>
            </CardFooter>
        </Card>
    )
}

// ============================================================
// Section: Domains
// ============================================================

const TCP_PORTS = new Set([5432, 3306, 6379, 27017, 1433, 5433, 3389, 22])

function getConnectionStringTemplate(serviceName: string, port: string, host: string, allocatedPort: number): string {
    const bare = host.replace(/^https?:\/\//, '')
    const portNum = parseInt(port)
    if (portNum === 5432) return `postgresql://user:password@${bare}:${allocatedPort}/dbname`
    if (portNum === 3306) return `mysql://user:password@${bare}:${allocatedPort}/dbname`
    if (portNum === 6379) return `redis://${bare}:${allocatedPort}`
    if (portNum === 27017) return `mongodb://user:password@${bare}:${allocatedPort}/dbname`
    return `${bare}:${allocatedPort}`
}

function getServiceTypeLabel(port: string): string {
    const portNum = parseInt(port)
    switch (portNum) {
        case 5432: case 5433: return 'PostgreSQL'
        case 3306: return 'MySQL'
        case 6379: return 'Redis'
        case 27017: return 'MongoDB'
        case 1433: return 'SQL Server'
        default: return 'TCP'
    }
}

function DomainsSection({ projectId }: { projectId: string }) {
    const { toast } = useToast()

    const { data: projectData } = api.v1.getProjectsProjectId.useQuery({ path: { projectId } })
    const { data: domainsData } = api.v1.getProjectsProjectIdDomains.useQuery({ path: { projectId } })
    const { data: servicesData } = api.v1.getProjectsProjectIdServices.useQuery({ path: { projectId } })

    const domains = useMemo(() => {
        if (projectData?.type === 'N8n') {
            return { ...domainsData, domains: domainsData?.domains?.filter((d) => d.serviceName?.toLowerCase() !== 'backend') ?? [] }
        }
        return domainsData
    }, [domainsData, projectData])

    const services = useMemo(() => (servicesData?.services ?? []) as ServiceWithTcp[], [servicesData])
    const httpServices = useMemo(() => services.filter((s) => s.protocol === 'Http'), [services])
    const availableServices = useMemo(() => httpServices.map((s) => s.name).filter((n): n is string => !!n), [httpServices])

    const serviceDomainMap = useMemo(() => {
        const map = new Map<string, DomainDto>()
        ;(domains?.domains ?? []).forEach((d) => { if (d.isInternal && d.serviceName) map.set(d.serviceName, d) })
        return map
    }, [domains?.domains])

    const servicePortMap = useMemo(() => {
        const map = new Map<string, string>()
        services.forEach((s) => { if (s.name && s.port) map.set(s.name, s.port) })
        return map
    }, [services])

    const guessProtocol = useCallback((name: string): components['schemas']['Api.Data.Protocol'] => {
        const port = servicePortMap.get(name)
        return port && TCP_PORTS.has(parseInt(port)) ? 'Tcp' : 'Http'
    }, [servicePortMap])

    // Domain mutations
    const [selectedDomainId, setSelectedDomainId] = useState<string | null>(null)
    const [deleteDomainId, setDeleteDomainId] = useState<string | null>(null)
    const [setPrimaryId, setSetPrimaryId] = useState<string | null>(null)

    const invalidate = () => {
        api.v1.getProjectsProjectIdDomains.invalidateQueries({ parameters: { path: { projectId } } })
    }
    const invalidateServices = () => {
        api.v1.getProjectsProjectIdServices.invalidateQueries({ parameters: { path: { projectId } } })
    }

    const { mutateAsync: addDomain } = api.v1.postProjectsProjectIdDomains.useMutation({ path: { projectId } }, {
        onSuccess: ({ domainId }) => { invalidate(); setSelectedDomainId(domainId || null) },
    })

    const { mutateAsync: deleteDomain } = api.v1.deleteProjectsProjectIdDomainsDomainId.useMutation({
        path: { projectId, domainId: deleteDomainId! },
    }, { onSuccess: invalidate })

    const { mutateAsync: setPrimary } = api.v1.postProjectsProjectIdDomainsDomainIdSetPrimary.useMutation({
        path: { projectId, domainId: setPrimaryId! },
    }, { onSuccess: () => { invalidate(); api.v1.getProjectsProjectId.invalidateQueries({ parameters: { path: { projectId } } }) } })

    useEffect(() => {
        if (!setPrimaryId) return
        setPrimary().finally(() => setSetPrimaryId(null))
    }, [setPrimaryId])

    // TCP mutations
    const [exposingService, setExposingService] = useState<string | null>(null)
    const [unexposingService, setUnexposingService] = useState<string | null>(null)
    const [changingProtocol, setChangingProtocol] = useState<string | null>(null)

    const exposeMutation = useMutation({
        mutationFn: async (name: string) => {
            const res = await fetch(`${apiBaseUrl}/v1/projects/${encodeURIComponent(projectId)}/services/${encodeURIComponent(name)}/expose-tcp`, { method: 'POST', credentials: 'include' })
            if (!res.ok) throw new Error(await res.text())
            return res.json()
        },
        onMutate: (name) => setExposingService(name),
        onSuccess: () => { invalidate(); invalidateServices() },
        onSettled: () => setExposingService(null),
    })

    const unexposeMutation = useMutation({
        mutationFn: async (name: string) => {
            const res = await fetch(`${apiBaseUrl}/v1/projects/${encodeURIComponent(projectId)}/services/${encodeURIComponent(name)}/expose-tcp`, { method: 'DELETE', credentials: 'include' })
            if (!res.ok) throw new Error(await res.text())
        },
        onMutate: (name) => setUnexposingService(name),
        onSuccess: () => { invalidate(); invalidateServices() },
        onSettled: () => setUnexposingService(null),
    })

    const protocolMutation = useMutation({
        mutationFn: async ({ name, protocol }: { name: string; protocol: 'Http' | 'Tcp' }) => {
            const res = await fetch(`${apiBaseUrl}/v1/projects/${encodeURIComponent(projectId)}/services/${encodeURIComponent(name)}/protocol`, {
                method: 'PATCH', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ protocol }),
            })
            if (!res.ok) throw new Error(await res.text())
        },
        onMutate: ({ name }) => setChangingProtocol(name),
        onSuccess: () => { invalidate(); invalidateServices() },
        onSettled: () => setChangingProtocol(null),
    })

    // Add domain form
    const addDomainSchema = useMemo(() => z.object({
        domain: z.string().transform((v) => v.trim()).refine((v) => v.length === 0 || v.length >= 4, { message: 'Min 4 chars' }).refine((v) => v.length === 0 || /^[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,}$/.test(v), { message: 'Invalid domain' }),
        serviceName: availableServices.length > 0 ? z.enum(availableServices as [string, ...string[]]) : z.string(),
        protocol: z.enum(['Http', 'Tcp'] satisfies components['schemas']['Api.Data.Protocol'][]),
    }), [availableServices])

    const [formServiceName, setFormServiceName] = useState('')
    const prevService = useRef(formServiceName)

    const addDomainForm = useAppForm({
        defaultValues: { domain: '', serviceName: availableServices[0] ?? '', protocol: 'Http' as components['schemas']['Api.Data.Protocol'] },
        validators: { onChange: addDomainSchema },
        onSubmit: async ({ value }) => { await addDomain(value) },
    })

    useEffect(() => {
        if (prevService.current !== formServiceName) {
            prevService.current = formServiceName
            addDomainForm.setFieldValue('protocol', guessProtocol(formServiceName), { dontUpdateMeta: true })
        }
    }, [formServiceName, guessProtocol, addDomainForm])

    const grouped = useMemo(() => {
        const all = domains?.domains ?? []
        const sortFn = (a: DomainDto, b: DomainDto) => (a.isInternal ? 1 : 0) - (b.isInternal ? 1 : 0)
        return {
            primary: all.filter((d) => d.isPrimary).sort(sortFn),
            verified: all.filter((d) => !d.isPrimary && d.isVerified).sort(sortFn),
            pending: all.filter((d) => !d.isPrimary && !d.isVerified).sort(sortFn),
        }
    }, [domains?.domains])

    return (
        <>
            {/* Services */}
            <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
                <CardHeader>
                    <CardTitle className="text-lg">Services</CardTitle>
                    <CardDescription>Detected services. HTTP services get auto-generated domains. TCP services can be exposed externally.</CardDescription>
                </CardHeader>
                <CardContent>
                    {services.length === 0 ? (
                        <p className="text-sm text-muted-foreground text-center py-8">No services detected yet.</p>
                    ) : (
                        <div className="space-y-2">
                            {services.map((s) => {
                                const isHttp = s.protocol === 'Http'
                                const isTcp = s.protocol === 'Tcp'
                                const isExposed = isTcp && !!s.allocatedTcpPort && !!s.tcpHost
                                const internalDomain = serviceDomainMap.get(s.name!)
                                const connStr = isExposed ? getConnectionStringTemplate(s.name!, s.port!, s.tcpHost!, s.allocatedTcpPort!) : null

                                return (
                                    <div key={s.name} className="group relative p-4 rounded-lg border border-zinc-800 bg-zinc-900/30 hover:bg-zinc-900/50 transition-all">
                                        <div className="flex items-start gap-3">
                                            <div className="shrink-0 w-10 h-10 rounded-lg flex items-center justify-center bg-zinc-800 text-muted-foreground">
                                                {isTcp && TCP_PORTS.has(parseInt(s.port ?? '0')) ? <Database className="w-5 h-5" /> : isTcp ? <Network className="w-5 h-5" /> : <Globe className="w-5 h-5" />}
                                            </div>
                                            <div className="min-w-0 flex-1">
                                                <div className="flex items-start justify-between gap-2">
                                                    <div className="min-w-0 flex-1">
                                                        <div className="flex items-center gap-2">
                                                            <h3 className="font-medium text-sm">{s.name}</h3>
                                                            <span className="text-xs text-muted-foreground/60 font-mono">:{s.port}</span>
                                                        </div>
                                                        <div className="flex items-center gap-2 mt-1 flex-wrap">
                                                            <Badge variant="outline" className="text-[10px] font-normal border-zinc-700 text-muted-foreground font-mono">
                                                                {isTcp ? getServiceTypeLabel(s.port!) : 'HTTP'}
                                                            </Badge>
                                                            {isHttp && internalDomain?.value && (
                                                                <a href={`https://${internalDomain.value}`} target="_blank" rel="noopener noreferrer" className="text-xs text-muted-foreground hover:text-foreground font-mono truncate flex items-center gap-1">
                                                                    {internalDomain.value}<ExternalLink className="w-3 h-3 shrink-0" />
                                                                </a>
                                                            )}
                                                            {isTcp && !isExposed && <span className="text-xs text-muted-foreground/60">Internal only</span>}
                                                            {isTcp && isExposed && (
                                                                <CopyText value={connStr!}>
                                                                    <span className="text-xs text-muted-foreground hover:text-foreground font-mono cursor-pointer">{s.tcpHost!.replace(/^https?:\/\//, '')}:{s.allocatedTcpPort}</span>
                                                                </CopyText>
                                                            )}
                                                        </div>
                                                    </div>
                                                    <div className="flex items-center gap-1.5 shrink-0">
                                                        <DropdownMenu>
                                                            <DropdownMenuTrigger asChild>
                                                                <Button variant="ghost" size="sm" className="h-7 px-2 text-xs text-muted-foreground" disabled={changingProtocol === s.name}>
                                                                    {s.protocol}<ChevronDown className="ml-1 h-3 w-3" />
                                                                </Button>
                                                            </DropdownMenuTrigger>
                                                            <DropdownMenuContent align="end">
                                                                <DropdownMenuItem onClick={() => protocolMutation.mutate({ name: s.name!, protocol: 'Http' })} disabled={s.protocol === 'Http'}>
                                                                    <Globe className="mr-2 h-4 w-4" />HTTP{s.protocol === 'Http' && <Check className="ml-auto h-4 w-4" />}
                                                                </DropdownMenuItem>
                                                                <DropdownMenuItem onClick={() => protocolMutation.mutate({ name: s.name!, protocol: 'Tcp' })} disabled={s.protocol === 'Tcp'}>
                                                                    <Network className="mr-2 h-4 w-4" />TCP{s.protocol === 'Tcp' && <Check className="ml-auto h-4 w-4" />}
                                                                </DropdownMenuItem>
                                                            </DropdownMenuContent>
                                                        </DropdownMenu>
                                                        {isTcp && !isExposed && (
                                                            <LoadingButton isLoading={exposingService === s.name} variant="outline" size="sm" className="h-7 px-3 text-xs" onClick={() => exposeMutation.mutate(s.name!)}>
                                                                Expose
                                                            </LoadingButton>
                                                        )}
                                                        {isTcp && isExposed && (
                                                            <div className="flex items-center gap-1">
                                                                <TooltipProvider><Tooltip><TooltipTrigger asChild>
                                                                    <CopyButton value={connStr!} className="h-7 w-7 p-0"><span className="sr-only">Copy</span></CopyButton>
                                                                </TooltipTrigger><TooltipContent><p className="font-mono text-xs max-w-xs break-all">{connStr}</p></TooltipContent></Tooltip></TooltipProvider>
                                                                <LoadingButton isLoading={unexposingService === s.name} variant="ghost" size="sm" className="h-7 px-2 text-xs text-muted-foreground hover:text-red-500" onClick={() => unexposeMutation.mutate(s.name!)}>
                                                                    <EyeOff className="h-3 w-3 mr-1" />Unexpose
                                                                </LoadingButton>
                                                            </div>
                                                        )}
                                                        {isHttp && internalDomain?.value && (
                                                            <Button variant="ghost" size="icon" className="h-7 w-7" asChild>
                                                                <a href={`https://${internalDomain.value}`} target="_blank" rel="noopener noreferrer"><ExternalLink className="h-3.5 w-3.5" /></a>
                                                            </Button>
                                                        )}
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                )
                            })}
                        </div>
                    )}
                </CardContent>
            </Card>

            {/* Custom Domains */}
            <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
                <CardHeader>
                    <CardTitle className="text-lg">Custom Domains</CardTitle>
                    <CardDescription>Manage custom domains for your HTTP services.</CardDescription>
                </CardHeader>
                <CardContent className="space-y-8">
                    {/* Domain groups */}
                    {grouped.primary.length > 0 && (
                        <DomainGroup title="Primary Domain" domains={grouped.primary} variant="primary" onVerify={setSelectedDomainId} onDelete={setDeleteDomainId} onSetPrimary={setSetPrimaryId} activePrimary={setPrimaryId} />
                    )}
                    {grouped.verified.length > 0 && (
                        <DomainGroup title="Verified Domains" domains={grouped.verified} variant="verified" onVerify={setSelectedDomainId} onDelete={setDeleteDomainId} onSetPrimary={setSetPrimaryId} activePrimary={setPrimaryId} />
                    )}
                    {grouped.pending.length > 0 && (
                        <DomainGroup title="Pending Verification" domains={grouped.pending} variant="pending" onVerify={setSelectedDomainId} onDelete={setDeleteDomainId} onSetPrimary={setSetPrimaryId} activePrimary={setPrimaryId} />
                    )}

                    {/* Add domain form */}
                    <div className="space-y-3">
                        <div>
                            <h3 className="text-xs font-medium text-muted-foreground uppercase tracking-widest">Add Custom Domain</h3>
                        </div>
                        <form onSubmit={(e) => { e.preventDefault(); addDomainForm.handleSubmit() }}>
                            <div className="flex items-center p-4 rounded-lg border border-dashed border-zinc-800 hover:border-zinc-700 bg-zinc-900/30 transition-all">
                                <div className="shrink-0 w-10 h-10 rounded-lg flex items-center justify-center bg-zinc-800 text-muted-foreground mr-4">
                                    <Globe className="w-5 h-5" />
                                </div>
                                <div className="flex flex-col sm:flex-row gap-3 flex-1 items-start sm:items-center">
                                    <addDomainForm.AppField name="domain" children={(field) => (
                                        <field.TextField className="flex-1 min-w-0 font-mono" placeholder="example.com" />
                                    )} />
                                    <div className="flex flex-col sm:flex-row gap-3 w-full sm:w-auto">
                                        <addDomainForm.AppField name="serviceName" listeners={{ onChange: (field) => setFormServiceName(field.value) }} children={(field) => (
                                            <field.SelectField className="w-full sm:w-[160px]" values={availableServices} />
                                        )} />
                                        <addDomainForm.AppField name="protocol" children={(field) => (
                                            <field.SelectField className="w-full sm:w-[100px]" values={['Http', 'Tcp']} />
                                        )} />
                                        <addDomainForm.Subscribe selector={(state) => [state.canSubmit, state.isSubmitting, state.values.domain]} children={([canSubmit, isSubmitting, domainValue]) => (
                                            <LoadingButton isLoading={!!isSubmitting} disabled={!canSubmit || !domainValue?.toString().trim()} type="submit" className="w-full sm:w-auto">Add</LoadingButton>
                                        )} />
                                    </div>
                                </div>
                            </div>
                        </form>
                    </div>

                    <VerifyDnsDialog selectedDomainId={selectedDomainId} onClose={() => { invalidate(); setSelectedDomainId(null) }} projectId={projectId} domains={domains?.domains ?? []} />
                    <AreYouSureDialog title="Delete this domain?" subtitle="You'll need to add and verify it again." open={!!deleteDomainId} onAnswer={(ans) => { if (ans && deleteDomainId) deleteDomain().finally(() => setDeleteDomainId(null)); else setDeleteDomainId(null) }} />
                </CardContent>
            </Card>
        </>
    )
}

function DomainGroup({
    title, domains, variant, onVerify, onDelete, onSetPrimary, activePrimary,
}: {
    title: string
    domains: DomainDto[]
    variant: 'primary' | 'verified' | 'pending'
    onVerify: (id: string) => void
    onDelete: (id: string) => void
    onSetPrimary: (id: string) => void
    activePrimary: string | null
}) {
    return (
        <div className="space-y-3">
            <h3 className="text-xs font-medium text-muted-foreground uppercase tracking-widest">{title}</h3>
            <div className="space-y-2">
                {domains.map((d) => {
                    const isPrimary = variant === 'primary'
                    const isPending = variant === 'pending'
                    return (
                        <div
                            key={d.domainId}
                            className={cn(
                                'group relative p-4 rounded-lg border transition-all',
                                isPrimary && 'bg-primary/5 border-primary/20 hover:border-primary/40',
                                !isPrimary && !isPending && 'bg-zinc-900/30 hover:bg-zinc-900/50 border-zinc-800',
                                isPending && 'bg-amber-500/5 border-amber-500/20 hover:border-amber-500/40 cursor-pointer',
                                d.isInternal && 'border-dashed',
                            )}
                            onClick={isPending ? () => onVerify(d.domainId!) : undefined}
                        >
                            <div className="flex items-start gap-3">
                                <div className={cn('shrink-0 w-10 h-10 rounded-lg flex items-center justify-center',
                                    isPrimary && 'bg-primary/10 text-primary',
                                    !isPrimary && !isPending && 'bg-zinc-800 text-muted-foreground',
                                    isPending && 'bg-amber-500/10 text-amber-600',
                                )}>
                                    {isPrimary ? <Star className="w-5 h-5" /> : d.isInternal ? <Server className="w-5 h-5" /> : d.protocol?.toLowerCase() === 'tcp' ? <Network className="w-5 h-5" /> : <Globe className="w-5 h-5" />}
                                </div>
                                <div className="min-w-0 flex-1">
                                    <div className="flex items-start justify-between gap-2">
                                        <div className="min-w-0 flex-1" onClick={(e) => e.stopPropagation()}>
                                            <CopyText value={d.value ?? ''}>
                                                <h3 className={cn('font-mono text-sm font-medium truncate cursor-pointer', isPrimary && 'text-primary', isPending && 'text-amber-600')}>{d.value}</h3>
                                            </CopyText>
                                            <div className="flex items-center gap-2 mt-1 flex-wrap">
                                                <span className="text-sm text-muted-foreground">{d.serviceName}</span>
                                                {d.protocol && <span className="text-xs text-muted-foreground/60 font-mono uppercase">{d.protocol}</span>}
                                                {d.isInternal && <Badge variant="outline" className="text-[10px] font-normal border-dashed border-zinc-700 text-muted-foreground">Auto-generated</Badge>}
                                            </div>
                                        </div>
                                        <div className="flex items-center gap-1 shrink-0">
                                            {activePrimary === d.domainId && <Badge variant="secondary" className="animate-pulse text-xs hidden sm:inline-flex">Setting...</Badge>}
                                            {!d.isVerified && !d.isInternal && (
                                                <Badge variant="outline" className="border-amber-500/30 text-amber-600 bg-amber-500/10 cursor-pointer hover:bg-amber-500/20 text-xs" onClick={(e) => { e.stopPropagation(); onVerify(d.domainId!) }}>
                                                    <Clock className="w-3 h-3 mr-1" />Unverified
                                                </Badge>
                                            )}
                                            <DropdownMenu>
                                                <DropdownMenuTrigger asChild>
                                                    <Button variant="ghost" size="icon" className="sm:opacity-0 sm:group-hover:opacity-100 h-8 w-8" onClick={(e) => e.stopPropagation()}>
                                                        <MoreVertical className="h-4 w-4" />
                                                    </Button>
                                                </DropdownMenuTrigger>
                                                <DropdownMenuContent align="end">
                                                    {!d.isVerified && !d.isInternal && <DropdownMenuItem onClick={() => onVerify(d.domainId!)}><Check className="mr-2 h-4 w-4" />Verify DNS</DropdownMenuItem>}
                                                    {d.value && <CopyButton value={d.value} className="gap-4" iconClassName="text-primary"><span className="text-sm">Copy Domain</span></CopyButton>}
                                                    {d.value && <DropdownMenuItem asChild><a href={`https://${d.value}`} target="_blank" rel="noopener noreferrer"><ExternalLink className="mr-2 h-4 w-4" />Visit Site</a></DropdownMenuItem>}
                                                    {d.isVerified && !d.isPrimary && <DropdownMenuItem onClick={() => onSetPrimary(d.domainId!)}><Star className="mr-2 h-4 w-4" />Set as primary</DropdownMenuItem>}
                                                    {!d.isInternal && <DropdownMenuItem className="text-red-500 focus:text-red-500 focus:bg-red-500/10" onClick={() => onDelete(d.domainId!)}><Trash2 className="mr-2 h-4 w-4" />Delete</DropdownMenuItem>}
                                                </DropdownMenuContent>
                                            </DropdownMenu>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    )
                })}
            </div>
        </div>
    )
}
