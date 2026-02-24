import { api, apiBaseUrl } from '@/api-client'
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
import { useMutation } from '@tanstack/react-query'

import VerifyDnsDialog from './!components/verify-dns-dialog'
import AreYouSureDialog from '@/components/are-you-sure-dialog'
import { Button } from '@/components/ui/button'
import { Trash2, MoreVertical, Check, Globe, Server, Clock, Star, ExternalLink, Network, Database, EyeOff, ChevronDown } from 'lucide-react'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { CopyButton } from '@/components/copy-button'
import { CopyText } from '@/components/copy-text'
import { cn } from '@/lib/utils'
import { components } from '@/generated'
import { useToast } from '@/hooks/use-toast'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip'

type DomainDto = NonNullable<Awaited<ReturnType<typeof api.v1.getProjectsProjectIdDomains.fetchQuery>>["domains"]>[number]

type ServiceDto = NonNullable<NonNullable<Awaited<ReturnType<typeof api.v1.getProjectsProjectIdServices.fetchQuery>>>["services"]>[number]

// TCP ports that are classified as database/TCP services
const TCP_PORTS = new Set([5432, 3306, 6379, 27017, 1433, 5433, 3389, 22])

interface ExposeTcpResponse {
  allocatedPort: number
  host: string
}

async function exposeTcpService(projectId: string, serviceName: string): Promise<ExposeTcpResponse> {
  const res = await fetch(
    `${apiBaseUrl}/v1/projects/${encodeURIComponent(projectId)}/services/${encodeURIComponent(serviceName)}/expose-tcp`,
    { method: 'POST', credentials: 'include' },
  )
  if (!res.ok) {
    const text = await res.text()
    throw new Error(text || `Failed to expose TCP service: ${res.status}`)
  }
  return res.json()
}

async function unexposeTcpService(projectId: string, serviceName: string): Promise<void> {
  const res = await fetch(
    `${apiBaseUrl}/v1/projects/${encodeURIComponent(projectId)}/services/${encodeURIComponent(serviceName)}/expose-tcp`,
    { method: 'DELETE', credentials: 'include' },
  )
  if (!res.ok) {
    const text = await res.text()
    throw new Error(text || `Failed to unexpose TCP service: ${res.status}`)
  }
}

async function updateServiceProtocol(projectId: string, serviceName: string, protocol: 'Http' | 'Tcp'): Promise<void> {
  const res = await fetch(
    `${apiBaseUrl}/v1/projects/${encodeURIComponent(projectId)}/services/${encodeURIComponent(serviceName)}/protocol`,
    {
      method: 'PATCH',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ protocol }),
    },
  )
  if (!res.ok) {
    const text = await res.text()
    throw new Error(text || `Failed to update protocol: ${res.status}`)
  }
}

function getConnectionStringTemplate(serviceName: string, port: string, host: string, allocatedPort: number): string {
  const portNum = parseInt(port)
  if (portNum === 5432) return `postgresql://user:password@${host}:${allocatedPort}/dbname`
  if (portNum === 3306) return `mysql://user:password@${host}:${allocatedPort}/dbname`
  if (portNum === 6379) return `redis://${host}:${allocatedPort}`
  if (portNum === 27017) return `mongodb://user:password@${host}:${allocatedPort}/dbname`
  return `${host}:${allocatedPort}`
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

function ServiceIcon({ port, protocol, className }: {
  port?: string | null
  protocol?: string
  className?: string
}) {
  if (protocol === 'Http') return <Globe className={className} />

  const portNum = parseInt(port ?? '0')
  if (TCP_PORTS.has(portNum)) return <Database className={className} />
  if (protocol === 'Tcp') return <Network className={className} />

  return <Globe className={className} />
}

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

// Extended service type with TCP allocation fields (until backend schema is regenerated)
interface ServiceWithTcp extends ServiceDto {
  allocatedTcpPort?: number | null
  tcpHost?: string | null
}

interface ServiceItemProps {
  service: ServiceWithTcp
  domain?: DomainDto
  projectId: string
  onExpose: (serviceName: string) => void
  onUnexpose: (serviceName: string) => void
  onProtocolChange: (serviceName: string, protocol: 'Http' | 'Tcp') => void
  isExposing: boolean
  isUnexposing: boolean
  isChangingProtocol: boolean
}

function ServiceItem({ service, domain, projectId, onExpose, onUnexpose, onProtocolChange, isExposing, isUnexposing, isChangingProtocol }: ServiceItemProps) {
  const isHttp = service.protocol === 'Http'
  const isTcp = service.protocol === 'Tcp'
  const isExposed = isTcp && !!service.allocatedTcpPort && !!service.tcpHost
  const internalDomainValue = domain?.value

  const connectionString = isExposed
    ? getConnectionStringTemplate(service.name!, service.port!, service.tcpHost!, service.allocatedTcpPort!)
    : null

  const typeLabel = isTcp ? getServiceTypeLabel(service.port!) : 'HTTP'

  return (
    <div className="group relative p-4 rounded-lg border border-zinc-800 bg-zinc-900/30 hover:bg-zinc-900/50 transition-all duration-200">
      <div className="flex items-start gap-3 sm:gap-4">
        <div className="flex-shrink-0 w-10 h-10 rounded-lg flex items-center justify-center bg-zinc-800 text-muted-foreground">
          <ServiceIcon port={service.port} protocol={service.protocol} className="w-5 h-5" />
        </div>

        <div className="min-w-0 flex-1">
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0 flex-1">
              <div className="flex items-center gap-2">
                <h3 className="font-medium text-sm">{service.name}</h3>
                <span className="text-xs text-muted-foreground/60 font-mono">:{service.port}</span>
              </div>
              <div className="flex items-center gap-2 mt-1 flex-wrap">
                <Badge variant="outline" className="text-[10px] font-normal border-zinc-700 text-muted-foreground font-mono">
                  {typeLabel}
                </Badge>

                {/* HTTP service: show domain link */}
                {isHttp && internalDomainValue && (
                  <a
                    href={`https://${internalDomainValue}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-xs text-muted-foreground hover:text-foreground transition-colors font-mono truncate flex items-center gap-1"
                  >
                    {internalDomainValue}
                    <ExternalLink className="w-3 h-3 flex-shrink-0" />
                  </a>
                )}

                {/* TCP service: not exposed */}
                {isTcp && !isExposed && (
                  <span className="text-xs text-muted-foreground/60">Internal only</span>
                )}

                {/* TCP service: exposed */}
                {isTcp && isExposed && (
                  <CopyText value={connectionString!}>
                    <span className="text-xs text-muted-foreground hover:text-foreground transition-colors font-mono cursor-pointer">
                      {service.tcpHost}:{service.allocatedTcpPort}
                    </span>
                  </CopyText>
                )}
              </div>
            </div>

            <div className="flex items-center gap-1.5 flex-shrink-0">
              {/* Protocol toggle */}
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-7 px-2 text-xs text-muted-foreground hover:text-foreground"
                    disabled={isChangingProtocol}
                  >
                    {service.protocol}
                    <ChevronDown className="ml-1 h-3 w-3" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem
                    onClick={() => onProtocolChange(service.name!, 'Http')}
                    disabled={service.protocol === 'Http'}
                  >
                    <Globe className="mr-2 h-4 w-4" />
                    HTTP
                    {service.protocol === 'Http' && <Check className="ml-auto h-4 w-4" />}
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    onClick={() => onProtocolChange(service.name!, 'Tcp')}
                    disabled={service.protocol === 'Tcp'}
                  >
                    <Network className="mr-2 h-4 w-4" />
                    TCP
                    {service.protocol === 'Tcp' && <Check className="ml-auto h-4 w-4" />}
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>

              {/* TCP expose/unexpose actions */}
              {isTcp && !isExposed && (
                <LoadingButton
                  isLoading={isExposing}
                  variant="outline"
                  size="sm"
                  className="h-7 px-3 text-xs"
                  onClick={() => onExpose(service.name!)}
                >
                  Expose
                </LoadingButton>
              )}

              {isTcp && isExposed && (
                <div className="flex items-center gap-1">
                  <TooltipProvider>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <CopyButton value={connectionString!} className="h-7 w-7 p-0">
                          <span className="sr-only">Copy connection string</span>
                        </CopyButton>
                      </TooltipTrigger>
                      <TooltipContent side="bottom">
                        <p className="font-mono text-xs max-w-xs break-all">{connectionString}</p>
                      </TooltipContent>
                    </Tooltip>
                  </TooltipProvider>
                  <LoadingButton
                    isLoading={isUnexposing}
                    variant="ghost"
                    size="sm"
                    className="h-7 px-2 text-xs text-muted-foreground hover:text-red-500"
                    onClick={() => onUnexpose(service.name!)}
                  >
                    <EyeOff className="h-3 w-3 mr-1" />
                    Unexpose
                  </LoadingButton>
                </div>
              )}

              {/* HTTP external link */}
              {isHttp && internalDomainValue && (
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7"
                  asChild
                >
                  <a href={`https://${internalDomainValue}`} target="_blank" rel="noopener noreferrer">
                    <ExternalLink className="h-3.5 w-3.5" />
                  </a>
                </Button>
              )}
            </div>
          </div>
        </div>
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

  const { toast } = useToast();

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

  const services = useMemo(() =>
    (servicesData?.services ?? []) as ServiceWithTcp[],
    [servicesData]
  );

  // Only HTTP services are available for custom domain assignment
  const httpServices = useMemo(() =>
    services.filter(s => s.protocol === 'Http'),
    [services]
  );

  const availableServices = useMemo(() =>
    httpServices.map(s => s.name).filter((name): name is string => !!name),
    [httpServices]
  );

  // Map service names to their ports for protocol guessing
  const servicePortMap = useMemo(() => {
    const map = new Map<string, string>();
    services.forEach(s => {
      if (s.name && s.port) {
        map.set(s.name, s.port);
      }
    });
    return map;
  }, [services]);

  // Build a map of service name -> internal domain for display in services table
  const serviceDomainMap = useMemo(() => {
    const map = new Map<string, DomainDto>();
    const domains = data?.domains ?? [];
    domains.forEach(d => {
      if (d.isInternal && d.serviceName) {
        map.set(d.serviceName, d);
      }
    });
    return map;
  }, [data?.domains]);

  // Guess protocol based on common port mappings
  const guessProtocol = useCallback((serviceName: string): components["schemas"]["Api.Data.Protocol"] => {
    const port = servicePortMap.get(serviceName);
    if (!port) return 'Http';

    const portNum = parseInt(port);
    return TCP_PORTS.has(portNum) ? 'Tcp' : 'Http';
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

  const invalidateServices = () => {
    api.v1.getProjectsProjectIdServices.invalidateQueries({
      parameters: {
        path: {
          projectId
        }
      }
    });
  }

  const invalidateAll = () => {
    invalidate();
    invalidateServices();
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

  // TCP expose mutation
  const [exposingService, setExposingService] = useState<string | null>(null);
  const exposeTcpMutation = useMutation({
    mutationFn: (serviceName: string) => exposeTcpService(projectId, serviceName),
    onMutate: (serviceName) => setExposingService(serviceName),
    onSuccess: (_data, serviceName) => {
      invalidateAll();
      toast({
        title: 'TCP service exposed',
        description: `${serviceName} is now accessible externally.`,
      });
    },
    onError: (error) => {
      toast({
        title: 'Failed to expose service',
        description: error instanceof Error ? error.message : String(error),
        variant: 'destructive',
      });
    },
    onSettled: () => setExposingService(null),
  });

  // TCP unexpose mutation
  const [unexposingService, setUnexposingService] = useState<string | null>(null);
  const unexposeTcpMutation = useMutation({
    mutationFn: (serviceName: string) => unexposeTcpService(projectId, serviceName),
    onMutate: (serviceName) => setUnexposingService(serviceName),
    onSuccess: (_data, serviceName) => {
      invalidateAll();
      toast({
        title: 'TCP service unexposed',
        description: `${serviceName} is now internal only.`,
      });
    },
    onError: (error) => {
      toast({
        title: 'Failed to unexpose service',
        description: error instanceof Error ? error.message : String(error),
        variant: 'destructive',
      });
    },
    onSettled: () => setUnexposingService(null),
  });

  // Protocol change mutation
  const [changingProtocolService, setChangingProtocolService] = useState<string | null>(null);
  const changeProtocolMutation = useMutation({
    mutationFn: ({ serviceName, protocol }: { serviceName: string; protocol: 'Http' | 'Tcp' }) =>
      updateServiceProtocol(projectId, serviceName, protocol),
    onMutate: ({ serviceName }) => setChangingProtocolService(serviceName),
    onSuccess: (_data, { serviceName, protocol }) => {
      invalidateAll();
      toast({
        title: 'Protocol updated',
        description: `${serviceName} is now configured as ${protocol}.`,
      });
    },
    onError: (error) => {
      toast({
        title: 'Failed to update protocol',
        description: error instanceof Error ? error.message : String(error),
        variant: 'destructive',
      });
    },
    onSettled: () => setChangingProtocolService(null),
  });

  const handleExpose = useCallback((serviceName: string) => {
    exposeTcpMutation.mutate(serviceName);
  }, [exposeTcpMutation]);

  const handleUnexpose = useCallback((serviceName: string) => {
    unexposeTcpMutation.mutate(serviceName);
  }, [unexposeTcpMutation]);

  const handleProtocolChange = useCallback((serviceName: string, protocol: 'Http' | 'Tcp') => {
    changeProtocolMutation.mutate({ serviceName, protocol });
  }, [changeProtocolMutation]);

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
      protocol: 'Http' as components["schemas"]["Api.Data.Protocol"]
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
      {/* Services Section */}
      <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
        <CardHeader>
          <CardTitle className="text-lg">Services</CardTitle>
          <CardDescription>All detected services in your project. HTTP services get auto-generated domains. TCP services can be exposed externally.</CardDescription>
        </CardHeader>
        <CardContent>
          {services.length === 0 ? (
            <div className="text-sm text-muted-foreground/60 text-center py-8">
              No services detected yet. Deploy your project to see services here.
            </div>
          ) : (
            <div className="space-y-2">
              {services.map((service) => (
                <ServiceItem
                  key={service.name}
                  service={service}
                  domain={serviceDomainMap.get(service.name!)}
                  projectId={projectId}
                  onExpose={handleExpose}
                  onUnexpose={handleUnexpose}
                  onProtocolChange={handleProtocolChange}
                  isExposing={exposingService === service.name}
                  isUnexposing={unexposingService === service.name}
                  isChangingProtocol={changingProtocolService === service.name}
                />
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Custom Domains Section */}
      <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
        <CardHeader>
          <CardTitle className="text-lg">Custom Domains</CardTitle>
          <CardDescription>Manage custom domains for your HTTP services. Your primary domain is used as the main entry point.</CardDescription>
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
            description="Connect your own domain to an HTTP service"
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
