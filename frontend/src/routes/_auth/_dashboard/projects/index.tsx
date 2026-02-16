import { api } from '@/api-client'
import StartStopProjectButton from '@/components/project/project-startstop-button'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { Input } from '@/components/ui/input'
import { useEphemeralQueryParams } from '@/hooks/use-ephemeral-storage.hook'
import { ProjectStateFilter } from '@/lib/project-state-filter'
import { getLink } from '@/links'
import { cn } from '@/lib/utils'
import { createFileRoute, useRouter } from '@tanstack/react-router'
import { motion } from 'framer-motion'
import { MoreVertical, Plus, Search, Workflow, Filter, FolderGit2, Activity, Server, Terminal, ExternalLink } from 'lucide-react'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { z } from 'zod'

const searchParamsSchema = z.object({
    'cli-auth': z.string().optional(),
})

export const Route = createFileRoute('/_auth/_dashboard/projects/')({
    component: RouteComponent,
    loader: () => api.v1.getProjects.prefetchQuery(),
    validateSearch: searchParamsSchema,
})

type FilterValue = 'all' | 'running' | 'stopped' | 'starting'

const filterOptions: { value: FilterValue; label: string; stateFilter: ProjectStateFilter }[] = [
    { value: 'all', label: 'All', stateFilter: ProjectStateFilter.All },
    { value: 'running', label: 'Running', stateFilter: ProjectStateFilter.Running },
    { value: 'stopped', label: 'Stopped', stateFilter: ProjectStateFilter.Stopped },
    { value: 'starting', label: 'Starting', stateFilter: ProjectStateFilter.Starting },
]

function RouteComponent() {
    const [search, setSearch] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');
    const [cliAuthDialogOpen, setCliAuthDialogOpen] = useState(false);

    const { collect: collectCliAuth } = useEphemeralQueryParams<{ 'cli-auth'?: string }>(
        ['cli-auth'],
        'cli-auth-params'
    );

    useEffect(() => {
        const params = collectCliAuth();
        if (params?.['cli-auth'] === 'success') {
            setCliAuthDialogOpen(true);
        }
    }, []);

    // Update debounced value after delay
    useEffect(() => {
        const timer = setTimeout(() => setDebouncedSearch(search), 50);
        return () => clearTimeout(timer);
    }, [search]);

    const [activeFilter, setActiveFilter] = useState<FilterValue>('all');
    const [filter, setFilter] = useState<ProjectStateFilter>(ProjectStateFilter.All);
    const [hasActiveDeployments, setHasActiveDeployments] = useState(false);

    const handleFilterChange = useCallback((value: FilterValue) => {
        setActiveFilter(value);
        const option = filterOptions.find(o => o.value === value);
        setFilter(option?.stateFilter ?? ProjectStateFilter.All);
    }, []);

    const { data: allProjectData } = api.v1.getProjects.useQuery({
        query: {
            stateFlags: ProjectStateFilter.All
        }
    });

    const {
        data: projectsData,
        fetchNextPage
    } = api.v1.getProjects.useInfiniteQuery(
        {
            query: {
                search: debouncedSearch,
                stateFlags: filter
            }
        },
        {
            refetchInterval: hasActiveDeployments ? 10000 : 30000,
            getNextPageParam: (lastPage) => {
                if (!lastPage) return undefined;
                const currentPage = lastPage.page ?? 1;
                const total = lastPage.totalProjectsCount ?? 0;
                const pageSize = lastPage.pageSize ?? 20;
                const totalPages = Math.ceil(total / pageSize);
                if (currentPage < totalPages) {
                    return {
                        query: {
                            page: currentPage + 1,
                            search: debouncedSearch,
                            stateFlags: filter
                        }
                    };
                }
                return undefined;
            },
            initialPageParam: {
                query: {
                    page: 1,
                    search: debouncedSearch,
                    stateFlags: filter
                }
            }
        }
    );

    const [lastValidPages, setLastValidPages] = useState<typeof api.v1.getProjects.types.data[]>([]);
    useEffect(() => {
        if (projectsData?.pages && projectsData.pages.length > 0) {
            setLastValidPages(projectsData.pages);
        }
    }, [projectsData?.pages]);

    const allPages = projectsData?.pages?.length
        ? projectsData.pages
        : lastValidPages;

    const allProjects = useMemo(() => {
        return allPages.flatMap(page => page.projects ?? []);
    }, [allPages]);

    useEffect(() => {
        setHasActiveDeployments(allProjects.some(p => p.activeDeploymentId));
    }, [allProjects]);

    const filteredTotalProjectsCount = useMemo(() => allPages[0]?.totalProjectsCount ?? 0, [allPages]);

    const totalProjectsCount = useMemo(() => allProjectData?.totalProjectsCount ?? 0, [allProjectData]);
    const runningProjectsCount = useMemo(() => allProjectData?.runningProjectsCount ?? 0, [allProjectData]);
    const stoppedProjectsCount = useMemo(() => totalProjectsCount - runningProjectsCount, [totalProjectsCount, runningProjectsCount]);

    const router = useRouter();

    // Infinite scroll observer
    const scrollSentinelRef = useRef<HTMLDivElement>(null);
    useEffect(() => {
        const sentinel = scrollSentinelRef.current;
        if (!sentinel) return;
        const observer = new IntersectionObserver(
            (entries) => {
                if (entries[0]?.isIntersecting) {
                    fetchNextPage();
                }
            },
            { rootMargin: '200px' }
        );
        observer.observe(sentinel);
        return () => observer.disconnect();
    }, [fetchNextPage]);

    return (
        <div className="mx-auto">
            <header className="mb-6">
                <h1 className="text-3xl font-bold mb-2">Projects</h1>
                <p className="text-muted-foreground">
                    Manage and monitor your running projects
                </p>
            </header>

            {/* Summary Bar */}
            <div className="flex items-center gap-6 rounded-lg border bg-card px-5 py-3 mb-6">
                <div className="flex items-center gap-2">
                    <FolderGit2 className="h-4 w-4 text-muted-foreground" />
                    <span className="text-xl font-semibold">{totalProjectsCount}</span>
                    <span className="text-sm text-muted-foreground">Total</span>
                </div>
                <div className="h-5 w-px bg-border" />
                <div className="flex items-center gap-2">
                    <Activity className="h-4 w-4 text-green-500" />
                    <span className="text-xl font-semibold text-green-500">{runningProjectsCount}</span>
                    <span className="text-sm text-muted-foreground">Running</span>
                </div>
                <div className="h-5 w-px bg-border" />
                <div className="flex items-center gap-2">
                    <Server className="h-4 w-4 text-muted-foreground" />
                    <span className="text-xl font-semibold text-muted-foreground">{stoppedProjectsCount}</span>
                    <span className="text-sm text-muted-foreground">Stopped</span>
                </div>
            </div>

            {/* Search, Filter & Actions */}
            <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-6 gap-4">
                <div className="flex flex-col sm:flex-row items-start sm:items-center w-full md:w-auto gap-3">
                    <div className="relative w-full md:max-w-md">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                        <Input
                            placeholder="Search projects..."
                            className="pl-9 font-mono text-sm"
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                        />
                    </div>
                    <div className="flex items-center rounded-lg border bg-muted/50 p-0.5">
                        {filterOptions.map((option) => (
                            <button
                                key={option.value}
                                onClick={() => handleFilterChange(option.value)}
                                className={cn(
                                    'px-3 py-1.5 text-sm font-medium rounded-md transition-colors',
                                    activeFilter === option.value
                                        ? 'bg-background text-foreground shadow-sm'
                                        : 'text-muted-foreground hover:text-foreground'
                                )}
                            >
                                {option.label}
                            </button>
                        ))}
                    </div>
                    {filteredTotalProjectsCount !== totalProjectsCount && (
                        <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
                            <Filter className="h-3.5 w-3.5" />
                            <span>Showing {filteredTotalProjectsCount} of {totalProjectsCount}</span>
                        </div>
                    )}
                </div>
                <div className="flex gap-2">
                    <Button className="hidden md:flex" variant="outline" onClick={() => router.navigate({ to: '/n8n' })}>
                        <Workflow className="mr-2 h-4 w-4" /> Create n8n Project
                    </Button>
                    <Button className="hidden md:flex" onClick={() => open(getLink('addNewService'), '_blank')}>
                        <Plus className="mr-2 h-4 w-4" /> Add New Service
                    </Button>
                </div>
            </div>

            {/* Project Card Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {allProjects.map((project, index) => {
                    const isN8n = project.type === 'N8n';
                    const state = project.state;
                    const statusColor =
                        state === 'Running' ? 'bg-green-500' :
                        state === 'Stopped' ? 'bg-red-500' :
                        state === 'Starting' ? 'bg-yellow-500' :
                        'bg-gray-500';
                    const statusTextColor =
                        state === 'Running' ? 'text-green-500' :
                        state === 'Stopped' ? 'text-red-500' :
                        state === 'Starting' ? 'text-yellow-500' :
                        'text-muted-foreground';

                    return (
                        <motion.div
                            key={project.id}
                            initial={{ opacity: 0, y: 12 }}
                            animate={{ opacity: 1, y: 0 }}
                            transition={{ duration: 0.25, delay: index * 0.04 }}
                            onClick={() => {
                                if (!project.id) return;
                                router.navigate({ to: `/projects/${project.id}` });
                            }}
                            className="group relative rounded-lg border bg-card p-4 cursor-pointer transition-all duration-200 hover:-translate-y-0.5 hover:border-primary/30 hover:shadow-md hover:shadow-primary/5"
                        >
                            {/* Top: Name + type icon */}
                            <div className="flex items-start justify-between mb-3">
                                <div className="flex items-center gap-2.5 min-w-0">
                                    <div className={cn(
                                        'flex items-center justify-center w-8 h-8 rounded-md shrink-0',
                                        isN8n ? 'bg-primary/10' : 'bg-muted'
                                    )}>
                                        {isN8n
                                            ? <Workflow className="h-4 w-4 text-primary" />
                                            : <FolderGit2 className="h-4 w-4 text-muted-foreground" />
                                        }
                                    </div>
                                    <span className="font-semibold truncate">{project.name}</span>
                                </div>
                                <DropdownMenu>
                                    <DropdownMenuTrigger asChild>
                                        <Button
                                            variant="ghost"
                                            size="sm"
                                            className="h-7 w-7 p-0 opacity-0 group-hover:opacity-100 transition-opacity"
                                            onClick={(e) => e.stopPropagation()}
                                        >
                                            <MoreVertical className="h-4 w-4" />
                                        </Button>
                                    </DropdownMenuTrigger>
                                    <DropdownMenuContent align="end" className="pointer-events-auto">
                                        <DropdownMenuLabel>Actions</DropdownMenuLabel>
                                        <DropdownMenuItem onClick={(e) => {
                                            e.stopPropagation();
                                            router.navigate({ to: "/projects/$projectId", params: { projectId: project.id! } });
                                        }}>View Details</DropdownMenuItem>
                                        <DropdownMenuItem onClick={(e) => {
                                            e.stopPropagation();
                                            router.navigate({ to: "/projects/$projectId/settings/general", params: { projectId: project.id! } });
                                        }}>Edit Service</DropdownMenuItem>
                                        <DropdownMenuSeparator />
                                        <DropdownMenuItem className="text-red-600" onClick={(e) => {
                                            e.stopPropagation();
                                            router.navigate({
                                                to: "/projects/$projectId/settings/general", params: { projectId: project.id! }
                                            });
                                        }}>Delete Service</DropdownMenuItem>
                                    </DropdownMenuContent>
                                </DropdownMenu>
                            </div>

                            {/* Status indicator */}
                            <div className="flex items-center gap-2 mb-3">
                                <span className="relative flex h-2.5 w-2.5">
                                    {state === 'Running' && (
                                        <span className={cn('animate-ping absolute inline-flex h-full w-full rounded-full opacity-75', statusColor)} />
                                    )}
                                    <span className={cn('relative inline-flex rounded-full h-2.5 w-2.5', statusColor)} />
                                </span>
                                <span className={cn('text-sm font-medium', statusTextColor)}>{state}</span>
                                {project.activeDeploymentId && (
                                    <button
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            router.navigate({
                                                to: '/projects/$projectId/deployments/$deploymentId',
                                                params: {
                                                    projectId: project.id!,
                                                    deploymentId: project.activeDeploymentId!,
                                                },
                                            });
                                        }}
                                        className="inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full bg-blue-500/10 hover:bg-blue-500/20 transition-colors cursor-pointer ml-auto"
                                        title="View active deployment"
                                    >
                                        <span className="relative flex h-2 w-2">
                                            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-blue-400 opacity-75" />
                                            <span className="relative inline-flex rounded-full h-2 w-2 bg-blue-500" />
                                        </span>
                                        <span className="text-[11px] font-medium text-blue-600 dark:text-blue-400">
                                            Deploying
                                        </span>
                                    </button>
                                )}
                            </div>

                            {/* Middle: Domain + repo */}
                            <div className="space-y-1 mb-3">
                                {project.primaryDomain && (
                                    <a
                                        href={`https://${project.primaryDomain}`}
                                        target="_blank"
                                        rel="noopener noreferrer"
                                        onClick={(e) => e.stopPropagation()}
                                        className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors truncate"
                                    >
                                        <ExternalLink className="h-3 w-3 shrink-0" />
                                        <span className="truncate">{project.primaryDomain}</span>
                                    </a>
                                )}
                                {project.repoName && (
                                    <p className="text-xs text-muted-foreground font-mono truncate">
                                        {project.repoName}
                                    </p>
                                )}
                            </div>

                            {/* Bottom: Tier badge + start/stop */}
                            <div className="flex items-center justify-between pt-2 border-t border-border/50">
                                {project.serverTier && (
                                    <Badge variant="secondary" className="text-xs">
                                        {project.serverTier}
                                    </Badge>
                                )}
                                {!project.serverTier && <span />}
                                {state && project.id && (
                                    <div onClick={(e) => e.stopPropagation()}>
                                        <StartStopProjectButton projectId={project.id} state={state} size="sm" />
                                    </div>
                                )}
                            </div>
                        </motion.div>
                    );
                })}
            </div>

            {/* Infinite scroll sentinel */}
            <div ref={scrollSentinelRef} className="h-4" />

            {/* CLI Auth Dialog */}
            <Dialog open={cliAuthDialogOpen} onOpenChange={setCliAuthDialogOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle className="flex items-center gap-2">
                            <Terminal className="h-5 w-5" />
                            Signed in successfully
                        </DialogTitle>
                        <DialogDescription>
                            Your CLI session is now authenticated. You can return to your terminal and start using Compozerr commands.
                        </DialogDescription>
                    </DialogHeader>
                    <DialogFooter>
                        <Button onClick={() => setCliAuthDialogOpen(false)}>Got it</Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    )
}
