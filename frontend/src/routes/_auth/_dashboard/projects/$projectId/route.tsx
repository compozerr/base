import {
  createFileRoute,
  Link,
  Outlet,
  useLocation,
} from '@tanstack/react-router'
import { cn } from '@/lib/utils'
import { api } from '@/api-client'
import { Button } from '@/components/ui/button'
import { DashboardHeaderPortal } from '@/components/dashboard-header-portal'
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

  return (
    <div className="flex flex-col min-h-screen w-full">
      {/* Portal breadcrumbs + tabs into the dashboard header */}
      <DashboardHeaderPortal>
        <div className="flex items-center gap-4 flex-1 min-w-0">
          {/* Breadcrumb */}
          <nav className="flex items-center gap-1.5 text-sm shrink-0" aria-label="Breadcrumb">
            <Link
              to="/projects"
              className="text-white/50 hover:text-white transition-colors"
            >
              Projects
            </Link>
            <ChevronRight className="h-3 w-3 text-white/30" />
            <span className="font-medium text-white truncate max-w-[160px]">
              {project?.name}
            </span>
          </nav>

          {/* Separator */}
          <div className="h-4 w-px bg-white/[0.08] hidden sm:block" />

          {/* Tabs */}
          <nav className="flex items-center gap-0.5 overflow-x-auto scrollbar-none">
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

          {/* Visit button - pushed to the right */}
          {project?.primaryDomain && (
            <Button variant="outline" size="sm" className="gap-1.5 ml-auto shrink-0 hidden sm:inline-flex" asChild>
              <a href={"https://" + project.primaryDomain} target="_blank" rel="noopener noreferrer">
                <ExternalLink className="h-3.5 w-3.5" />
                Visit
              </a>
            </Button>
          )}
        </div>
      </DashboardHeaderPortal>

      {/* Page content */}
      <div className="py-6">
        <Outlet />
      </div>
    </div>
  )
}
