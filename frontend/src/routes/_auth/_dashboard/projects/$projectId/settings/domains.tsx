import { api } from '@/api-client'
import { useAppForm } from '@/components/form/use-app-form'
import LoadingButton from '@/components/loading-button'
import { Badge } from '@/components/ui/badge'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { createFileRoute } from '@tanstack/react-router'
import { useEffect, useState, useMemo, useCallback, useRef } from 'react'
import { z } from "zod"

import VerifyDnsDialog from './!components/verify-dns-dialog'
import AreYouSureDialog from '@/components/are-you-sure-dialog'
import { Button } from '@/components/ui/button'
import { Trash2, MoreVertical, Check, Globe, Server, Clock, Star, ExternalLink, Network } from 'lucide-react'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { CopyButton } from '@/components/copy-button'
import { CopyText } from '@/components/copy-text'
import { cn } from '@/lib/utils'
import { components } from '@/generated';

type DomainDto = NonNullable<Awaited<ReturnType<typeof api.v1.getProjectsProjectIdDomains.fetchQuery>>["domains"]>[number]

function DomainIcon({ protocol, isPrimary, isInternal, className }: {
  protocol?: string | null
  isPrimary?: boolean
  isInternal?: boolean
  className?: string
}) {
  if (isPrimary) return <Star className={className} />
  if (isInternal) return <Server className={className} />
  if (protocol?.toLowerCase() === 'tcp') return <Network className={className} />
  return <Globe className={className} />
}

interface DomainItemProps {
  domain: DomainDto
  onVerify: (domainId: string) => void
  onDelete: (domainId: string) => void
  onSetPrimary: (domainId: string) => void
  isSettingPrimary: boolean
  variant?: 'primary' | 'verified' | 'pending'
}

function DomainItem({ domain, onVerify, onDelete, onSetPrimary, isSettingPrimary, variant = 'verified' }: DomainItemProps) {
  const isPrimary = variant === 'primary'
  const isPending = variant === 'pending'

  return (
    <div
      className={cn(
        "group relative p-4 rounded-lg border transition-all duration-200",
        isPrimary && "bg-primary/5 border-primary/20 hover:border-primary/40",
        !isPrimary && !isPending && "bg-zinc-900/30 hover:bg-zinc-900/50 border-zinc-800",
        isPending && "bg-amber-500/5 border-amber-500/20 hover:border-amber-500/40 cursor-pointer",
        domain.isInternal && "border-dashed"
      )}
      onClick={isPending ? () => onVerify(domain.domainId!) : undefined}
    >
      <div className="flex items-start gap-3 sm:gap-4">
        <div className={cn(
          "flex-shrink-0 w-10 h-10 rounded-lg flex items-center justify-center",
          isPrimary && "bg-primary/10 text-primary",
          !isPrimary && !isPending && "bg-zinc-800 text-muted-foreground",
          isPending && "bg-amber-500/10 text-amber-600"
        )}>
          <DomainIcon
            protocol={domain.protocol}
            isPrimary={isPrimary}
            isInternal={domain.isInternal}
            className="w-5 h-5"
          />
        </div>

        <div className="min-w-0 flex-1">
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0 flex-1">
              <div onClick={(e) => e.stopPropagation()}>
                <CopyText value={domain.value ?? ''}>
                  <h3 className={cn(
                    "font-mono text-sm font-medium truncate cursor-pointer",
                    isPrimary && "text-primary",
                    isPending && "text-amber-600"
                  )}>
                    {domain.value}
                  </h3>
                </CopyText>
              </div>
              <div className="flex items-center gap-2 mt-1 flex-wrap">
                <span className="text-sm text-muted-foreground">{domain.serviceName}</span>
                {domain.protocol && (
                  <span className="text-xs text-muted-foreground/60 font-mono uppercase">{domain.protocol}</span>
                )}
                {domain.isInternal && (
                  <Badge variant="outline" className="text-[10px] font-normal border-dashed border-zinc-700 text-muted-foreground">
                    Auto-generated
                  </Badge>
                )}
              </div>
            </div>

            <div className="flex items-center gap-1 flex-shrink-0">
              {isSettingPrimary && (
                <Badge variant="secondary" className="animate-pulse text-xs hidden sm:inline-flex">
                  Setting as primary...
                </Badge>
              )}
              {!domain.isVerified && !domain.isInternal && (
                <Badge
                  variant="outline"
                  className="border-amber-500/30 text-amber-600 bg-amber-500/10 cursor-pointer hover:bg-amber-500/20 transition-colors text-xs"
                  onClick={(e) => { e.stopPropagation(); onVerify(domain.domainId!); }}
                >
                  <Clock className="w-3 h-3 mr-1" />
                  <span className="hidden sm:inline">Unverified</span>
                  <span className="sm:hidden">!</span>
                </Badge>
              )}
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="sm:opacity-0 sm:group-hover:opacity-100 transition-opacity h-8 w-8"
                    onClick={(e) => e.stopPropagation()}
                  >
                    <MoreVertical className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  {!domain.isVerified && !domain.isInternal && (
                    <DropdownMenuItem onClick={() => onVerify(domain.domainId!)}>
                      <Check className="mr-2 h-4 w-4" />
                      Verify DNS
                    </DropdownMenuItem>
                  )}
                  {domain.value && (
                    <CopyButton
                      value={domain.value}
                      className="gap-4"
                      iconClassName="text-primary"
                    >
                      <span className="text-sm">Copy Domain</span>
                    </CopyButton>
                  )}
                  {domain.value && (
                    <DropdownMenuItem asChild>
                      <a href={`https://${domain.value}`} target="_blank" rel="noopener noreferrer">
                        <ExternalLink className="mr-2 h-4 w-4" />
                        Visit Site
                      </a>
                    </DropdownMenuItem>
                  )}
                  {domain.isVerified && !domain.isPrimary && (
                    <DropdownMenuItem onClick={() => onSetPrimary(domain.domainId!)}>
                      <Star className="mr-2 h-4 w-4" />
                      Set as primary
                    </DropdownMenuItem>
                  )}
                  {!domain.isInternal && (
                    <DropdownMenuItem
                      className="text-red-500 focus:text-red-500 focus:bg-red-500/10"
                      onClick={() => onDelete(domain.domainId!)}
                    >
                      <Trash2 className="mr-2 h-4 w-4" />
                      Delete
                    </DropdownMenuItem>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

interface DomainSectionProps {
  title: string
  description?: string
  children: React.ReactNode
  isEmpty?: boolean
  emptyMessage?: string
}

function DomainSection({ title, description, children, isEmpty, emptyMessage }: DomainSectionProps) {
  if (isEmpty) {
    return null
  }

  return (
    <div className="space-y-3">
      <div>
        <h3 className="text-xs font-medium text-muted-foreground uppercase tracking-widest">{title}</h3>
        {description && <p className="text-xs text-muted-foreground/60 mt-0.5">{description}</p>}
      </div>
      <div className="space-y-2">
        {children}
      </div>
    </div>
  )
}

export const Route = createFileRoute(
  '/_auth/_dashboard/projects/$projectId/settings/domains',
)({
  component: DomainsSettingsTab,
  loader: async ({ params: { projectId } }) => {
    await Promise.all([
      api.v1.getProjectsProjectIdDomains.prefetchQuery({ parameters: { path: { projectId } } }),
      api.v1.getProjectsProjectIdServices.prefetchQuery({ parameters: { path: { projectId } } }),
      api.v1.getProjectsProjectId.prefetchQuery({ parameters: { path: { projectId } } }),
    ]);
  }
})

function DomainsSettingsTab() {
  const { projectId } = Route.useParams();

  const { data: projectData } = api.v1.getProjectsProjectId.useQuery({ path: { projectId } });

  const { data: getProjectDomainsData } = api.v1.getProjectsProjectIdDomains.useQuery({
    path: {
      projectId
    }
  });

  const data = useMemo(() => {
    //TODO: N8N SPECIAL RULE, that needs to be handled in the backend at a later point!!
    if (projectData?.type === "N8n") {
      return {
        ...getProjectDomainsData,
        domains: [...(getProjectDomainsData?.domains?.filter(x => x.serviceName?.toLowerCase() !== "backend") ?? [])]
      };
    }

    return getProjectDomainsData;
  }, [getProjectDomainsData, projectData]);

  const { data: servicesData } = api.v1.getProjectsProjectIdServices.useQuery({
    path: {
      projectId
    }
  });

  const availableServices = useMemo(() =>
    servicesData?.services?.map(s => s.name).filter((name): name is string => !!name) ?? [],
    [servicesData]
  );

  // Map service names to their ports for protocol guessing
  const servicePortMap = useMemo(() => {
    const map = new Map<string, string>();
    servicesData?.services?.forEach(s => {
      if (s.name && s.port) {
        map.set(s.name, s.port);
      }
    });
    return map;
  }, [servicesData]);

  // Guess protocol based on common port mappings
  const guessProtocol = useCallback((serviceName: string): components["schemas"]["Api.Data.Protocol"] => {
    const port = servicePortMap.get(serviceName);
    if (!port) return 'Http';

    const portNum = parseInt(port);
    const tcpPorts = [5432, 3306, 6379, 27017, 1433, 5433, 3389, 22, 21, 25, 110, 143, 587];

    return tcpPorts.includes(portNum) ? 'Tcp' : 'Http';
  }, [servicePortMap]);

  const addDomainSchema = useMemo(() => z.object({
    domain: z.string()
      .transform((val) => val.trim())
      .refine((val) => val.length === 0 || val.length >= 4, { message: 'Domain must be at least 4 characters' })
      .refine((val) => val.length === 0 || val.length <= 255, { message: 'Domain must be at most 255 characters' })
      .refine((val) => val.length === 0 || /^[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,}$/.test(val), { message: 'Invalid domain name' }),
    serviceName: availableServices.length > 0
      ? z.enum(availableServices as [string, ...string[]])
      : z.string(),
    protocol: z.enum(["Http", "Tcp"] satisfies components["schemas"]["Api.Data.Protocol"][]),
  }), [availableServices]);

  const [deleteDomainId, setDeleteDomainId] = useState<string | null>(null);

  const [selectedDomainId, setSelectedDomainId] = useState<string | null>(null);

  const invalidate = () => {
    api.v1.getProjectsProjectIdDomains.invalidateQueries({
      parameters: {
        path: {
          projectId
        }
      }
    });
  }


  const { mutateAsync } = api.v1.postProjectsProjectIdDomains.useMutation({
    path: {
      projectId
    }
  }, {
    onSuccess: ({ domainId }) => {
      invalidate();
      setSelectedDomainId(domainId || null);
    },
  });

  const { mutateAsync: deleteDomainMutateAsync } = api.v1.deleteProjectsProjectIdDomainsDomainId.useMutation({
    path: {
      projectId,
      domainId: deleteDomainId!
    }
  }, {
    onSuccess: () => {
      invalidate();
    }
  })

  const [selectedSetPrimaryDomainId, setSelectedSetPrimaryDomainId] = useState<string | null>(null);

  const { mutateAsync: setPrimaryDomainMutateAsync } = api.v1.postProjectsProjectIdDomainsDomainIdSetPrimary.useMutation({
    path: {
      projectId,
      domainId: selectedSetPrimaryDomainId!
    }
  }, {
    onSuccess: () => {
      invalidate();
      api.v1.getProjectsProjectId.invalidateQueries({
        parameters: {
          path: {
            projectId: projectId
          }
        }
      });
    }
  })

  const deleteDomainAsync = async () => {
    if (!deleteDomainId) return;

    await deleteDomainMutateAsync();
  }

  useEffect(() => {
    if (!selectedSetPrimaryDomainId) return;

    setPrimaryDomainAsync(selectedSetPrimaryDomainId).finally(() => {
      setSelectedSetPrimaryDomainId(null);
    });
  }, [selectedSetPrimaryDomainId])

  const setPrimaryDomainAsync = async (domainId: string) => {
    if (!domainId) return;

    await setPrimaryDomainMutateAsync();
  }

  const addDomainForm = useAppForm({
    defaultValues: {
      domain: '',
      serviceName: availableServices[0] ?? '',
      protocol: guessProtocol(availableServices[0] ?? '')
    },
    validators: {
      onChange: addDomainSchema
    },
    onSubmit: async ({ value }) => {
      await mutateAsync(value);
    }
  });

  const [serviceName, setServiceName] = useState("");
  const prevServiceNameRef = useRef<string>(serviceName);

  useEffect(() => {
    if (prevServiceNameRef.current !== serviceName) {
      prevServiceNameRef.current = serviceName;
      const guessedProtocol = guessProtocol(serviceName);
      addDomainForm.setFieldValue('protocol', guessedProtocol, { dontUpdateMeta: true });
    }
  }, [serviceName, guessProtocol, addDomainForm]);

  // Group domains by status, sorting external domains before internal ones
  const groupedDomains = useMemo(() => {
    const domains = data?.domains ?? []
    const sortByExternal = (a: DomainDto, b: DomainDto) =>
      (a.isInternal ? 1 : 0) - (b.isInternal ? 1 : 0)
    const primary = domains.filter(d => d.isPrimary).sort(sortByExternal)
    const verified = domains.filter(d => !d.isPrimary && d.isVerified).sort(sortByExternal)
    const pending = domains.filter(d => !d.isPrimary && !d.isVerified).sort(sortByExternal)
    return { primary, verified, pending }
  }, [data?.domains])

  const handleVerify = useCallback((domainId: string) => {
    setSelectedDomainId(domainId)
  }, [])

  const handleDelete = useCallback((domainId: string) => {
    setDeleteDomainId(domainId)
  }, [])

  const handleSetPrimary = useCallback((domainId: string) => {
    setSelectedSetPrimaryDomainId(domainId)
  }, [])

  return (
    <div className="space-y-6">
      <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
        <CardHeader>
          <CardTitle className="text-lg">Domains</CardTitle>
          <CardDescription>Manage domains for your project. Your primary domain is used as the main entry point.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-8">
          {/* Primary Domain Section */}
          <DomainSection
            title="Primary Domain"
            description="Your main domain used for redirects and as the default entry point"
            isEmpty={groupedDomains.primary.length === 0}
          >
            {groupedDomains.primary.map((domain) => (
              <DomainItem
                key={domain.domainId}
                domain={domain}
                variant="primary"
                onVerify={handleVerify}
                onDelete={handleDelete}
                onSetPrimary={handleSetPrimary}
                isSettingPrimary={selectedSetPrimaryDomainId === domain.domainId}
              />
            ))}
          </DomainSection>

          {/* Verified Domains Section */}
          <DomainSection
            title="Verified Domains"
            description="These domains are active and routing traffic to your services"
            isEmpty={groupedDomains.verified.length === 0}
          >
            {groupedDomains.verified.map((domain) => (
              <DomainItem
                key={domain.domainId}
                domain={domain}
                variant="verified"
                onVerify={handleVerify}
                onDelete={handleDelete}
                onSetPrimary={handleSetPrimary}
                isSettingPrimary={selectedSetPrimaryDomainId === domain.domainId}
              />
            ))}
          </DomainSection>

          {/* Pending Verification Section */}
          <DomainSection
            title="Pending Verification"
            description="Configure DNS records to activate these domains"
            isEmpty={groupedDomains.pending.length === 0}
          >
            {groupedDomains.pending.map((domain) => (
              <DomainItem
                key={domain.domainId}
                domain={domain}
                variant="pending"
                onVerify={handleVerify}
                onDelete={handleDelete}
                onSetPrimary={handleSetPrimary}
                isSettingPrimary={selectedSetPrimaryDomainId === domain.domainId}
              />
            ))}
          </DomainSection>

          {/* Add Custom Domain Section */}
          <DomainSection
            title="Add Custom Domain"
            description="Connect your own domain to this project"
          >
            <form onSubmit={(e) => {
              e.preventDefault();
              addDomainForm.handleSubmit();
            }}>
              <div className="group relative flex items-center p-4 rounded-lg border border-dashed border-zinc-800 hover:border-zinc-700 bg-zinc-900/30 hover:bg-zinc-900/50 transition-all duration-200">
                <addDomainForm.Subscribe selector={(state) => state.values.protocol}>
                  {(protocol) => (
                    <div className="flex-shrink-0 w-10 h-10 rounded-lg flex items-center justify-center bg-zinc-800 text-muted-foreground mr-4">
                      <DomainIcon protocol={protocol} className="w-5 h-5" />
                    </div>
                  )}
                </addDomainForm.Subscribe>
                <div className="flex flex-col sm:flex-row gap-3 flex-1 items-start sm:items-center">
                  <addDomainForm.AppField name='domain' children={(field) => (
                    <field.TextField className='flex-1 min-w-0 font-mono' placeholder='example.com' />
                  )} />
                  <div className="flex flex-col sm:flex-row gap-3 w-full sm:w-auto">
                    <addDomainForm.AppField name="serviceName" listeners={{
                      onChange: (field) => setServiceName(field.value)
                    }} children={(field) => (
                      <field.SelectField className='w-full sm:w-[160px]' values={availableServices} />
                    )} />
                    <addDomainForm.AppField name="protocol" children={(field) => (
                      <field.SelectField className='w-full sm:w-[100px]' values={['Http', 'Tcp']} />
                    )} />
                    <addDomainForm.Subscribe
                      selector={(state) => [state.canSubmit, state.isSubmitting, state.values.domain]} children={([canSubmit, isSubmitting, domainValue]) => (
                        <LoadingButton isLoading={!!isSubmitting} disabled={!canSubmit || !domainValue?.toString().trim()} type='submit' className="w-full sm:w-auto">
                          Add
                        </LoadingButton>
                      )} />
                  </div>
                </div>
              </div>
            </form>
          </DomainSection>

          <VerifyDnsDialog
            selectedDomainId={selectedDomainId}
            onClose={() => { invalidate(); setSelectedDomainId(null); }}
            projectId={projectId}
            domains={data?.domains ?? []} />

          <AreYouSureDialog
            title='Are you sure you want to delete this domain?'
            subtitle="This action cannot be undone. You'll need to add and verify the domain again."
            open={!!deleteDomainId}
            onAnswer={(ans) => {
              if (ans && deleteDomainId) {
                deleteDomainAsync().finally(() => {
                  setDeleteDomainId(null);
                });
              } else {
                setDeleteDomainId(null);
              }
            }} />
        </CardContent>
      </Card>
    </div>
  )
}

export default DomainsSettingsTab
