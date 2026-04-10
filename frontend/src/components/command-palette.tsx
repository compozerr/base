import { useCallback, useEffect, useMemo, useState } from "react"
import { useQuery } from "@tanstack/react-query"
import {
  Building2,
  FolderGit2,
  User,
} from "lucide-react"
import {
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command"
import { useActions } from "@/lib/command-palette/action-registry"
import type { Action, ActionGroup } from "@/lib/command-palette/types"
import { apiBaseUrl } from "@/api-client"
import {
  getCurrentOrgId,
  orgFetch,
} from "@/lib/org-storage"
import { switchOrg } from "@/lib/switch-org"

type NavigateFn = (opts: { to: string; params?: Record<string, string> }) => void

interface Organization {
  id: string
  name: string
  slug: string
  isPersonal: boolean
}

const GROUP_LABELS: Record<ActionGroup, string> = {
  action: "Actions",
  project: "Projects",
  organization: "Organizations",
  navigation: "Navigation",
}

const GROUP_ORDER: ActionGroup[] = ["action", "project", "organization", "navigation"]

export function CommandPalette({ navigate }: { navigate: NavigateFn }) {
  const [open, setOpen] = useState(false)
  const [search, setSearch] = useState("")
  const registeredActions = useActions()

  // Toggle on Cmd+K / Ctrl+K
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "k" && (e.metaKey || e.ctrlKey)) {
        e.preventDefault()
        setOpen((prev) => !prev)
      }
    }
    document.addEventListener("keydown", handleKeyDown)
    return () => document.removeEventListener("keydown", handleKeyDown)
  }, [])

  // Reset search when closing
  useEffect(() => {
    if (!open) setSearch("")
  }, [open])

  // Fetch organizations (always available, cached)
  const { data: organizations } = useQuery<Organization[]>({
    queryKey: ["organizations"],
    queryFn: async () => {
      const res = await orgFetch(`${apiBaseUrl}/v1/organizations`)
      if (!res.ok) return []
      return res.json()
    },
    staleTime: 60_000,
    enabled: open,
  })

  // Search projects across all orgs when user types
  const debouncedSearch = useDebounce(search, 300)
  const shouldSearchProjects = open && debouncedSearch.length > 0

  const { data: projectResults, isLoading: isSearchingProjects } = useQuery({
    queryKey: ["command-palette-projects", debouncedSearch, organizations?.map(o => o.id)],
    queryFn: async () => {
      if (!organizations || organizations.length === 0) return []
      const results = await Promise.all(
        organizations.map(async (org) => {
          const res = await fetch(`${apiBaseUrl}/v1/projects?search=${encodeURIComponent(debouncedSearch)}`, {
            credentials: "include",
            headers: { "X-Organization-Id": org.id },
          })
          if (!res.ok) return []
          const data = await res.json()
          return (data.projects ?? []).map((p: Record<string, unknown>) => ({
            ...p,
            organizationId: org.id,
            organizationName: org.isPersonal ? "Personal" : org.name,
          }))
        })
      )
      return results.flat()
    },
    enabled: shouldSearchProjects,
    staleTime: 10_000,
  })

  // Build org actions from API data
  const orgActions: Action[] = useMemo(() => {
    if (!organizations) return []
    const currentOrgId = getCurrentOrgId()
    return organizations
      .filter((org) => {
        if (!search) return true
        const searchLower = search.toLowerCase()
        const name = org.isPersonal ? "personal" : org.name.toLowerCase()
        return name.includes(searchLower) || org.slug.toLowerCase().includes(searchLower)
      })
      .map((org) => ({
        id: `org-${org.id}`,
        name: org.isPersonal ? "Personal Account" : org.name,
        subtitle:
          org.id === currentOrgId
            ? "Current"
            : `Switch to ${org.isPersonal ? "Personal" : org.name}`,
        icon: org.isPersonal ? User : Building2,
        group: "organization" as const,
        perform: () => {
          if (org.id !== currentOrgId) {
            switchOrg(org.id)
          }
        },
      }))
  }, [organizations, search])

  // Build project actions from search results
  const projectActions: Action[] = useMemo(() => {
    if (!projectResults) return []
    const currentOrgId = getCurrentOrgId()
    return projectResults.map((project: Record<string, string | null>) => ({
      id: `project-${project.id}`,
      name: project.name ?? "Unnamed",
      subtitle: project.organizationId !== currentOrgId
        ? `\u2197 ${project.organizationName}`
        : (project.organizationName ?? undefined),
      icon: FolderGit2,
      group: "project" as const,
      perform: () => {
        if (project.organizationId && project.organizationId !== currentOrgId) {
          switchOrg(project.organizationId)
        }
        navigate({
          to: "/projects/$projectId",
          params: { projectId: project.id! },
        })
      },
    }))
  }, [projectResults, navigate])

  // Merge all actions and group them
  const allActions = useMemo(() => {
    return [...registeredActions, ...projectActions, ...orgActions]
  }, [registeredActions, projectActions, orgActions])

  const groupedActions = useMemo(() => {
    const groups = new Map<ActionGroup, Action[]>()
    for (const action of allActions) {
      const list = groups.get(action.group) ?? []
      list.push(action)
      groups.set(action.group, list)
    }
    for (const [, actions] of groups) {
      actions.sort((a, b) => (a.priority ?? 50) - (b.priority ?? 50))
    }
    return groups
  }, [allActions])

  // Track whether a project search is pending (including debounce gap)
  const isSearchPending = open && search.length > 0 && (
    search !== debouncedSearch || isSearchingProjects
  )

  const handleSelect = useCallback(
    (actionId: string) => {
      const action = allActions.find((a) => a.id === actionId)
      if (action) {
        setOpen(false)
        requestAnimationFrame(() => {
          action.perform()
        })
      }
    },
    [allActions]
  )

  return (
    <CommandDialog open={open} onOpenChange={setOpen}>
      <CommandInput
        placeholder="Search or type a command..."
        value={search}
        onValueChange={setSearch}
        loading={isSearchPending}
      />
      <CommandList>
        {!isSearchPending && <CommandEmpty>No results found.</CommandEmpty>}
        {GROUP_ORDER.map((groupKey) => {
          const actions = groupedActions.get(groupKey)
          if (!actions || actions.length === 0) return null
          return (
            <CommandGroup key={groupKey} heading={GROUP_LABELS[groupKey]}>
              {actions.map((action) => (
                <CommandItem
                  key={action.id}
                  value={`${action.name} ${action.keywords?.join(" ") ?? ""}`}
                  onSelect={() => handleSelect(action.id)}
                  className="flex items-center gap-2"
                >
                  {action.icon && <action.icon className="h-4 w-4 text-muted-foreground shrink-0" />}
                  <div className="flex flex-col min-w-0">
                    <span className="truncate">{action.name}</span>
                    {action.subtitle && (
                      <span className="text-xs text-muted-foreground truncate">
                        {action.subtitle}
                      </span>
                    )}
                  </div>
                  {action.shortcut && (
                    <div className="ml-auto flex items-center gap-1">
                      {action.shortcut.map((key) => (
                        <kbd
                          key={key}
                          className="pointer-events-none inline-flex h-5 select-none items-center gap-1 rounded border bg-muted px-1.5 font-mono text-[10px] font-medium text-muted-foreground"
                        >
                          {key}
                        </kbd>
                      ))}
                    </div>
                  )}
                </CommandItem>
              ))}
            </CommandGroup>
          )
        })}
      </CommandList>
    </CommandDialog>
  )
}

function useDebounce<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState(value)
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedValue(value), delay)
    return () => clearTimeout(timer)
  }, [value, delay])
  return debouncedValue
}
