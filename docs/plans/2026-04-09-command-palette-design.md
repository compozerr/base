# Command Palette (⌘K) Design Spec

## Overview

A global command palette for the Compozerr web dashboard, triggered by `⌘K` / `Ctrl+K`. Provides fuzzy search across all entities (projects, organizations) and contextual actions from the current page. Follows the Linear-style flat list pattern — no nesting, everything one keystroke away.

**Library:** shadcn `<Command>` component (thin wrapper around cmdk by pacocoursey).

## Architecture

Four decoupled layers, each with a single responsibility:

| Layer | Responsibility | File(s) |
|-------|---------------|---------|
| **Action Registry** | React context + `useRegisterActions` hook. Stores/deregisters actions by component lifecycle | `frontend/src/lib/command-palette/action-registry.tsx` |
| **Search Engine** | Fuzzy matches registered actions + debounced API results. Handles cross-org logic | `frontend/src/lib/command-palette/search.ts` |
| **Command Palette UI** | The `⌘K` dialog using shadcn `<Command>`. Renders groups, items, keyboard nav | `frontend/src/components/command-palette.tsx` |
| **Route Action Registrations** | Each page calls `useRegisterActions()` to declare its contextual actions | Spread across route files |

### Provider Placement

```
QueryClientProvider
  └── ModalProvider
       └── CommandPaletteProvider  ← NEW
            └── RouterProvider
```

`CommandPaletteProvider` needs access to:
- React Query (for API search results)
- Router (for navigation actions)
- Org storage (for cross-org switching)

The provider holds the action registry state and renders the `<Command>` dialog (always mounted, hidden until triggered).

## Action Registry

### Action Interface

```typescript
interface Action {
  id: string                          // unique key, e.g. "deploy-latest"
  name: string                        // display label, e.g. "Deploy latest commit"
  icon?: LucideIcon                   // optional icon
  group: 'navigation' | 'action' | 'project' | 'organization'
  keywords?: string[]                 // extra fuzzy match terms
  shortcut?: string[]                 // e.g. ['d', 'l'] — display only hint
  perform: () => void | Promise<void> // what happens on select
  enabled?: boolean | (() => boolean) // conditionally show/hide (false = invisible, not disabled)
  priority?: number                   // sort weight within group (lower = higher)
}
```

### useRegisterActions Hook

```typescript
function useRegisterActions(actions: Action[], deps?: DependencyList): void
```

- Registers actions on mount, deregisters on unmount
- Re-registers when `deps` change (like `useEffect`)
- Actions with `enabled: false` (or `enabled()` returning false) are excluded from results entirely

**Usage:**
```tsx
// In any route component
useRegisterActions([
  {
    id: 'deploy-latest',
    name: 'Deploy latest commit',
    icon: Rocket,
    group: 'action',
    keywords: ['deploy', 'ship', 'push'],
    perform: () => deployMutation.mutate(),
  },
], [deployMutation])
```

### Registry Implementation

- Internal state: `Map<string, Action>` in context
- `register(actions: Action[])` — adds to map, returns cleanup function
- `getActions()` — returns all enabled actions
- Consumers subscribe via context — re-render when actions change

## Search

Two sources combined into one flat result list:

### 1. Local Actions (instant)

Fuzzy-matched against all registered actions using cmdk's built-in `command-score` algorithm. Matches against `name` and `keywords`. No API call needed.

### 2. API Results (debounced 300ms)

Two queries fire in parallel when the user types:
- **Projects:** The projects endpoint is org-scoped (via `X-Organization-Id` header). For cross-org search, we fetch projects for each org in parallel using `Promise.all`. The number of orgs per user is small (typically < 10), so this is performant. Each result carries its `organizationId` for cross-org switching logic.
- **Organizations:** `api.v1.getOrganizations.useQuery()` — client-side filtered (list is small, always pre-fetched)

API results are converted into temporary `Action` objects with navigation as their `perform` function.

### Cross-Organization Logic

- Each result displays its org name as secondary text: "my-saas-app · Acme Corp"
- Results from a different org than the currently active one show a subtle indicator: "↗ Acme Corp"
- On select: `setCurrentOrgId(targetOrgId)` → invalidate project queries → navigate to entity
- Seamless switch, no confirmation step

### Result Grouping & Ordering

Groups displayed as inline headers (Linear-style):

```
── Actions ──────────────       (page-contextual, first if matches exist)
   Deploy latest commit
   Cancel deployment
── Projects ──────────────      (API results)
   🟢 my-saas-app · Acme Corp
   🔴 landing-page · Personal
── Organizations ─────────
   👥 Acme Corp  ↗
── Navigation ────────────      (static links)
   Settings
   API Tokens
```

### States

- **Empty input:** Show all registered actions + recent/pinned items
- **Typing:** Local actions filter instantly, API results show loading indicator in group header
- **No results:** "No results found. Try a different search."

## Command Palette UI

### Component Structure

```tsx
<Dialog open={open} onOpenChange={setOpen}>
  <Command shouldFilter={false}>  {/* custom filtering via search engine */}
    <Command.Input placeholder="Search or type a command..." />
    <Command.List>
      <Command.Empty>No results found.</Command.Empty>
      <Command.Loading>Searching...</Command.Loading>
      {groups.map(group => (
        <Command.Group heading={group.label}>
          {group.items.map(item => (
            <Command.Item onSelect={item.perform}>
              {item.icon && <item.icon />}
              <span>{item.name}</span>
              {item.subtitle && <span className="text-muted">{item.subtitle}</span>}
              {item.shortcut && <Kbd>{item.shortcut}</Kbd>}
            </Command.Item>
          ))}
        </Command.Group>
      ))}
    </Command.List>
  </Command>
</Dialog>
```

### Keyboard & UX

- `⌘K` (Mac) / `Ctrl+K` (Windows) — toggle open/close
- `Escape` — close
- Arrow keys — navigate items
- `Enter` — execute selected action
- Input auto-focused on open, cleared on close
- Dialog closes after action execution
- Focus trapping and scroll handled by shadcn/cmdk

## Global Actions

Registered in `CommandPaletteProvider` — always available from any page:

### Navigation

| Action | Target |
|--------|--------|
| Go to Projects | `/projects` |
| Go to Settings | `/settings` |
| Go to Organizations | `/organizations` |
| Go to Modules | `/my-modules` |

### Actions

| Action | Effect |
|--------|--------|
| Create new project | Navigate to add-new-service link |
| Create new organization | Open CreateOrgDialog |
| Create API token | Navigate to `/settings` |
| Switch to {org name} | One entry per org — `setCurrentOrgId` + invalidate + navigate to `/projects` |

## Page-Specific Actions

Registered via `useRegisterActions` in each route component:

| Page | Actions |
|------|---------|
| **Projects list** | Filter by state (Running/Stopped/Starting), Create n8n project |
| **Project detail** | Start/Stop project, Visit domain, Go to settings |
| **Deployments** | Deploy latest commit, Cancel deployment, Refresh deployments |
| **Environments** | Create environment, Refresh environments |
| **Environment detail** | Deploy to environment, Delete environment |
| **Project settings** | Transfer project, Delete project |
| **Org detail** | View members |
| **Org members** | Invite member |
| **User settings** | Create API token |

## File Structure

```
frontend/src/
├── components/
│   ├── ui/
│   │   └── command.tsx              ← shadcn Command (npx shadcn add command)
│   └── command-palette.tsx          ← Main palette UI component
├── lib/
│   └── command-palette/
│       ├── action-registry.tsx      ← Context + useRegisterActions hook
│       ├── search.ts                ← Search engine (local + API)
│       ├── types.ts                 ← Action interface & types
│       └── global-actions.ts        ← Always-available actions
└── routes/
    └── _auth/_dashboard/
        ├── projects/index.tsx       ← + useRegisterActions call
        ├── projects/$projectId/
        │   ├── index.tsx            ← + useRegisterActions call
        │   ├── deployments/index.tsx ← + useRegisterActions call
        │   ├── environments/index.tsx ← + useRegisterActions call
        │   └── settings/general.tsx  ← + useRegisterActions call
        └── settings.tsx             ← + useRegisterActions call
```

## Implementation Notes

### Dependencies

- `cmdk` — installed automatically when adding shadcn Command component
- No other new dependencies needed

### CLAUDE.md Update

Add a section documenting the `useRegisterActions` pattern so all future development follows it:

```markdown
### Command Palette Actions

When adding new pages or interactive features, register actions with the command palette:

\`\`\`typescript
import { useRegisterActions } from "@/lib/command-palette/action-registry"

useRegisterActions([
  {
    id: 'my-action',
    name: 'Do something',
    icon: SomeIcon,
    group: 'action',
    perform: () => { /* ... */ },
  },
], [/* deps */])
\`\`\`
```

### Sub-Agent Strategy for Implementation

The `useRegisterActions` calls across route files are independent of each other and can be implemented in parallel by sub-agents:

- **Agent 1:** Core infrastructure (action registry, search engine, command palette UI, provider setup)
- **Agent 2:** Global actions + project list page actions
- **Agent 3:** Project detail + deployments page actions
- **Agent 4:** Environments + settings page actions
- **Agent 5:** Organization pages actions (module routes)

Each agent works on its own set of files with no conflicts.

### Worktree

All implementation work happens on the `feature-command-palette` worktree branch at:
```
/Users/milo/milodev/gits/web/.worktrees/feature-command-palette
```
