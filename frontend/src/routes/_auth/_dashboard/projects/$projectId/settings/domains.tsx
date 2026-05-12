import { createFileRoute } from '@tanstack/react-router'
import { api } from '@/api-client'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { z } from 'zod'
import { cn } from '@/lib/utils'
import { components } from '@/generated'

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import LoadingButton from '@/components/loading-button'
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuSub,
    DropdownMenuSubContent,
    DropdownMenuSubTrigger,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { CopyText } from '@/components/copy-text'
import AreYouSureDialog from '@/components/are-you-sure-dialog'
import VerifyDnsDialog from './!components/verify-dns-dialog'
import { useAppForm } from '@/components/form/use-app-form'

import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Globe, Star, Check, Clock, Trash2, MoreVertical, ExternalLink, Network } from 'lucide-react'

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/settings/domains',
)({
    component: DomainsSettingsTab,
})

type DomainDto = NonNullable<Awaited<ReturnType<typeof api.v1.getProjectsProjectIdDomains.fetchQuery>>['domains']>[number]

const addDomainSchema = z.object({
    domain: z
        .string()
        .transform((v) => v.trim())
        .refine((v) => v.length === 0 || v.length >= 4, { message: 'Min 4 chars' })
        .refine((v) => v.length === 0 || /^[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,}$/.test(v), {
            message: 'Invalid domain',
        }),
    serviceName: z.string(),
    protocol: z.enum(['Http', 'Tcp'] satisfies components['schemas']['Api.Data.Protocol'][]),
    environmentId: z.string(),
})

function DomainsSettingsTab() {
    const { projectId } = Route.useParams()

    const { data: domainsData } = api.v1.getProjectsProjectIdDomains.useQuery({ path: { projectId } })
    const { data: envsData } = api.v1.getProjectsProjectIdEnvironments.useQuery({ path: { projectId } })
    const { data: servicesData } = api.v1.getProjectsProjectIdServices.useQuery({ path: { projectId } })

    const [selectedDomainId, setSelectedDomainId] = useState<string | null>(null)
    const [deleteDomainId, setDeleteDomainId] = useState<string | null>(null)
    const [setPrimaryId, setSetPrimaryId] = useState<string | null>(null)

    const availableServices = useMemo(
        () =>
            (servicesData?.services ?? [])
                .filter((s) => s.protocol === 'Http')
                .map((s) => s.name)
                .filter((n): n is string => !!n),
        [servicesData],
    )

    const productionEnvId = useMemo(
        () => envsData?.environments?.find((e) => e.type === 'Production')?.id ?? '',
        [envsData],
    )

    const envOptions = useMemo(
        () => envsData?.environments?.map((e) => e.id).filter((id): id is string => !!id) ?? [],
        [envsData],
    )

    const envNameMap = useMemo(() => {
        const map = new Map<string, string>()
        ;(envsData?.environments ?? []).forEach((e) => {
            if (e.id) map.set(e.id, e.name ?? e.id)
        })
        return map
    }, [envsData])

    const isProduction = (envId: string | null | undefined) => {
        if (!envId) return false
        return envsData?.environments?.find((e) => e.id === envId)?.type === 'Production'
    }

    const invalidate = () => {
        api.v1.getProjectsProjectIdDomains.invalidateQueries({ parameters: { path: { projectId } } })
    }

    const { mutateAsync: addDomain } = api.v1.postProjectsProjectIdDomains.useMutation(
        { path: { projectId } },
        {
            onSuccess: ({ domainId }) => {
                invalidate()
                setSelectedDomainId(domainId || null)
            },
        },
    )

    const { mutateAsync: deleteDomain } = api.v1.deleteProjectsProjectIdDomainsDomainId.useMutation(
        { path: { projectId, domainId: deleteDomainId! } },
        { onSuccess: invalidate },
    )

    const { mutateAsync: setPrimary } = api.v1.postProjectsProjectIdDomainsDomainIdSetPrimary.useMutation(
        { path: { projectId, domainId: setPrimaryId! } },
        {
            onSuccess: () => {
                invalidate()
                api.v1.getProjectsProjectId.invalidateQueries({ parameters: { path: { projectId } } })
            },
        },
    )

    const { mutateAsync: changeService } = api.v1.patchProjectsProjectIdDomainsDomainIdService.useMutation(
        undefined as void,
        { onSuccess: invalidate },
    )

    const { mutateAsync: changeEnvironment } = api.v1.patchProjectsProjectIdDomainsDomainIdEnvironment.useMutation(
        undefined as void,
        { onSuccess: invalidate },
    )

    const onChangeService = useCallback(
        (domainId: string, serviceName: string, protocol: components['schemas']['Api.Data.Protocol']) =>
            changeService({ path: { projectId, domainId }, body: { serviceName, protocol } }),
        [changeService, projectId],
    )

    const onChangeEnvironment = useCallback(
        (domainId: string, envId: string | null) =>
            changeEnvironment({ path: { projectId, domainId }, body: { environmentId: envId } }),
        [changeEnvironment, projectId],
    )

    const sharedGroupProps = {
        onVerify: setSelectedDomainId,
        onDelete: setDeleteDomainId,
        onSetPrimary: setSetPrimaryId,
        activePrimary: setPrimaryId,
        isProduction,
        envNameMap,
        availableServices,
        environments: envsData?.environments ?? [],
        onChangeService,
        onChangeEnvironment,
    }

    useEffect(() => {
        if (!setPrimaryId) return
        setPrimary().finally(() => setSetPrimaryId(null))
    }, [setPrimaryId])

    const grouped = useMemo(() => {
        const all = domainsData?.domains ?? []
        const sortFn = (a: DomainDto, b: DomainDto) => (a.isInternal ? 1 : 0) - (b.isInternal ? 1 : 0)
        return {
            primary: all.filter((d) => d.isPrimary).sort(sortFn),
            verified: all.filter((d) => !d.isPrimary && d.isVerified).sort(sortFn),
            pending: all.filter((d) => !d.isPrimary && !d.isVerified).sort(sortFn),
        }
    }, [domainsData?.domains])

    const addDomainForm = useAppForm({
        defaultValues: {
            domain: '',
            serviceName: availableServices[0] ?? '',
            protocol: 'Http' as components['schemas']['Api.Data.Protocol'],
            environmentId: productionEnvId,
        },
        validators: { onChange: addDomainSchema },
        onSubmit: async ({ value }) => {
            await addDomain({
                domain: value.domain,
                serviceName: value.serviceName,
                protocol: value.protocol,
                environmentId: value.environmentId || null,
            })
        },
    })

    // Keep environmentId default in sync with productionEnvId once loaded
    useEffect(() => {
        if (productionEnvId && !addDomainForm.getFieldValue('environmentId')) {
            addDomainForm.setFieldValue('environmentId', productionEnvId)
        }
    }, [productionEnvId])

    const hasDomains =
        grouped.primary.length > 0 || grouped.verified.length > 0 || grouped.pending.length > 0

    return (
        <div className="space-y-6">
            <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
                <CardHeader className="pb-4">
                    <CardTitle className="text-lg flex items-center gap-2">
                        <Globe className="h-5 w-5" />
                        Custom Domains
                    </CardTitle>
                    <CardDescription>
                        Manage all custom domains across your project environments.
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-8">
                    {/* Domain groups */}
                    {hasDomains ? (
                        <>
                            {grouped.primary.length > 0 && (
                                <DomainGroup title="Primary Domain" domains={grouped.primary} variant="primary" {...sharedGroupProps} />
                            )}
                            {grouped.verified.length > 0 && (
                                <DomainGroup title="Verified Domains" domains={grouped.verified} variant="verified" {...sharedGroupProps} />
                            )}
                            {grouped.pending.length > 0 && (
                                <DomainGroup title="Pending Verification" domains={grouped.pending} variant="pending" {...sharedGroupProps} />
                            )}
                        </>
                    ) : (
                        <p className="text-sm text-muted-foreground text-center py-8">
                            No custom domains configured yet.
                        </p>
                    )}

                    {/* Add domain form */}
                    <div className="space-y-3">
                        <h3 className="text-xs font-medium text-muted-foreground uppercase tracking-widest">
                            Add Custom Domain
                        </h3>
                        <form
                            onSubmit={(e) => {
                                e.preventDefault()
                                addDomainForm.handleSubmit()
                            }}
                        >
                            <div className="flex items-center p-4 rounded-lg border border-dashed border-zinc-800 hover:border-zinc-700 bg-zinc-900/30 transition-all">
                                <div className="shrink-0 w-10 h-10 rounded-lg flex items-center justify-center bg-zinc-800 text-muted-foreground mr-4">
                                    <Globe className="w-5 h-5" />
                                </div>
                                <div className="flex flex-col sm:flex-row gap-3 flex-1 items-start sm:items-center">
                                    <addDomainForm.AppField
                                        name="domain"
                                        children={(field) => (
                                            <field.TextField
                                                className="flex-1 min-w-0 font-mono"
                                                placeholder="example.com"
                                            />
                                        )}
                                    />
                                    <div className="flex flex-col sm:flex-row gap-3 w-full sm:w-auto">
                                        {availableServices.length > 0 && (
                                            <addDomainForm.AppField
                                                name="serviceName"
                                                children={(field) => (
                                                    <field.SelectField
                                                        className="w-full sm:w-[160px]"
                                                        values={availableServices}
                                                    />
                                                )}
                                            />
                                        )}
                                        {envOptions.length > 1 && (
                                            <addDomainForm.AppField
                                                name="environmentId"
                                                children={(field) => (
                                                    <div className="w-full sm:w-[160px]">
                                                        <Select value={field.state.value} onValueChange={(v) => field.handleChange(v)}>
                                                            <SelectTrigger>
                                                                <SelectValue placeholder="Environment" />
                                                            </SelectTrigger>
                                                            <SelectContent>
                                                                {(envsData?.environments ?? []).map((e) => (
                                                                    <SelectItem key={e.id} value={e.id!}>
                                                                        {e.name ?? e.id}
                                                                    </SelectItem>
                                                                ))}
                                                            </SelectContent>
                                                        </Select>
                                                    </div>
                                                )}
                                            />
                                        )}
                                        <addDomainForm.Subscribe
                                            selector={(state) => [
                                                state.canSubmit,
                                                state.isSubmitting,
                                                state.values.domain,
                                            ]}
                                            children={([canSubmit, isSubmitting, domainValue]) => (
                                                <LoadingButton
                                                    isLoading={!!isSubmitting}
                                                    disabled={
                                                        !canSubmit || !domainValue?.toString().trim()
                                                    }
                                                    type="submit"
                                                    className="w-full sm:w-auto"
                                                >
                                                    Add
                                                </LoadingButton>
                                            )}
                                        />
                                    </div>
                                </div>
                            </div>
                        </form>
                    </div>

                    <VerifyDnsDialog
                        selectedDomainId={selectedDomainId}
                        onClose={() => {
                            invalidate()
                            setSelectedDomainId(null)
                        }}
                        projectId={projectId}
                        domains={domainsData?.domains ?? []}
                    />
                    <AreYouSureDialog
                        title="Delete this domain?"
                        subtitle="You'll need to add and verify it again."
                        open={!!deleteDomainId}
                        onAnswer={(ans) => {
                            if (ans && deleteDomainId) {
                                deleteDomain().finally(() => setDeleteDomainId(null))
                            } else {
                                setDeleteDomainId(null)
                            }
                        }}
                    />
                </CardContent>
            </Card>
        </div>
    )
}

function EnvironmentBadge({
    domain,
    isProductionFn,
    envNameMap,
}: {
    domain: DomainDto
    isProductionFn: (id: string | null | undefined) => boolean
    envNameMap: Map<string, string>
}) {
    if (!domain.environmentId && !domain.environmentName) {
        return (
            <Badge
                variant="outline"
                className="text-[10px] border-zinc-600/30 text-zinc-400 bg-zinc-800/40"
            >
                Unassigned
            </Badge>
        )
    }

    const name =
        domain.environmentName ??
        (domain.environmentId ? envNameMap.get(domain.environmentId) : undefined) ??
        'Unknown'

    const isProd = isProductionFn(domain.environmentId)

    return (
        <Badge
            variant="outline"
            className={cn(
                'text-[10px]',
                isProd
                    ? 'bg-emerald-500/10 text-emerald-500 border-emerald-500/20'
                    : 'bg-blue-500/10 text-blue-500 border-blue-500/20',
            )}
        >
            {name}
        </Badge>
    )
}

function DomainGroup({
    title,
    domains,
    variant,
    onVerify,
    onDelete,
    onSetPrimary,
    activePrimary,
    isProduction,
    envNameMap,
    availableServices,
    environments,
    onChangeService,
    onChangeEnvironment,
}: {
    title: string
    domains: DomainDto[]
    variant: 'primary' | 'verified' | 'pending'
    onVerify: (id: string) => void
    onDelete: (id: string) => void
    onSetPrimary: (id: string) => void
    activePrimary: string | null
    isProduction: (id: string | null | undefined) => boolean
    envNameMap: Map<string, string>
    availableServices: string[]
    environments: NonNullable<NonNullable<Awaited<ReturnType<typeof api.v1.getProjectsProjectIdEnvironments.fetchQuery>>>['environments']>
    onChangeService: (domainId: string, serviceName: string, protocol: components['schemas']['Api.Data.Protocol']) => void
    onChangeEnvironment: (domainId: string, environmentId: string | null) => void
}) {
    return (
        <div className="space-y-3">
            <h3 className="text-xs font-medium text-muted-foreground uppercase tracking-widest">
                {title}
            </h3>
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
                                !isPrimary &&
                                    !isPending &&
                                    'bg-zinc-900/30 hover:bg-zinc-900/50 border-zinc-800',
                                isPending &&
                                    'bg-amber-500/5 border-amber-500/20 hover:border-amber-500/40 cursor-pointer',
                                d.isInternal && 'border-dashed',
                            )}
                            onClick={isPending ? () => onVerify(d.domainId!) : undefined}
                        >
                            <div className="flex items-center gap-3">
                                <div
                                    className={cn(
                                        'shrink-0 w-10 h-10 rounded-lg flex items-center justify-center',
                                        isPrimary && 'bg-primary/10 text-primary',
                                        !isPrimary &&
                                            !isPending &&
                                            'bg-zinc-800 text-muted-foreground',
                                        isPending && 'bg-amber-500/10 text-amber-600',
                                    )}
                                >
                                    {isPrimary ? (
                                        <Star className="w-5 h-5" />
                                    ) : (
                                        <Globe className="w-5 h-5" />
                                    )}
                                </div>

                                <div className="min-w-0 flex-1">
                                    <div className="flex items-start justify-between gap-2">
                                        <div
                                            className="min-w-0 flex-1"
                                            onClick={(e) => e.stopPropagation()}
                                        >
                                            <CopyText value={d.value ?? ''}>
                                                <span
                                                    className={cn(
                                                        'font-mono text-sm font-medium truncate cursor-pointer block',
                                                        isPrimary && 'text-primary',
                                                        isPending && 'text-amber-600',
                                                    )}
                                                >
                                                    {d.value}
                                                </span>
                                            </CopyText>
                                            <div className="flex items-center gap-2 mt-1 flex-wrap">
                                                {d.serviceName && (
                                                    <span className="text-xs text-muted-foreground">
                                                        {'->'} {d.serviceName}
                                                    </span>
                                                )}
                                                <EnvironmentBadge
                                                    domain={d}
                                                    isProductionFn={isProduction}
                                                    envNameMap={envNameMap}
                                                />
                                                {d.isVerified && !d.isInternal && (
                                                    <Badge
                                                        variant="outline"
                                                        className="text-[10px] border-green-500/30 text-green-500 bg-green-500/10"
                                                    >
                                                        <Check className="w-3 h-3 mr-1" />
                                                        Verified
                                                    </Badge>
                                                )}
                                                {!d.isVerified && !d.isInternal && (
                                                    <Badge
                                                        variant="outline"
                                                        className="text-[10px] border-amber-500/30 text-amber-600 bg-amber-500/10 cursor-pointer hover:bg-amber-500/20"
                                                        onClick={(e) => {
                                                            e.stopPropagation()
                                                            onVerify(d.domainId!)
                                                        }}
                                                    >
                                                        <Clock className="w-3 h-3 mr-1" />
                                                        Unverified
                                                    </Badge>
                                                )}
                                                {d.isInternal && (
                                                    <Badge
                                                        variant="outline"
                                                        className="text-[10px] font-normal border-dashed border-zinc-700 text-muted-foreground"
                                                    >
                                                        Auto-generated
                                                    </Badge>
                                                )}
                                            </div>
                                        </div>

                                        <div className="flex items-center gap-1 shrink-0">
                                            {activePrimary === d.domainId && (
                                                <Badge
                                                    variant="secondary"
                                                    className="animate-pulse text-xs hidden sm:inline-flex"
                                                >
                                                    Setting...
                                                </Badge>
                                            )}
                                            {d.value && (
                                                <Button variant="ghost" size="icon" className="h-8 w-8" asChild>
                                                    <a
                                                        href={`https://${d.value}`}
                                                        target="_blank"
                                                        rel="noopener noreferrer"
                                                        onClick={(e) => e.stopPropagation()}
                                                    >
                                                        <ExternalLink className="h-3.5 w-3.5" />
                                                    </a>
                                                </Button>
                                            )}
                                            <DropdownMenu>
                                                <DropdownMenuTrigger asChild>
                                                    <Button
                                                        variant="ghost"
                                                        size="icon"
                                                        className="h-8 w-8"
                                                        onClick={(e) => e.stopPropagation()}
                                                    >
                                                        <MoreVertical className="h-4 w-4" />
                                                    </Button>
                                                </DropdownMenuTrigger>
                                                <DropdownMenuContent align="end">
                                                    {!d.isVerified && !d.isInternal && (
                                                        <DropdownMenuItem
                                                            onClick={() => onVerify(d.domainId!)}
                                                        >
                                                            <Check className="mr-2 h-4 w-4" />
                                                            Verify DNS
                                                        </DropdownMenuItem>
                                                    )}
                                                    {d.value && (
                                                        <DropdownMenuItem asChild>
                                                            <a
                                                                href={`https://${d.value}`}
                                                                target="_blank"
                                                                rel="noopener noreferrer"
                                                            >
                                                                <ExternalLink className="mr-2 h-4 w-4" />
                                                                Visit Site
                                                            </a>
                                                        </DropdownMenuItem>
                                                    )}
                                                    {d.isVerified && !d.isPrimary && (
                                                        <DropdownMenuItem
                                                            onClick={() => onSetPrimary(d.domainId!)}
                                                        >
                                                            <Star className="mr-2 h-4 w-4" />
                                                            Set as Primary
                                                        </DropdownMenuItem>
                                                    )}
                                                    {!d.isInternal && availableServices.length > 0 && (
                                                        <DropdownMenuSub>
                                                            <DropdownMenuSubTrigger>
                                                                <Network className="mr-2 h-4 w-4" />
                                                                Route to service
                                                            </DropdownMenuSubTrigger>
                                                            <DropdownMenuSubContent>
                                                                <DropdownMenuLabel className="text-xs text-muted-foreground">Service</DropdownMenuLabel>
                                                                {availableServices.map((svc) => (
                                                                    <DropdownMenuItem
                                                                        key={svc}
                                                                        disabled={svc === d.serviceName}
                                                                        onClick={() => onChangeService(d.domainId!, svc, (d.protocol ?? 'Http') as components['schemas']['Api.Data.Protocol'])}
                                                                    >
                                                                        {svc}
                                                                        {svc === d.serviceName && <Check className="ml-auto h-4 w-4" />}
                                                                    </DropdownMenuItem>
                                                                ))}
                                                            </DropdownMenuSubContent>
                                                        </DropdownMenuSub>
                                                    )}
                                                    {!d.isInternal && environments.length > 1 && (
                                                        <DropdownMenuSub>
                                                            <DropdownMenuSubTrigger>
                                                                <Globe className="mr-2 h-4 w-4" />
                                                                Route to environment
                                                            </DropdownMenuSubTrigger>
                                                            <DropdownMenuSubContent>
                                                                <DropdownMenuLabel className="text-xs text-muted-foreground">Environment</DropdownMenuLabel>
                                                                {environments.map((e) => (
                                                                    <DropdownMenuItem
                                                                        key={e.id}
                                                                        disabled={e.id === d.environmentId}
                                                                        onClick={() => onChangeEnvironment(d.domainId!, e.id!)}
                                                                    >
                                                                        {e.name ?? e.id}
                                                                        {e.id === d.environmentId && <Check className="ml-auto h-4 w-4" />}
                                                                    </DropdownMenuItem>
                                                                ))}
                                                                <DropdownMenuSeparator />
                                                                <DropdownMenuItem
                                                                    disabled={!d.environmentId}
                                                                    onClick={() => onChangeEnvironment(d.domainId!, null)}
                                                                >
                                                                    Unassigned
                                                                    {!d.environmentId && <Check className="ml-auto h-4 w-4" />}
                                                                </DropdownMenuItem>
                                                            </DropdownMenuSubContent>
                                                        </DropdownMenuSub>
                                                    )}
                                                    {!d.isInternal && (
                                                        <>
                                                            <DropdownMenuSeparator />
                                                            <DropdownMenuItem
                                                                className="text-red-500 focus:text-red-500 focus:bg-red-500/10"
                                                                onClick={() => onDelete(d.domainId!)}
                                                            >
                                                                <Trash2 className="mr-2 h-4 w-4" />
                                                                Delete
                                                            </DropdownMenuItem>
                                                        </>
                                                    )}
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
