import { Badge } from '@/components/ui/badge'
import { Formatter } from '@/lib/formatter'
import { getProjectStateFromNumber as getProjectStateFromStateString, ProjectState } from '@/lib/project-state'
import { createFileRoute, getRouteApi, Link } from '@tanstack/react-router'
import { Cpu, ExternalLink, Globe, HardDrive, HelpCircle, MemoryStick, Settings, Star, Github } from 'lucide-react'
import { UsageGraph } from '@/components/usage-graph'
import StartStopProjectButton from '@/components/project/project-startstop-button'
import { api } from '@/api-client'
import { useMemo } from 'react'
import { Skeleton } from '@/components/ui/skeleton'
import { onN8nIntent } from '@/hooks/use-n8n-intent-ephemeral-storage.hook'
import { useModal } from '@/hooks/use-modal'
import confetti from 'canvas-confetti'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { motion } from 'framer-motion'

export const Route = createFileRoute('/_auth/_dashboard/projects/$projectId/')({
    component: RouteComponent,
})

function RouteComponent() {
    const { projectId } = getRouteApi("/_auth/_dashboard/projects/$projectId").useLoaderData();
    const { data: project } = api.v1.getProjectsProjectId.useQuery({ path: { projectId } }, {
        refetchInterval: (project) => project.state.data?.state === ProjectState.Starting ? 2000 : 30000,
    })

    const modal = useModal();

    const getStateColor = (state: ProjectState) => {
        switch (state) {
            case ProjectState.Running:
                return 'bg-green-500'
            case ProjectState.Starting:
                return 'bg-yellow-500'
            case ProjectState.Stopped:
                return 'bg-red-500'
            default:
                return 'bg-gray-500'
        }
    }

    const getStateGlow = (state: ProjectState) => {
        switch (state) {
            case ProjectState.Running:
                return 'shadow-[0_0_8px_rgba(34,197,94,0.4)]'
            case ProjectState.Starting:
                return 'shadow-[0_0_8px_rgba(234,179,8,0.4)]'
            case ProjectState.Stopped:
                return 'shadow-[0_0_8px_rgba(239,68,68,0.4)]'
            default:
                return ''
        }
    }

    if (!project) {
        return (
            <div className="text-center py-10">
                <p>No project data available</p>
            </div>
        )
    }

    const { data: getProjectDomainsData } = api.v1.getProjectsProjectIdDomains.useQuery({
        path: {
            projectId
        }
    });

    const domains = useMemo(() => {
        let returningDomains = getProjectDomainsData?.domains ?? [];
        //TODO: N8N SPECIAL RULE, that needs to be handled in the backend at a later point!!
        if (project?.type === "N8n") {
            returningDomains = [...(returningDomains.filter(x => x.serviceName?.toLowerCase() !== "backend"))]
        }

        return returningDomains.map(d => d.value);
    }, [getProjectDomainsData, project]);

    onN8nIntent(async (intent) => {
        if (intent.action == "created" && intent.projectId === project.id && project.primaryDomain) {
            // Trigger confetti animation
            confetti({
                particleCount: 100,
                spread: 70,
                origin: { y: 0.6 }
            });

            const result = await modal.confirm(
                "Congratulations! 🎉",
                "Your n8n project has been created! Do you want to go to the instance?",
                {
                    confirmText: "Yes, take me there now",
                    cancelText: "No"
                }
            );

            if (result) {
                window.open(`https://${project.primaryDomain}`, '_blank');
            }
        }
    });

    const { data: tiers } = api.v1.getServersTiers.useQuery();

    const serverTier = useMemo(() => {
        return tiers?.tiers?.find(t => t.id?.value === project.serverTier);
    }, [tiers, project.serverTier]);

    const getTemplateTypeDisplay = (type: string) => {
        switch (type) {
            case 'Dockerfile':
                return { label: 'Dockerfile', description: 'Single Dockerfile deployment' };
            case 'DockerCompose':
                return { label: 'Docker Compose', description: 'Multi-container Docker Compose deployment' };
            case 'Compozerr':
                return { label: 'Compozerr Standard', description: 'Frontend + Backend deployment structure' };
            case 'N8n':
                return { label: 'n8n', description: 'n8n workflow automation platform' };
            default:
                return { label: type, description: 'Unknown deployment type' };
        }
    };

    const templateType = getTemplateTypeDisplay(project.type);
    const currentState = getProjectStateFromStateString(project.state);

    return (
        <div className="space-y-6">
            {/* Hero Section */}
            <motion.div
                initial={{ opacity: 0, y: 12 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.3 }}
                className="space-y-4"
            >
                <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                    <div className="flex items-center gap-3">
                        <h1 className="text-3xl font-bold tracking-tight">{project.name}</h1>
                        <Badge
                            className={cn(
                                getStateColor(currentState),
                                getStateGlow(currentState),
                                'border-0'
                            )}
                        >
                            {currentState}
                        </Badge>
                    </div>

                    {/* Quick action buttons */}
                    <div className="flex items-center gap-2">
                        {project.id && project.state && (
                            <StartStopProjectButton
                                projectId={project.id}
                                state={project.state}
                                variant="outline"
                                size="sm"
                            />
                        )}
                        {project.primaryDomain && (
                            <Button variant="outline" size="sm" className="gap-1.5" asChild>
                                <a href={`https://${project.primaryDomain}`} target="_blank" rel="noopener noreferrer">
                                    <Globe className="h-3.5 w-3.5" />
                                    Visit
                                </a>
                            </Button>
                        )}
                        <Button variant="ghost" size="icon" className="h-9 w-9" asChild>
                            <Link to="/projects/$projectId/settings" params={{ projectId }}>
                                <Settings className="h-4 w-4" />
                            </Link>
                        </Button>
                    </div>
                </div>

                {/* Primary domain chip */}
                {project.primaryDomain && (
                    <a
                        href={`https://${project.primaryDomain}`}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-secondary/60 text-sm text-muted-foreground hover:text-foreground hover:bg-secondary transition-colors"
                    >
                        <Globe className="h-3.5 w-3.5" />
                        {project.primaryDomain}
                        <ExternalLink className="h-3 w-3" />
                    </a>
                )}
            </motion.div>

            {/* Bento Grid */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                {/* Project Details Card */}
                <motion.div
                    initial={{ opacity: 0, y: 16 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.3, delay: 0.05 }}
                    className="rounded-xl border border-zinc-800 bg-zinc-900/50 backdrop-blur-sm p-6 hover:border-zinc-700 transition-colors"
                >
                    <h2 className="text-lg font-semibold mb-4">Project Details</h2>
                    <div className="space-y-4">
                        <div>
                            <p className="text-xs font-medium text-muted-foreground mb-1">Project ID</p>
                            <p className="text-sm font-mono text-foreground/80">{project.id}</p>
                        </div>

                        <div>
                            <p className="text-xs font-medium text-muted-foreground mb-1">Repository</p>
                            <a
                                className="inline-flex items-center gap-1.5 text-sm hover:text-foreground text-foreground/80 transition-colors"
                                href={`https://github.com/${project.repoName}`}
                                target="_blank"
                                rel="noreferrer"
                            >
                                <Github className="h-3.5 w-3.5" />
                                {project.repoName}
                                <ExternalLink className="h-3 w-3" />
                            </a>
                        </div>

                        <div>
                            <p className="text-xs font-medium text-muted-foreground mb-1">
                                Hardware - {serverTier?.id.value}
                            </p>
                            {serverTier ? (
                                <div className="flex flex-col gap-1.5">
                                    <span className="flex items-center gap-2 text-sm text-foreground/80">
                                        <MemoryStick className="h-4 w-4 text-zinc-400" />
                                        {serverTier.ramGb} GB RAM
                                    </span>
                                    <span className="flex items-center gap-2 text-sm text-foreground/80">
                                        <Cpu className="h-4 w-4 text-green-400" />
                                        {serverTier.cores} Cores vCPU
                                    </span>
                                    <span className="flex items-center gap-2 text-sm text-foreground/80">
                                        <HardDrive className="h-4 w-4 text-amber-400" />
                                        {serverTier.diskGb} GB Disk
                                    </span>
                                </div>
                            ) : (
                                <div className="flex flex-col gap-1.5">
                                    <Skeleton className="h-[14px] w-[150px]" />
                                    <Skeleton className="h-[14px] w-[150px]" />
                                    <Skeleton className="h-[14px] w-[150px]" />
                                </div>
                            )}
                        </div>

                        <div>
                            <p className="text-xs font-medium text-muted-foreground mb-1 flex items-center gap-1">
                                Template Type
                                <TooltipProvider>
                                    <Tooltip>
                                        <TooltipTrigger asChild>
                                            <HelpCircle className="h-3 w-3 text-muted-foreground cursor-help" />
                                        </TooltipTrigger>
                                        <TooltipContent className="max-w-[250px]">
                                            <p>{templateType.description}</p>
                                            <p className="text-xs text-muted-foreground mt-1">
                                                Wrong type? Run <code className="bg-muted px-1 rounded">compozerr migrate --redetect</code> to update.
                                            </p>
                                        </TooltipContent>
                                    </Tooltip>
                                </TooltipProvider>
                            </p>
                            <p className="text-sm text-foreground/80">{templateType.label}</p>
                        </div>
                    </div>
                </motion.div>

                {/* Domains Card */}
                <motion.div
                    initial={{ opacity: 0, y: 16 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.3, delay: 0.1 }}
                    className="rounded-xl border border-zinc-800 bg-zinc-900/50 backdrop-blur-sm p-6 hover:border-zinc-700 transition-colors"
                >
                    <div className="flex items-center justify-between mb-4">
                        <h2 className="text-lg font-semibold">Domains</h2>
                        <Link
                            to="/projects/$projectId/settings"
                            params={{ projectId }}
                            className="text-xs text-muted-foreground hover:text-foreground transition-colors"
                        >
                            Manage Domains
                        </Link>
                    </div>

                    <div className="space-y-2 max-h-[240px] overflow-y-auto pr-1">
                        {domains && domains.length > 0 ? (
                            domains
                                .sort((d) => d === project.primaryDomain ? -1 : 1)
                                .map((d) => (
                                    <a
                                        key={d}
                                        href={`https://${d}`}
                                        target="_blank"
                                        rel="noopener noreferrer"
                                        className="flex items-center justify-between px-3 py-2 rounded-lg bg-zinc-800/50 hover:bg-zinc-800 transition-colors group"
                                    >
                                        <div className="flex items-center gap-2 text-sm text-foreground/80 group-hover:text-foreground transition-colors">
                                            <Globe className="h-3.5 w-3.5 text-muted-foreground" />
                                            <span className="truncate">{d}</span>
                                        </div>
                                        <div className="flex items-center gap-2">
                                            {project.primaryDomain === d && (
                                                <Star className="h-3.5 w-3.5 text-amber-400 fill-amber-400" />
                                            )}
                                            <ExternalLink className="h-3 w-3 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity" />
                                        </div>
                                    </a>
                                ))
                        ) : (
                            <p className="text-sm text-muted-foreground py-4 text-center">No domains configured</p>
                        )}
                    </div>
                </motion.div>

                {/* Resource Usage Card - spans full width on lg */}
                <motion.div
                    initial={{ opacity: 0, y: 16 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.3, delay: 0.15 }}
                    className="lg:col-span-2"
                >
                    <UsageGraph projectId={project.id!} />
                </motion.div>
            </div>
        </div>
    )
}
