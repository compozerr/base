import { api } from '@/api-client'
import { DataTable } from '@/components/data-table'
import StartStopProjectButton from '@/components/project/project-startstop-button'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { Input } from '@/components/ui/input'
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select'
import { Formatter } from '@/lib/formatter'
import { ProjectStateFilter } from '@/lib/project-state-filter'
import { getLink } from '@/links'
import { createFileRoute, useRouter } from '@tanstack/react-router'
import { MoreVertical, Plus, Search } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'


export const Route = createFileRoute('/_auth/_dashboard/projects/')({
    component: RouteComponent,
    loader: () => api.v1.getProjects.prefetchQuery(),
})

function RouteComponent() {
    const [search, setSearch] = useState('');
    const [debouncedSearch, setDebouncedSearch] = useState('');

    // Update debounced value after delay
    useEffect(() => {
        const timer = setTimeout(() => setDebouncedSearch(search), 50);
        return () => clearTimeout(timer);
    }, [search]);

    const [filter, setFilter] = useState<ProjectStateFilter>(ProjectStateFilter.All);

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
            getNextPageParam: (lastPage) => {
                // If the current page *is* full, there may be more
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

    const totalProjectsCount = useMemo(() => allPages[0]?.totalProjectsCount ?? 0, [allPages]);
    const runningProjectsCount = useMemo(() => allPages[0]?.runningProjectsCount ?? 0, [allPages]);

    const router = useRouter();

    return (
        <div className="mx-auto">
            <header className="mb-8">
                <h1 className="text-3xl font-bold mb-2">Projects</h1>
                <p className="text-muted-foreground">
                    Manage and monitor your running projects
                </p>
            </header>

            <div className="grid gap-6 md:grid-cols-3 mb-8">
                <DashboardCard title="Total Projects" value={totalProjectsCount.toString()} />
                <DashboardCard title="Running Projects" value={runningProjectsCount.toString()} />
            </div>

            <div className="flex flex-col md:flex-row justify-between items-center mb-6 gap-4">
                <div className="flex items-center w-full md:w-auto">
                    <div className="relative max-w-sm mr-2">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                        <Input placeholder="Search projects..." className="pl-9" value={search} onChange={(e) => setSearch(e.target.value)} />
                    </div>
                    <Select defaultValue="all" onValueChange={(value) => {
                        switch (value) {
                            case 'all':
                                setFilter(ProjectStateFilter.All);
                                break;
                            case 'running':
                                setFilter(ProjectStateFilter.Running);
                                break;
                            case 'stopped':
                                setFilter(ProjectStateFilter.Stopped);
                                break;
                            case 'starting':
                                setFilter(ProjectStateFilter.Starting);
                                break;
                            default:
                                setFilter(ProjectStateFilter.All);
                                break;
                        }
                    }}>
                        <SelectTrigger className="w-[180px]">
                            <SelectValue placeholder="Filter by status" />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value="all">All Projects</SelectItem>
                            <SelectItem value="running">Running</SelectItem>
                            <SelectItem value="stopped">Stopped</SelectItem>
                            <SelectItem value="starting">Starting</SelectItem>
                        </SelectContent>
                    </Select>
                </div>
                <Button onClick={() => open(getLink('addNewService'), '_blank')}>
                    <Plus className="mr-2 h-4 w-4" /> Add New Service
                </Button>
            </div>

            <DataTable onRowClick={(row) => {
                if (!row.id) return;
                router.navigate({ to: `/projects/${row.id}` });
            }} isLoading={false} data={allProjects} fetchNextPage={fetchNextPage} columns={[
                {
                    accessorKey: 'name',
                    header: 'Project',
                    cell: ({ row }) => {
                        return <div className="flex items-center gap-2">

                            <div>
                                <div className="font-medium">{row.original.name}</div>
                                <div className="text-xs text-muted-foreground">{row.original.repoName}</div>
                            </div>
                        </div>
                    }
                },
                {
                    accessorKey: "serverTier",
                    header: "Server Tier"
                },
                {
                    accessorKey: "state",
                    header: "State",
                    cell: ({ row }) => {
                        const state = row.original.state;

                        return (<span
                            className={`inline-flex items-center rounded-full px-2 py-1 text-xs ${state === "Running"
                                ? "bg-green-500/10 text-green-500"
                                : state === "Stopped"
                                    ? "bg-red-500/10 text-red-500"
                                    : "bg-yellow-500/10 text-yellow-500"
                                }`}
                        >
                            {state}
                        </span>)
                    }
                },
                {
                    accessorKey: "",
                    header: "Actions",
                    cell: ({ row }) => {
                        const state = row.original.state;
                        const projectId = row.original.id;

                        return (
                            <div className="flex gap-2">
                                {state && projectId && <StartStopProjectButton projectId={projectId} state={state} />}
                                <DropdownMenu>
                                    <DropdownMenuTrigger asChild>
                                        <Button variant="ghost" size="sm">
                                            <MoreVertical className="h-4 w-4" />
                                        </Button>
                                    </DropdownMenuTrigger>
                                    <DropdownMenuContent align="end" className='pointer-events-auto'>
                                        <DropdownMenuLabel>Actions</DropdownMenuLabel>
                                        <DropdownMenuItem onClick={(e) => {
                                            e.stopPropagation();
                                            router.navigate({ to: "/projects/$projectId", params: { projectId: row.original.id! } });
                                        }}>View Details</DropdownMenuItem>
                                        <DropdownMenuItem onClick={(e) => {
                                            e.stopPropagation();
                                            router.navigate({ to: "/projects/$projectId/settings/general", params: { projectId: row.original.id! } });
                                        }}>Edit Service</DropdownMenuItem>
                                        <DropdownMenuSeparator />
                                        <DropdownMenuItem className="text-red-600" onClick={(e) => {
                                            e.stopPropagation();
                                            router.navigate({
                                                to: "/projects/$projectId/settings/general", params: { projectId: row.original.id! }
                                            });
                                        }}>Delete Service</DropdownMenuItem>
                                    </DropdownMenuContent>
                                </DropdownMenu>
                            </div>
                        )
                    }
                }
            ]} />
        </div>
    )
}

function DashboardCard({ title, value }: { title: string; value: string }) {
    return (
        <Card>
            <CardHeader>
                <CardTitle>

                    {title}
                </CardTitle>
            </CardHeader>
            <CardContent className='text-3xl'>
                {value}
            </CardContent>
        </Card>
    )
}
