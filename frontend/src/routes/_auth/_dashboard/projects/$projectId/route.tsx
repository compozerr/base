import * as React from 'react'
import {
  createFileRoute,
  Link,
  Outlet,
  useLocation,
} from '@tanstack/react-router'
import { cn } from '@/lib/utils'
import { api } from '@/api-client'
import { Button } from '@/components/ui/button'
import { ChevronRight, ExternalLink } from 'lucide-react'
import { AnimatePresence, motion } from 'framer-motion'

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
      {/* Navigation bar */}
      <div className="border-b border-zinc-800 px-1 py-3">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          {/* Breadcrumb */}
          <div className="flex items-center gap-1.5 text-sm">
            <Link
              to="/projects"
              className="text-muted-foreground hover:text-foreground transition-colors"
            >
              Projects
            </Link>
            <ChevronRight className="h-3.5 w-3.5 text-muted-foreground" />
            <span className="font-medium text-foreground truncate max-w-[200px]">
              {project?.name}
            </span>
          </div>

          {/* Visit button */}
          {project?.primaryDomain && (
            <Button variant="outline" size="sm" className="gap-1.5 w-fit" asChild>
              <a href={"https://" + project.primaryDomain} target="_blank" rel="noopener noreferrer">
                <ExternalLink className="h-3.5 w-3.5" />
                Visit
              </a>
            </Button>
          )}
        </div>

        {/* Pill tabs */}
        <nav className="mt-3 flex items-center gap-1 overflow-x-auto scrollbar-none -mb-px">
          {tabs.map((tab) => {
            const active = isTabActive(tab.href)
            return (
              <Link
                key={tab.name}
                to={tab.href}
                className={cn(
                  'relative px-3.5 py-1.5 text-sm font-medium rounded-md transition-colors whitespace-nowrap',
                  active
                    ? 'bg-secondary text-foreground'
                    : 'text-muted-foreground hover:text-foreground hover:bg-secondary/50',
                )}
              >
                {tab.name}
              </Link>
            )
          })}
        </nav>
      </div>

      {/* Page content */}
      <div className="py-6">
        <AnimatePresence mode="wait">
          <motion.div
            key={pathname}
            initial={{ opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -8 }}
            transition={{ duration: 0.2, ease: 'easeInOut' }}
          >
            <Outlet />
          </motion.div>
        </AnimatePresence>
      </div>
    </div>
  )
}
