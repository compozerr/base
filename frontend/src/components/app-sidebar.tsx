import { api } from "@/api-client"
import { getCurrentOrgId } from "@/lib/org-storage"
import { switchOrg } from "@/lib/switch-org"
import { CompozerrLogo } from "@/components/compozerr-logo"
import { useAuth } from "@/hooks/use-dynamic-auth"
import { getLink } from "@/links"
import { Link, useNavigate, useRouter, useRouterState } from "@tanstack/react-router"
import { AnimatePresence, motion } from "framer-motion"
import { Building2, Check, ChevronDown, ChevronsUpDown, FileText, FolderGit2, Home, LogOut, Package, Settings, User, Users } from "lucide-react"
import { useEffect, useState } from "react"
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
  const [orgSwitcherOpen, setOrgSwitcherOpen] = useState(false)

  const { logout, user } = useAuth()

  const { data: organizations } = api.v1.getOrganizations.useQuery(undefined, { staleTime: 60_000 })

  const [currentOrgId, setCurrentOrgId] = useState<string | null>(() =>
    getCurrentOrgId()
  )

  // Sync org state when changed externally (e.g. command palette)
  useEffect(() => {
    const handler = (e: Event) => {
      const orgId = (e as CustomEvent<string>).detail
      setCurrentOrgId(orgId)
    }
    window.addEventListener("org-changed", handler)
    return () => window.removeEventListener("org-changed", handler)
  }, [])

  // Auto-select personal org if none selected
  useEffect(() => {
    if (!organizations || organizations.length === 0) return
    if (currentOrgId && organizations.find((o) => o.id === currentOrgId)) return
    const personal = organizations.find((o) => o.isPersonal) ?? organizations[0]
    if (personal) {
      switchOrg(personal.id)
    }
  }, [organizations, currentOrgId])

  const currentOrg = organizations?.find((o) => o.id === currentOrgId)

  const handleSwitchOrg = (org: NonNullable<typeof organizations>[number]) => {
    switchOrg(org.id)
    setOrgSwitcherOpen(false)
  }

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

  const projectMatch = currentPath.match(/^\/projects\/([^/]+)/)
  const currentProjectId = projectMatch?.[1]

  return (
    <Sidebar className="border-r border-white/[0.06] bg-black/80 backdrop-blur-xl">
      {/* Brand */}
      <SidebarHeader className="border-b border-white/[0.06] px-4 py-4">
        <Link to="/" className="mb-3 block">
          <CompozerrLogo className="text-lg text-white hover:text-white/80 transition-colors" />
        </Link>

        {/* Org Switcher */}
        <div className="relative">
          <button
            onClick={() => setOrgSwitcherOpen((v) => !v)}
            className="flex w-full items-center gap-2 rounded-md border border-white/[0.08] bg-white/[0.03] px-3 py-2 text-sm transition-colors hover:bg-white/[0.06]"
          >
            {currentOrg?.isPersonal ? (
              user?.avatarUrl ? (
                <img src={user.avatarUrl} alt="" className="h-5 w-5 rounded-full" />
              ) : (
                <User className="h-4 w-4 text-white/50" />
              )
            ) : (
              <Building2 className="h-4 w-4 text-white/50" />
            )}
            <span className="flex-1 truncate text-left text-white/80">
              {currentOrg?.isPersonal ? "Personal Account" : currentOrg?.name ?? "Select org..."}
            </span>
            <ChevronsUpDown className="h-3.5 w-3.5 shrink-0 text-white/30" />
          </button>

          <AnimatePresence>
            {orgSwitcherOpen && (
              <>
                <div className="fixed inset-0 z-40" onClick={() => setOrgSwitcherOpen(false)} />
                <motion.div
                  initial={{ opacity: 0, y: -4 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -4 }}
                  transition={{ duration: 0.12 }}
                  className="absolute left-0 right-0 top-full z-50 mt-1 rounded-md border border-white/[0.08] bg-zinc-900 p-1 shadow-xl"
                >
                  {organizations?.map((org) => (
                    <button
                      key={org.id}
                      onClick={() => handleSwitchOrg(org)}
                      className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-white/70 transition-colors hover:bg-white/[0.06] hover:text-white"
                    >
                      {org.isPersonal ? (
                        <User className="h-3.5 w-3.5 shrink-0 text-white/40" />
                      ) : (
                        <Building2 className="h-3.5 w-3.5 shrink-0 text-white/40" />
                      )}
                      <span className="flex-1 truncate text-left">
                        {org.isPersonal ? "Personal Account" : org.name}
                      </span>
                      {org.id === currentOrgId && (
                        <Check className="h-3.5 w-3.5 shrink-0 text-white/60" />
                      )}
                    </button>
                  ))}
                  <div className="my-1 border-t border-white/[0.06]" />
                  <button
                    onClick={() => {
                      setOrgSwitcherOpen(false)
                      navigate({ to: "/organizations" })
                    }}
                    className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-sm text-white/50 transition-colors hover:bg-white/[0.06] hover:text-white"
                  >
                    <Settings className="h-3.5 w-3.5" />
                    <span>Manage Organizations</span>
                  </button>
                </motion.div>
              </>
            )}
          </AnimatePresence>
        </div>
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
                    navigate({ viewTransition: true, to: "/members" })
                  }
                  isActive={isActive("/members")}
                  tooltip="Members"
                  className={
                    isActive("/members")
                      ? "relative text-white before:absolute before:left-0 before:top-1/2 before:h-5 before:-translate-y-1/2 before:w-[2px] before:rounded-r before:bg-[hsl(var(--nav-accent))]"
                      : "text-white/60 hover:text-white"
                  }
                >
                  <Users className="h-4 w-4" />
                  <span>Members</span>
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
