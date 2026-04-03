# Environment-Centric UI Refactor

## Problem

Settings that are per-environment (server tier, auto-deploy, environment variables, domains) currently live in a project-level Settings tab. With preview environments, each environment needs its own configuration. The current structure conflates project-level and environment-level concerns.

## Design

### New Navigation Structure

```
/projects/$projectId/
  Overview          — project dashboard (unchanged)
  Deployments       — deployment history (unchanged, already shows env badges)
  Environments/
    /               — environment list (production card + preview cards)
    /$environmentId — environment detail page (NEW)
  Settings          — project-level admin only
```

### What Moves Where

| Setting | Current Location | New Location |
|---------|-----------------|--------------|
| Server Tier | Settings > General | Environment detail page |
| Auto-Deploy toggle | Settings > General | Environment detail page |
| Environment Variables | Settings > Environment tab | Environment detail page |
| Domains | Settings > Domains tab | Environment detail page |
| Deploy button | Environments list (per card) | Environment detail page (primary action) |
| Linked Repository | Settings > General | Settings (stays) |
| Transfer Project | Settings > General | Settings (stays) |
| Delete Project | Settings > General | Settings (stays) |

### Environment Detail Page (`/environments/$environmentId`)

When clicking an environment card, navigate to a detail page with sections:

**Header:**
- Environment name + type badge (Production/Preview)
- Branch(es) with git icon
- Preview URL (if preview env)
- Deploy button (primary action, top-right)
- Delete button (preview only, danger)

**Sections (vertical layout, not tabs — simpler for fewer items):**

1. **Deployments** — Recent deployments for this environment (filtered from project deployments by environmentId). Shows last 5 with "View all" link to the main Deployments page filtered.

2. **Server Tier** — Tier selector cards (same UI as current General Settings). For production, this changes the project's billing. For preview, it changes that environment's billing.

3. **Auto-Deploy** — Toggle switch. Controls whether pushes to this environment's branch trigger automatic deployments.

4. **Environment Variables** — Key-value editor (same UI as current Settings > Environment tab). Scoped to this environment's branch. System type tabs if applicable.

5. **Domains** — Domain management (same UI as current Settings > Domains tab). For production: custom domains. For preview: shows auto-generated subdomain + ability to add custom domains.

**Back navigation:** Breadcrumb shows `Environments > {name}`, clicking Environments returns to the list.

### Settings Tab (Simplified)

After the move, Settings contains only project-level admin:

- **Linked Repository** — GitHub repo status, reconnect, change repo
- **Transfer Project** — Transfer to another organization
- **Danger Zone** — Delete project

The sub-tabs (General, Environment, Domains) are removed. Everything fits on one page.

### Backend Changes

**New endpoint:** `GET /v1/projects/{projectId}/environments/{environmentId}/deployments`
- Returns deployments filtered by EnvironmentId
- Paginated, same response format as project deployments

**Modified endpoints:**
- Tier change (`POST /stripe/subscriptions/upsert`) — needs to accept `environmentId` to change tier for a specific environment
- Environment variables (`GET/PUT /projects/{projectId}/environment`) — already branch-scoped, may need to accept environmentId instead of branch query param
- Domains (`GET/PUT /projects/{projectId}/domains`) — needs to support per-environment domain management (production domains stay as-is, preview domains are separate)

**No new models needed** — `ProjectEnvironment` already has `ServerTierId`, `Branches`, `AutoDeploy`, and `Subdomain` fields.

### Frontend Changes

**New files:**
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/environments/$environmentId/index.tsx` — environment detail page

**Modified files:**
- `environments/index.tsx` — environment cards become clickable links to detail page
- `settings/route.tsx` — remove Environment and Domains sub-tabs
- `settings/general.tsx` — remove Server Tier and Auto-Deploy sections
- `settings/environment.tsx` — move content to environment detail page, delete file
- `settings/domains.tsx` — move content to environment detail page, delete file

### Migration Path

1. Build the environment detail page with all sections
2. Make environment cards clickable (link to detail page)
3. Move tier, auto-deploy, env vars, domains into detail page
4. Simplify Settings to project-level only
5. Remove old settings sub-tabs

Each step is independently deployable.

## Out of Scope

- Environment comparison/diff views
- Environment cloning (duplicate settings from one env to another)
- Multi-branch per environment management UI
- Environment promotion UI (already has backend support)
