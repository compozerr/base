# Projects UI/UX Redesign Design

## Overview
Redesign the entire `/projects` route tree to transform the generic admin template into a modern, developer-focused dashboard inspired by Vercel/Railway aesthetics.

## Target Audience
Developers deploying containerized applications. The UI should feel like a premium developer tool.

## Design Language
- **Theme**: Dark glassmorphic with subtle gradients and frosted glass card effects
- **Typography**: System font stack with monospace accents for technical data (IDs, hashes, domains)
- **Color**: Dark base (`bg-black` / `bg-zinc-950`), accent blues/greens for status, subtle border glows
- **Motion**: Framer Motion for micro-animations - status transitions, card hovers, deployment progress
- **Spacing**: Generous whitespace, clear visual hierarchy

## Pages to Redesign

### 1. Dashboard Layout (`_dashboard.tsx`)
- Refined sidebar with active indicators and project quick-switch
- Mobile: bottom navigation bar (Projects, Settings, Docs, Profile)
- Breadcrumbs in header area
- Command-palette style search hint

### 2. Projects List (`/projects/index.tsx`)
- Summary bar (total | running | stopped) instead of 3 separate stat cards
- Card grid replacing DataTable - each card shows:
  - Project name + type icon (Docker/Compose/n8n)
  - Status dot with pulse for running
  - Primary domain link
  - Last deployment time
  - Server tier badge
- Search becomes command-bar style input
- Filter as segmented control
- Floating "Add Service" button

### 3. Project Overview (`/projects/$id/index.tsx`)
- Hero section: project name, status badge with glow, quick actions
- Bento grid: details, domains (as clickable chips), resource usage, recent deploys
- Resource sparklines visible immediately
- Quick domain access links

### 4. Project Route Layout (`/projects/$id/route.tsx`)
- Sub-navigation as horizontal scroll tabs (mobile-friendly)
- Breadcrumb trail
- Visit button stays prominent

### 5. Deployments List (`/projects/$id/deployments/index.tsx`)
- Timeline-style instead of flat rows
- Status indicator with commit info
- Duration bars, expandable previews
- Active deployment progress animation

### 6. Deployment Detail (`/projects/$id/deployments/$id/index.tsx`)
- Better terminal log viewer with color coding
- Side-by-side info → stacks on mobile
- Redeploy action prominent

### 7. Settings Route + Sub-pages
- Clean sectioned layout
- Better env var editor
- Domain visual status indicators

## Technical Constraints
- Use existing shadcn/ui components where possible
- Leverage framer-motion (already installed)
- Keep all existing API calls and data flow intact
- Responsive: mobile-first, desktop enhanced
- No new backend changes needed

## Files to Modify
- `frontend/src/routes/_auth/_dashboard.tsx`
- `frontend/src/routes/_auth/_dashboard/projects/index.tsx`
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/index.tsx`
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/route.tsx`
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/deployments/index.tsx`
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/deployments/$deploymentId/index.tsx`
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/settings/route.tsx`
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/settings/general.tsx`
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/settings/environment.tsx`
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/settings/domains.tsx`
- `frontend/src/components/app-sidebar.tsx`
- `frontend/src/components/data-table.tsx` (may be preserved, just not used for projects)
- `frontend/src/components/usage-graph.tsx`
- `frontend/src/index.css` (CSS variable tweaks if needed)
