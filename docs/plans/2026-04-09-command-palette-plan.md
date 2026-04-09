# Command Palette (⌘K) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a global ⌘K command palette with fuzzy search across entities and contextual page actions.

**Architecture:** Four decoupled layers — types, action registry (React context + `useRegisterActions` hook), command palette UI (shadcn Command/cmdk), and per-route action registrations. Provider wraps inside `QueryClientProvider` but above `RouterProvider`.

**Tech Stack:** cmdk (via shadcn Command component), React context, TanStack Router, OpenAPI Qraft API client.

**Worktree:** All work happens in `/Users/milo/milodev/gits/web/.worktrees/feature-command-palette`

**Design spec:** `docs/plans/2026-04-09-command-palette-design.md`

---

### Task 1: Install shadcn Command component and create types

**Files:**
- Create: `frontend/src/components/ui/command.tsx` (via shadcn CLI)
- Create: `frontend/src/lib/command-palette/types.ts`

- [ ] **Step 1: Install shadcn Command component**

Run from the worktree:
```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette/frontend
npx shadcn@latest add command
```

This installs `cmdk` as a dependency and creates `src/components/ui/command.tsx`.

- [ ] **Step 2: Create types file**

Create `frontend/src/lib/command-palette/types.ts`:

```typescript
import type { LucideIcon } from "lucide-react"

export type ActionGroup = "action" | "navigation" | "project" | "organization"

export interface Action {
  id: string
  name: string
  icon?: LucideIcon
  group: ActionGroup
  keywords?: string[]
  shortcut?: string[]
  perform: () => void | Promise<void>
  enabled?: boolean | (() => boolean)
  priority?: number
  subtitle?: string
}
```

- [ ] **Step 3: Verify command component was created**

Run:
```bash
ls -la /Users/milo/milodev/gits/web/.worktrees/feature-command-palette/frontend/src/components/ui/command.tsx
```
Expected: File exists.

- [ ] **Step 4: Commit**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette
git add frontend/src/components/ui/command.tsx frontend/src/lib/command-palette/types.ts frontend/package.json frontend/package-lock.json
git commit -m "feat: add shadcn Command component and command palette types"
```

---

### Task 2: Action Registry — context and `useRegisterActions` hook

**Files:**
- Create: `frontend/src/lib/command-palette/action-registry.tsx`

- [ ] **Step 1: Create action registry**

Create `frontend/src/lib/command-palette/action-registry.tsx`:

```tsx
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useSyncExternalStore,
  type DependencyList,
  type ReactNode,
} from "react"
import type { Action } from "./types"

type Listener = () => void

class ActionStore {
  private actions = new Map<string, Action>()
  private listeners = new Set<Listener>()

  subscribe = (listener: Listener) => {
    this.listeners.add(listener)
    return () => this.listeners.delete(listener)
  }

  getSnapshot = (): ReadonlyMap<string, Action> => {
    return this.actions
  }

  register(actions: Action[]): () => void {
    for (const action of actions) {
      this.actions.set(action.id, action)
    }
    this.notify()
    return () => {
      for (const action of actions) {
        this.actions.delete(action.id)
      }
      this.notify()
    }
  }

  private notify() {
    // Create a new Map reference so useSyncExternalStore detects the change
    this.actions = new Map(this.actions)
    for (const listener of this.listeners) {
      listener()
    }
  }
}

const ActionRegistryContext = createContext<ActionStore | null>(null)

export function ActionRegistryProvider({ children }: { children: ReactNode }) {
  const storeRef = useRef<ActionStore>(null)
  if (!storeRef.current) {
    storeRef.current = new ActionStore()
  }

  return (
    <ActionRegistryContext.Provider value={storeRef.current}>
      {children}
    </ActionRegistryContext.Provider>
  )
}

function useActionStore(): ActionStore {
  const store = useContext(ActionRegistryContext)
  if (!store) {
    throw new Error("useRegisterActions must be used within ActionRegistryProvider")
  }
  return store
}

export function useRegisterActions(actions: Action[], deps: DependencyList = []) {
  const store = useActionStore()

  useEffect(() => {
    return store.register(actions)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps)
}

export function useActions(): Action[] {
  const store = useActionStore()
  const actionsMap = useSyncExternalStore(store.subscribe, store.getSnapshot)

  return Array.from(actionsMap.values()).filter((action) => {
    if (action.enabled === undefined) return true
    if (typeof action.enabled === "function") return action.enabled()
    return action.enabled
  })
}
```

- [ ] **Step 2: Commit**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette
git add frontend/src/lib/command-palette/action-registry.tsx
git commit -m "feat: add action registry context and useRegisterActions hook"
```

---

### Task 3: Command Palette UI component

**Files:**
- Create: `frontend/src/components/command-palette.tsx`

- [ ] **Step 1: Create the command palette component**

Create `frontend/src/components/command-palette.tsx`:

```tsx
import { useCallback, useEffect, useMemo, useState } from "react"
import { useNavigate } from "@tanstack/react-router"
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
  CommandLoading,
} from "@/components/ui/command"
import { useActions } from "@/lib/command-palette/action-registry"
import type { Action, ActionGroup } from "@/lib/command-palette/types"
import { api, apiBaseUrl } from "@/api-client"
import {
  getCurrentOrgId,
  setCurrentOrgId,
  orgFetch,
} from "@/lib/org-storage"

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

export function CommandPalette() {
  const [open, setOpen] = useState(false)
  const [search, setSearch] = useState("")
  const navigate = useNavigate()
  const registeredActions = useActions()

  // Toggle on ⌘K / Ctrl+K
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
            setCurrentOrgId(org.id)
            api.v1.getProjects.invalidateQueries()
          }
          navigate({ to: "/organizations/$orgId", params: { orgId: org.id } })
        },
      }))
  }, [organizations, search, navigate])

  // Build project actions from search results
  const projectActions: Action[] = useMemo(() => {
    if (!projectResults) return []
    const currentOrgId = getCurrentOrgId()
    return projectResults.map((project: Record<string, string | null>) => ({
      id: `project-${project.id}`,
      name: project.name ?? "Unnamed",
      subtitle: project.organizationId !== currentOrgId
        ? `↗ ${project.organizationName}`
        : (project.organizationName ?? undefined),
      icon: FolderGit2,
      group: "project" as const,
      perform: () => {
        if (project.organizationId && project.organizationId !== currentOrgId) {
          setCurrentOrgId(project.organizationId)
          api.v1.getProjects.invalidateQueries()
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
    // Sort within groups by priority
    for (const [, actions] of groups) {
      actions.sort((a, b) => (a.priority ?? 50) - (b.priority ?? 50))
    }
    return groups
  }, [allActions])

  const handleSelect = useCallback(
    (actionId: string) => {
      const action = allActions.find((a) => a.id === actionId)
      if (action) {
        setOpen(false)
        // Small delay so the dialog closes before performing the action
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
      />
      <CommandList>
        <CommandEmpty>No results found.</CommandEmpty>
        {isSearchingProjects && <CommandLoading>Searching projects...</CommandLoading>}
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

// Simple debounce hook
function useDebounce<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState(value)
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedValue(value), delay)
    return () => clearTimeout(timer)
  }, [value, delay])
  return debouncedValue
}
```

- [ ] **Step 2: Commit**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette
git add frontend/src/components/command-palette.tsx
git commit -m "feat: add command palette UI component with fuzzy search and cross-org results"
```

---

### Task 4: Provider integration and global actions

**Files:**
- Create: `frontend/src/lib/command-palette/global-actions.ts`
- Modify: `frontend/src/main.tsx`

- [ ] **Step 1: Create global actions hook**

Create `frontend/src/lib/command-palette/global-actions.ts`:

```typescript
import { useNavigate } from "@tanstack/react-router"
import { Home, Package, Settings, Building2, Plus, Key } from "lucide-react"
import { useRegisterActions } from "./action-registry"
import type { Action } from "./types"

export function useGlobalActions() {
  const navigate = useNavigate()

  const actions: Action[] = [
    {
      id: "nav-projects",
      name: "Go to Projects",
      icon: Home,
      group: "navigation",
      keywords: ["home", "dashboard", "services"],
      perform: () => navigate({ to: "/projects" }),
    },
    {
      id: "nav-settings",
      name: "Go to Settings",
      icon: Settings,
      group: "navigation",
      keywords: ["account", "preferences", "tokens", "billing"],
      perform: () => navigate({ to: "/settings" }),
    },
    {
      id: "nav-organizations",
      name: "Go to Organizations",
      icon: Building2,
      group: "navigation",
      keywords: ["orgs", "teams", "members"],
      perform: () => navigate({ to: "/organizations" }),
    },
    {
      id: "nav-modules",
      name: "Go to Modules",
      icon: Package,
      group: "navigation",
      keywords: ["marketplace", "plugins", "extensions"],
      perform: () => navigate({ to: "/my-modules" }),
    },
    {
      id: "action-create-api-token",
      name: "Create API Token",
      icon: Key,
      group: "action",
      keywords: ["token", "api", "key", "auth"],
      priority: 80,
      perform: () => navigate({ to: "/settings" }),
    },
  ]

  useRegisterActions(actions, [navigate])
}
```

- [ ] **Step 2: Modify main.tsx to add provider and global actions**

In `frontend/src/main.tsx`, add the `ActionRegistryProvider` and `CommandPalette`. The provider wraps inside `ModalProvider` and around the router + a new `GlobalActionsRegistrar` component:

Replace the `InnerApp` component and the provider tree inside the return statement. The new `main.tsx` should look like:

```tsx
import ReactDOM from 'react-dom/client'
import { RouterProvider, createRouter } from '@tanstack/react-router'
import { routeTree } from './routeTree.gen'
import { useDynamicAuth } from './hooks/use-dynamic-auth'
import "./index.css";
import { QueryClientProvider } from '@tanstack/react-query';
import { queryClient } from './api-client';
import { Toaster } from './components/ui/toaster';
import { ModalProvider } from './hooks/use-modal';
import { LoadingAnimation } from './components/loading-animation';
import { HelmetProvider } from 'react-helmet-async'
import { ActionRegistryProvider } from './lib/command-palette/action-registry'
import { CommandPalette } from './components/command-palette'
import { useGlobalActions } from './lib/command-palette/global-actions'

const router = createRouter({
  routeTree,
  defaultPreload: 'intent',
  scrollRestoration: false,
  defaultPendingComponent: LoadingAnimation,
  context: {
    auth: undefined!
  }
})

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}

function GlobalActionsRegistrar({ children }: { children: React.ReactNode }) {
  useGlobalActions()
  return <>{children}</>
}

const App = () => {
  const { authComponents, isLoading, error } = useDynamicAuth();

  if (isLoading) {
    return null;
  }

  if (error) {
    return <div>Error loading auth: {error.message}</div>;
  }

  if (!authComponents) {
    return <div>Failed to load auth components</div>;
  }

  const { AuthProvider } = authComponents;

  const InnerApp = () => {
    const auth = authComponents.useDynamicAuth();
    return <RouterProvider router={router} context={{ auth }} />;
  };

  return (
    <AuthProvider>
      <HelmetProvider>
        <QueryClientProvider client={queryClient}>
          <ModalProvider>
            <ActionRegistryProvider>
              <GlobalActionsRegistrar>
                <InnerApp />
                <CommandPalette />
              </GlobalActionsRegistrar>
            </ActionRegistryProvider>
            <Toaster />
          </ModalProvider>
        </QueryClientProvider>
      </HelmetProvider>
    </AuthProvider>
  );
};

const rootElement = document.getElementById('app')!;

if (!rootElement?.innerHTML) {
  const root = ReactDOM.createRoot(rootElement)
  root.render(<App />)
}
```

- [ ] **Step 3: Verify it compiles**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette/frontend
npx tsc --noEmit
```

Fix any type errors.

- [ ] **Step 4: Commit**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette
git add frontend/src/lib/command-palette/global-actions.ts frontend/src/main.tsx
git commit -m "feat: integrate command palette provider with global navigation actions"
```

---

### Task 5: Register actions — Projects list page

**Files:**
- Modify: `frontend/src/routes/_auth/_dashboard/projects/index.tsx`

- [ ] **Step 1: Add useRegisterActions to projects list**

Add the import at the top of the file:
```typescript
import { useRegisterActions } from "@/lib/command-palette/action-registry"
import { Workflow, Plus } from "lucide-react"  // already imported, just ensure present
```

Inside the `RouteComponent` function, after the existing hooks (after `const router = useRouter()`), add:

```typescript
useRegisterActions(
  [
    {
      id: "projects-filter-running",
      name: "Filter: Running projects",
      group: "action",
      keywords: ["filter", "running", "active"],
      icon: Activity,
      perform: () => handleFilterChange("running"),
    },
    {
      id: "projects-filter-stopped",
      name: "Filter: Stopped projects",
      group: "action",
      keywords: ["filter", "stopped", "inactive"],
      icon: Server,
      perform: () => handleFilterChange("stopped"),
    },
    {
      id: "projects-filter-all",
      name: "Filter: Show all projects",
      group: "action",
      keywords: ["filter", "all", "reset"],
      icon: FolderGit2,
      perform: () => handleFilterChange("all"),
    },
    {
      id: "projects-create-n8n",
      name: "Create n8n project",
      group: "action",
      keywords: ["n8n", "workflow", "automation", "create"],
      icon: Workflow,
      perform: () => router.navigate({ to: "/n8n" }),
    },
    {
      id: "projects-add-service",
      name: "Add new service",
      group: "action",
      keywords: ["create", "new", "project", "service", "add"],
      icon: Plus,
      perform: () => open(getLink("addNewService"), "_blank"),
    },
  ],
  [handleFilterChange, router]
)
```

- [ ] **Step 2: Commit**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette
git add frontend/src/routes/_auth/_dashboard/projects/index.tsx
git commit -m "feat: register command palette actions for projects list page"
```

---

### Task 6: Register actions — Project detail page

**Files:**
- Modify: `frontend/src/routes/_auth/_dashboard/projects/$projectId/index.tsx`

- [ ] **Step 1: Add useRegisterActions to project detail**

Add import at the top:
```typescript
import { useRegisterActions } from "@/lib/command-palette/action-registry"
import { Globe, Settings } from "lucide-react"  // already imported
```

Inside `RouteComponent`, after the project data is loaded (after `const currentState = ...` around line 127), add:

```typescript
useRegisterActions(
  [
    {
      id: "project-visit-domain",
      name: `Visit ${project.primaryDomain ?? "domain"}`,
      group: "action",
      keywords: ["open", "domain", "website", "url", "visit"],
      icon: Globe,
      enabled: !!project.primaryDomain,
      perform: () => window.open(`https://${project.primaryDomain}`, "_blank"),
    },
    {
      id: "project-settings",
      name: "Go to project settings",
      group: "navigation",
      keywords: ["settings", "config", "configure", "delete", "transfer"],
      icon: Settings,
      perform: () =>
        navigate({
          to: "/projects/$projectId/settings",
          params: { projectId },
        }),
    },
  ],
  [project.primaryDomain, projectId]
)
```

Note: `navigate` is not available in this component — it uses `Link` components. Add `useNavigate` import and call:

```typescript
import { createFileRoute, getRouteApi, Link, useNavigate } from '@tanstack/react-router'
```

And inside the component:
```typescript
const navigate = useNavigate()
```

- [ ] **Step 2: Commit**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette
git add frontend/src/routes/_auth/_dashboard/projects/\$projectId/index.tsx
git commit -m "feat: register command palette actions for project detail page"
```

---

### Task 7: Register actions — Deployments page

**Files:**
- Modify: `frontend/src/routes/_auth/_dashboard/projects/$projectId/deployments/index.tsx`

- [ ] **Step 1: Add useRegisterActions to deployments page**

Add import at the top:
```typescript
import { useRegisterActions } from "@/lib/command-palette/action-registry"
```

Inside `RouteComponent`, after the mutation hooks (after `handleDeployFromLatestCommitAsync`), add:

```typescript
useRegisterActions(
  [
    {
      id: "deployments-deploy-latest",
      name: "Deploy from latest commit",
      group: "action",
      keywords: ["deploy", "ship", "push", "release", "build"],
      icon: RocketIcon,
      enabled: !isDeployFromLatestCommitPending,
      perform: () => handleDeployFromLatestCommitAsync(),
    },
    {
      id: "deployments-refresh",
      name: "Refresh deployments",
      group: "action",
      keywords: ["refresh", "reload", "update"],
      icon: RefreshCw,
      perform: () => handleClick(),
    },
  ],
  [isDeployFromLatestCommitPending, handleDeployFromLatestCommitAsync, handleClick]
)
```

- [ ] **Step 2: Commit**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette
git add frontend/src/routes/_auth/_dashboard/projects/\$projectId/deployments/index.tsx
git commit -m "feat: register command palette actions for deployments page"
```

---

### Task 8: Register actions — Environments page

**Files:**
- Modify: `frontend/src/routes/_auth/_dashboard/projects/$projectId/environments/index.tsx`

- [ ] **Step 1: Add useRegisterActions to environments page**

Add import at the top:
```typescript
import { useRegisterActions } from "@/lib/command-palette/action-registry"
```

Inside `RouteComponent`, after the state/data hooks (after `const invalidate = ...`), add:

```typescript
const [createEnvDialogOpen, setCreateEnvDialogOpen] = useState(false)
```

Note: Check if `createEnvDialogOpen` state already exists in the component. If it does, use the existing state setter. If the `CreateEnvironmentDialog` is triggered by a different mechanism, find the state that controls it and use that.

```typescript
useRegisterActions(
  [
    {
      id: "environments-create",
      name: "Create new environment",
      group: "action",
      keywords: ["environment", "preview", "staging", "branch", "create", "new"],
      icon: Plus,
      perform: () => setCreateEnvDialogOpen(true),
    },
    {
      id: "environments-refresh",
      name: "Refresh environments",
      group: "action",
      keywords: ["refresh", "reload", "update"],
      icon: RefreshCw,
      perform: () => handleRefresh(),
    },
  ],
  [handleRefresh]
)
```

- [ ] **Step 2: Commit**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette
git add frontend/src/routes/_auth/_dashboard/projects/\$projectId/environments/index.tsx
git commit -m "feat: register command palette actions for environments page"
```

---

### Task 9: Register actions — Project settings page

**Files:**
- Modify: `frontend/src/routes/_auth/_dashboard/projects/$projectId/settings/general.tsx`

- [ ] **Step 1: Add useRegisterActions to project settings**

Add import at the top:
```typescript
import { useRegisterActions } from "@/lib/command-palette/action-registry"
```

Inside `GeneralSettingsTab`, after the existing hooks (after `const navigate = useNavigate()`), add:

```typescript
useRegisterActions(
  [
    {
      id: "settings-delete-project",
      name: "Delete project",
      group: "action",
      keywords: ["delete", "remove", "destroy"],
      icon: Trash2,
      priority: 90,
      perform: () => setWantsDeletion(true),
    },
    {
      id: "settings-transfer-project",
      name: "Transfer project to another organization",
      group: "action",
      keywords: ["transfer", "move", "migrate", "organization"],
      icon: ArrowRightLeft,
      perform: () => {
        // Scroll to transfer section
        document.getElementById("transfer-section")?.scrollIntoView({ behavior: "smooth" })
      },
    },
  ],
  []
)
```

Note: The transfer section doesn't have an id yet. The sub-agent should check if the `TransferProjectSection` has a dialog open state that can be triggered directly. If the `TransferProjectSection` manages its own `transferDialogOpen` state internally, the command palette action should scroll to the card and the user clicks the button. Alternatively, if the state can be lifted, do that.

- [ ] **Step 2: Commit**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette
git add frontend/src/routes/_auth/_dashboard/projects/\$projectId/settings/general.tsx
git commit -m "feat: register command palette actions for project settings page"
```

---

### Task 10: Register actions — Organizations pages

**Files:**
- Modify: `modules/organizations/frontend/src/routes/_auth/_dashboard/organizations/index.tsx`
- Modify: `modules/organizations/frontend/src/routes/_auth/_dashboard/organizations/$orgId/members.tsx`

- [ ] **Step 1: Add useRegisterActions to organizations list**

Add import at the top of `organizations/index.tsx`:
```typescript
import { useRegisterActions } from "@/lib/command-palette/action-registry"
```

Inside `OrganizationsPage`, after the existing hooks, add:

```typescript
useRegisterActions(
  [
    {
      id: "orgs-create",
      name: "Create new organization",
      group: "action",
      keywords: ["organization", "team", "create", "new"],
      icon: Plus,
      perform: () => setCreateDialogOpen(true),
    },
  ],
  []
)
```

- [ ] **Step 2: Add useRegisterActions to members page**

Add import at the top of `members.tsx`:
```typescript
import { useRegisterActions } from "@/lib/command-palette/action-registry"
```

Inside the members page component, after existing hooks, add:

```typescript
useRegisterActions(
  [
    {
      id: "members-invite",
      name: "Invite member",
      group: "action",
      keywords: ["invite", "add", "member", "user", "team"],
      icon: Plus,
      perform: () => setInviteDialogOpen(true),
    },
  ],
  []
)
```

Note: Find the actual state variable name that controls the invite dialog. It may be named differently — check the component code for the dialog open state.

- [ ] **Step 3: Commit**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette
git add modules/organizations/frontend/src/routes/_auth/_dashboard/organizations/index.tsx
git add modules/organizations/frontend/src/routes/_auth/_dashboard/organizations/\$orgId/members.tsx
git commit -m "feat: register command palette actions for organization pages"
```

---

### Task 11: Register actions — User settings page

**Files:**
- Modify: `frontend/src/routes/_auth/_dashboard/settings.tsx`

- [ ] **Step 1: Add useRegisterActions to user settings**

Add import at the top:
```typescript
import { useRegisterActions } from "@/lib/command-palette/action-registry"
```

Inside `RouteComponent` (the main settings component), add:

```typescript
useRegisterActions(
  [
    {
      id: "settings-create-token",
      name: "Create API token",
      group: "action",
      keywords: ["api", "token", "key", "create", "auth"],
      icon: Key,
      perform: () => {
        // Scroll to API tokens section and open dialog
        document.getElementById("api-tokens")?.scrollIntoView({ behavior: "smooth" })
      },
    },
  ],
  []
)
```

Note: Check if the `ApiTokensSection` has state that can be lifted or if a ref-based approach is better. The sub-agent should look at the actual component structure and find the most direct way to trigger the create token dialog.

- [ ] **Step 2: Commit**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette
git add frontend/src/routes/_auth/_dashboard/settings.tsx
git commit -m "feat: register command palette actions for user settings page"
```

---

### Task 12: Update CLAUDE.md with useRegisterActions documentation

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Add Command Palette Actions section to CLAUDE.md**

Add the following section after the "Frontend Architecture" section (after the "API Client: OpenAPI Qraft" subsection), before "### Configuration":

```markdown
### Command Palette Actions

The app has a global ⌘K command palette. When adding new pages or interactive features, register contextual actions so they're discoverable via keyboard:

```typescript
import { useRegisterActions } from "@/lib/command-palette/action-registry"
import { Rocket } from "lucide-react"

// Inside your route component:
useRegisterActions([
  {
    id: 'deploy-latest',           // unique across the app
    name: 'Deploy latest commit',  // what the user sees
    icon: Rocket,                  // optional Lucide icon
    group: 'action',               // 'action' | 'navigation' | 'project' | 'organization'
    keywords: ['deploy', 'ship'],  // extra fuzzy search terms
    perform: () => deployMutation.mutate(),
  },
], [deployMutation])  // deps array — re-registers when deps change
```

- `group: 'action'` — contextual actions for the current page (deploy, filter, create)
- `group: 'navigation'` — links to other pages (global actions registered in `global-actions.ts`)
- Actions auto-register on mount and auto-deregister on unmount
- Set `enabled: false` or `enabled: () => someCondition` to conditionally hide actions
```

- [ ] **Step 2: Commit**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette
git add CLAUDE.md
git commit -m "docs: add useRegisterActions pattern to CLAUDE.md"
```

---

### Task 13: Type-check and verify

**Files:** None (verification only)

- [ ] **Step 1: Run TypeScript type checking**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette/frontend
npx tsc --noEmit
```

Fix any type errors found.

- [ ] **Step 2: Run the dev server to verify it works**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette
compozerr dev
```

Verify:
1. Frontend loads at http://localhost:1234
2. ⌘K opens the command palette
3. Navigation actions appear (Projects, Settings, Organizations, Modules)
4. Typing filters results
5. Selecting a navigation action navigates correctly
6. On the projects page, contextual actions appear (filter, create)
7. On the deployments page, "Deploy from latest commit" appears

- [ ] **Step 3: Final commit if any fixes were needed**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-command-palette
git add -A
git commit -m "fix: resolve type errors and polish command palette integration"
```

---

## Sub-Agent Parallelization Strategy

Tasks can be grouped for parallel execution by sub-agents:

| Agent | Tasks | Files touched |
|-------|-------|---------------|
| **Agent 1 (Core)** | Tasks 1-4 | `components/ui/command.tsx`, `lib/command-palette/*`, `components/command-palette.tsx`, `main.tsx` |
| **Agent 2** | Tasks 5-6 | `routes/.../projects/index.tsx`, `routes/.../projects/$projectId/index.tsx` |
| **Agent 3** | Tasks 7-8 | `routes/.../deployments/index.tsx`, `routes/.../environments/index.tsx` |
| **Agent 4** | Tasks 9-11 | `routes/.../settings/general.tsx`, `routes/.../settings.tsx`, `modules/organizations/...` |
| **Agent 5** | Tasks 12-13 | `CLAUDE.md`, type checking |

**Dependency:** Agent 1 must complete first (creates the infrastructure). Agents 2-4 can run in parallel after Agent 1. Agent 5 runs after all others.
