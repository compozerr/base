import { api } from "@/api-client"
import { CompozerrLogo } from "@/components/compozerr-logo"
import { useAuth } from "@/hooks/use-dynamic-auth"
import { getLink } from "@/links"
import { Link, useNavigate, useRouter, useRouterState } from "@tanstack/react-router"
import { AnimatePresence, motion } from "framer-motion"
import { ChevronDown, FileText, FolderGit2, Home, LogOut, Package, Settings, User } from "lucide-react"
import { useState } from "react"
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarSeparator,
} from "./ui/sidebar"

export function AppSidebar() {
  const navigate = useNavigate()
  const router = useRouter()
  const routerState = useRouterState()
  const currentPath = routerState.location.pathname
  const [projectSwitcherOpen, setProjectSwitcherOpen] = useState(false)

  const { logout, user } = useAuth()

  const { data: projectsData } = api.v1.getProjects.useQuery({
    query: { stateFlags: 0 },
  })

  const handleLogout = () => {
    if (window.confirm("Are you sure you want to logout?")) {
      logout().then(() => {
        router.invalidate().finally(() => {
          navigate({ to: "/" })
        })
      })
    }
  }

  const isActive = (path: string) => currentPath.startsWith(path)

  // Extract current project ID from path
  const projectMatch = currentPath.match(/^\/projects\/([^/]+)/)
  const currentProjectId = projectMatch?.[1]
  const currentProject = projectsData?.projects?.find(
    (p) => p.id === currentProjectId
  )

  return (
    <Sidebar className="border-r border-white/[0.06] bg-black/80 backdrop-blur-xl">
      {/* Brand */}
      <SidebarHeader className="border-b border-white/[0.06] px-4 py-5">
        <Link to="/">
          <CompozerrLogo className="text-lg text-white hover:text-white/80 transition-colors" />
        </Link>
      </SidebarHeader>

      <SidebarContent>
        {/* Main Navigation */}
        <SidebarGroup>
          <SidebarGroupLabel className="px-4 text-[11px] font-medium uppercase tracking-wider text-white/40">
            Navigation
          </SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuButton
                  onClick={() =>
                    navigate({ viewTransition: true, to: "/projects" })
                  }
                  isActive={isActive("/projects")}
                  tooltip="Projects"
                  className={
                    isActive("/projects")
                      ? "relative text-white before:absolute before:left-0 before:top-1/2 before:h-5 before:-translate-y-1/2 before:w-[2px] before:rounded-r before:bg-[hsl(var(--nav-accent))]"
                      : "text-white/60 hover:text-white"
                  }
                >
                  <Home className="h-4 w-4" />
                  <span>Projects</span>
                </SidebarMenuButton>
              </SidebarMenuItem>
              <SidebarMenuItem>
                <SidebarMenuButton
                  onClick={() =>
                    navigate({ viewTransition: true, to: "/my-modules" })
                  }
                  isActive={isActive("/my-modules")}
                  tooltip="Modules"
                  className={
                    isActive("/my-modules")
                      ? "relative text-white before:absolute before:left-0 before:top-1/2 before:h-5 before:-translate-y-1/2 before:w-[2px] before:rounded-r before:bg-[hsl(var(--nav-accent))]"
                      : "text-white/60 hover:text-white"
                  }
                >
                  <Package className="h-4 w-4" />
                  <span>Modules</span>
                </SidebarMenuButton>
              </SidebarMenuItem>
              <SidebarMenuItem>
                <SidebarMenuButton
                  onClick={() =>
                    navigate({ viewTransition: true, to: "/settings" })
                  }
                  isActive={isActive("/settings")}
                  tooltip="Settings"
                  className={
                    isActive("/settings")
                      ? "relative text-white before:absolute before:left-0 before:top-1/2 before:h-5 before:-translate-y-1/2 before:w-[2px] before:rounded-r before:bg-[hsl(var(--nav-accent))]"
                      : "text-white/60 hover:text-white"
                  }
                >
                  <Settings className="h-4 w-4" />
                  <span>Settings</span>
                </SidebarMenuButton>
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        {/* Project Quick-Switch */}
        {projectsData?.projects && projectsData.projects.length > 0 && (
          <>
            <SidebarSeparator className="bg-white/[0.06]" />
            <SidebarGroup>
              <SidebarGroupContent>
                <button
                  onClick={() => setProjectSwitcherOpen((v) => !v)}
                  className="flex w-full items-center justify-between rounded-md px-4 py-2 text-[11px] font-medium uppercase tracking-wider text-white/40 hover:text-white/60 transition-colors"
                >
                  <span>Projects</span>
                  <motion.div
                    animate={{ rotate: projectSwitcherOpen ? 180 : 0 }}
                    transition={{ duration: 0.15 }}
                  >
                    <ChevronDown className="h-3 w-3" />
                  </motion.div>
                </button>
                <AnimatePresence initial={false}>
                  {projectSwitcherOpen && (
                    <motion.div
                      initial={{ height: 0, opacity: 0 }}
                      animate={{ height: "auto", opacity: 1 }}
                      exit={{ height: 0, opacity: 0 }}
                      transition={{ duration: 0.15 }}
                      className="overflow-hidden"
                    >
                      <SidebarMenu>
                        {projectsData.projects.slice(0, 8).map((project) => (
                          <SidebarMenuItem key={project.id}>
                            <SidebarMenuButton
                              onClick={() =>
                                navigate({
                                  viewTransition: true,
                                  to: "/projects/$projectId",
                                  params: { projectId: project.id },
                                })
                              }
                              isActive={currentProjectId === project.id}
                              tooltip={project.name ?? ""}
                              className={
                                currentProjectId === project.id
                                  ? "relative text-white before:absolute before:left-0 before:top-1/2 before:h-5 before:-translate-y-1/2 before:w-[2px] before:rounded-r before:bg-[hsl(var(--nav-accent))]"
                                  : "text-white/60 hover:text-white"
                              }
                            >
                              <FolderGit2 className="h-3.5 w-3.5" />
                              <span className="truncate text-xs">
                                {project.name}
                              </span>
                            </SidebarMenuButton>
                          </SidebarMenuItem>
                        ))}
                      </SidebarMenu>
                    </motion.div>
                  )}
                </AnimatePresence>
              </SidebarGroupContent>
            </SidebarGroup>
          </>
        )}

        <SidebarSeparator className="bg-white/[0.06]" />

        {/* Resources */}
        <SidebarGroup>
          <SidebarGroupLabel className="px-4 text-[11px] font-medium uppercase tracking-wider text-white/40">
            Resources
          </SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuButton
                  onClick={() =>
                    window.open(getLink("documentation"), "_blank")
                  }
                  tooltip="Documentation"
                  className="text-white/60 hover:text-white"
                >
                  <FileText className="h-4 w-4" />
                  <span>Documentation</span>
                </SidebarMenuButton>
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>

      {/* Account section at bottom */}
      <SidebarFooter className="border-t border-white/[0.06]">
        <SidebarMenu>
          <SidebarMenuItem>
            <div className="flex items-center gap-3 px-2 py-2">
              {user?.avatarUrl ? (
                <img
                  src={user.avatarUrl}
                  alt={`${user.name}-avatar`}
                  className="h-7 w-7 rounded-full ring-1 ring-white/10"
                />
              ) : (
                <div className="flex h-7 w-7 items-center justify-center rounded-full bg-white/10">
                  <User className="h-3.5 w-3.5 text-white/70" />
                </div>
              )}
              <span className="truncate text-sm font-medium text-white/80">
                {user?.name}
              </span>
            </div>
          </SidebarMenuItem>
          <SidebarMenuItem>
            <SidebarMenuButton
              onClick={handleLogout}
              tooltip="Logout"
              className="text-white/60 hover:text-red-400"
            >
              <LogOut className="h-4 w-4" />
              <span>Logout</span>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarFooter>
    </Sidebar>
  )
}
