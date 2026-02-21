import {
  createFileRoute,
  Link,
  Outlet,
  useLocation,
} from '@tanstack/react-router'
import { cn } from '@/lib/utils'
import { api } from '@/api-client'
import { Button } from '@/components/ui/button'
import { DashboardHeaderBreadcrumb, DashboardHeaderTabs } from '@/components/dashboard-header-portal'
import { ChevronRight, ExternalLink } from 'lucide-react'

export const Route = createFileRoute('/_auth/_dashboard/projects/$projectId')({
  component: RouteComponent,
  loader: async ({ params: { projectId } }) => {
    await api.v1.getProjectsProjectId.prefetchQuery({
      parameters: { path: { projectId } },
    });

    return { projectId };
  }
})

function RouteComponent() {
  const { pathname } = useLocation()
  const { projectId } = Route.useLoaderData()

  const { data: project } = api.v1.getProjectsProjectId.useQuery({ path: { projectId } })

  const isTabActive = (href: string) => {
    if (href === `/projects/${projectId}`) {
      return pathname === href
    }
    return pathname.startsWith(href)
  }

  const tabs = [
    { name: 'Overview', href: `/projects/${projectId}` },
    { name: 'Deployments', href: `/projects/${projectId}/deployments` },
    { name: 'Settings', href: `/projects/${projectId}/settings` },
  ]

  const tabNav = (
    <nav className="flex items-center gap-0.5 overflow-x-auto scrollbar-none pb-2 pt-0.5">
      {tabs.map((tab) => {
        const active = isTabActive(tab.href)
        return (
          <Link
            key={tab.name}
            to={tab.href}
            className={cn(
              'px-2.5 py-1 text-[13px] font-medium rounded-md transition-colors whitespace-nowrap',
              active
                ? 'bg-white/[0.08] text-white'
                : 'text-white/50 hover:text-white hover:bg-white/[0.04]',
            )}
          >
            {tab.name}
          </Link>
        )
      })}
    </nav>
  )

  return (
    <div className="flex flex-col min-h-screen w-full">
      {/* Breadcrumb row: inline with sidebar trigger */}
      <DashboardHeaderBreadcrumb>
        <nav className="flex items-center gap-1.5 text-sm min-w-0 flex-1" aria-label="Breadcrumb">
          <Link
            to="/projects"
            className="text-white/50 hover:text-white transition-colors shrink-0"
          >
            Projects
          </Link>
          <ChevronRight className="h-3 w-3 text-white/30 shrink-0" />
          <span className="font-medium text-white truncate">
            {project?.name}
          </span>
        </nav>

        {project?.primaryDomain && (
          <Button variant="outline" size="sm" className="gap-1.5 shrink-0 ml-auto hidden sm:inline-flex h-7 text-xs" asChild>
            <a href={"https://" + project.primaryDomain} target="_blank" rel="noopener noreferrer">
              <ExternalLink className="h-3 w-3" />
              Visit
            </a>
          </Button>
        )}
      </DashboardHeaderBreadcrumb>

      {/* Tabs row: desktop (portal into header) */}
      <DashboardHeaderTabs>
        {tabNav}
      </DashboardHeaderTabs>

      {/* Tabs row: mobile (inline, visible only on small screens) */}
      <div className="md:hidden border-b border-white/[0.08] px-1 -mx-4 mb-2">
        {tabNav}
      </div>

      {/* Page content */}
      <div className="py-6">
        <Outlet />
      </div>
    </div>
  )
}
