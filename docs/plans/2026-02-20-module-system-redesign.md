# Module System Redesign

**Date**: 2026-02-20
**Status**: Approved design, pending implementation

## Problem

The current module system uses git submodules for external modules, creating a painful workflow:
- Change in module → open submodule → commit → push → go back → update hash → commit → push
- Mixed model: some modules are submodules, some are inline directories (inconsistent)
- Dependency versions are vague (`"latest"`, `"*"`) with no lockfile
- Two dependency layers (compozerr.json + .csproj) that can drift apart
- No clear versioning or marketplace publishing workflow

## Design Decisions

1. **Modules are just directories** — no submodules, no special git behavior
2. **Git subtree with --squash under the hood** — CLI wraps `git subtree` commands; users never type them
3. **compozerr.json is the single source of truth** for dependencies; .csproj references are auto-wired by CLI
4. **Server-side auto-sync** — Compozerr backend receives GitHub webhooks on push, detects module changes, and syncs them to module source repos automatically
5. **Compozerr Registry stores metadata only** — module discovery, ratings, analytics; code stays in git repos
6. **All module CLI commands nested under `compozerr module`**

## Architecture

```
YOUR PROJECT (regular git repo)
  modules/           ← just directories, committed normally
    stripe/
    auth/
    database/
  compozerr.json     ← project manifest (modules list)
  compozerr.lock     ← lockfile (source repos, exact commits)
       │                    ▲
       │ push (normal)      │ add/update (CLI)
       ▼                    │
MODULE SOURCE REPOS (GitHub)
  compozerr/stripe   ← marketplace original
  your-org/stripe    ← your fork
       │                    ▲
       │ register           │ discover
       ▼                    │
COMPOZERR REGISTRY (API)
  Metadata only: name, description, git URL, versions,
  install count, ratings, reviews, compatibility, screenshots
```

## File Formats

### Project compozerr.json (root)

```json
{
  "type": "project",
  "name": "web",
  "id": "0198a99a-d92f-70ff-b90d-201bc173963a",
  "modules": {
    "stripe": "compozerr/stripe@^1.0.0",
    "auth": "compozerr-lib/auth@^2.1.0",
    "database": "compozerr-lib/database@latest",
    "github": "your-org/github@main"
  }
}
```

- Renamed `dependencies` → `modules`
- Values are `<org/repo>@<version-or-ref>` (semver ranges, branches, commit hashes, `latest`)

### Module compozerr.json (inside each module)

```json
{
  "type": "module",
  "name": "stripe",
  "version": "1.2.0",
  "description": "Stripe billing, subscriptions, and invoices",
  "origin": {
    "repo": "https://github.com/compozerr/stripe",
    "commit": "abc123def",
    "addedAt": "2026-02-20T10:30:00Z"
  },
  "modules": {
    "auth": "compozerr-lib/auth@^2.0.0",
    "jobs": "compozerr-lib/jobs@^1.0.0"
  },
  "backend": {
    "projects": ["Stripe/Stripe.csproj"],
    "envDefaults": {
      "STRIPE_API_KEY": "",
      "STRIPE_WEBHOOK_SECRET": ""
    }
  },
  "frontend": {
    "routesDir": "frontend/src/routes",
    "package": "@repo/stripe"
  },
  "services": {
    "start": "docker compose up -d",
    "stop": "docker compose down",
    "readyCheck": "pg_isready -h localhost"
  },
  "marketplace": {
    "tags": ["billing", "payments", "subscriptions"],
    "screenshots": ["docs/screenshot-1.png"],
    "compatibility": {
      "dotnet": ">=9.0",
      "node": ">=18"
    }
  }
}
```

Key fields:
- `origin` — tracks where the module was copied from (set by CLI)
- `modules` — dependencies on other modules (consistent naming with root)
- `version` — semver, used by registry and lockfile
- `backend.projects` — .csproj paths for auto-wiring
- `services` — replaces old `start`/`end`/`readyMessage`
- `marketplace` — metadata for registry

### compozerr.lock (project root, auto-generated)

```json
{
  "lockVersion": 1,
  "modules": {
    "stripe": {
      "source": "https://github.com/compozerr/stripe",
      "version": "1.2.0",
      "resolvedCommit": "abc123def456",
      "installedAt": "2026-02-20T10:30:00Z",
      "updatedAt": "2026-02-20T10:30:00Z",
      "integrity": "sha256-xxxx",
      "dependencies": {
        "auth": "^2.0.0",
        "jobs": "^1.0.0"
      }
    }
  }
}
```

- Auto-generated, never hand-edited
- Tracks exact commit hashes for reproducibility
- Integrity hashes detect manual tampering
- Flat dependency tree (all modules top-level)

## CLI Commands

All module commands are nested under `compozerr module`:

```
compozerr module new <name>                Create new module (scaffold + source repo)
compozerr module add <org/module[@ver]>    Add existing module from marketplace/repo
compozerr module remove <module>           Remove module (unwire, delete files)
compozerr module update [module]           Pull upstream updates (3-way merge)
compozerr module status                    Show all modules, changes, warnings
compozerr module release                   Interactive version + publish to marketplace
compozerr module publish <module>          Create source repo from existing local module
compozerr module search <query>            Search marketplace
compozerr module info <module>             Show module details from marketplace
```

### compozerr module status

```
 Modules
 ──────────────────────────────────────────────────────────────
 stripe       1.2.0   1.2.0   ✓ Up to date
 auth         2.1.3   2.2.0   ↑ Update available
 database     1.0.0   1.0.0   ● Modified locally (3 files)
 github       0.5.0   0.5.0   ● Modified locally (12 files)

 Run 'compozerr module release' to publish local changes

 ⚠ Warnings
 ──────────────────────────────────────────────────────────────
 ⚠ modules/analytics/  Not registered — not in compozerr.lock
   Run 'compozerr module add <org/analytics>' or 'compozerr module publish analytics'
```

### compozerr module release (interactive Ink UI)

```
Modules with unpublished changes:

  ● database    (3 files changed since v1.0.0)
  ● github      (12 files changed since v0.5.0)

? Select module to version: github
? New version (current: 0.5.0): 0.6.0
? Release notes: Added webhook support for PR events

✓ Published github@0.6.0 to marketplace
  3 projects using this module will see the update
```

Requires project to be pushed to origin first (so Compozerr backend can verify the commit).

## Sync Workflow

### Developer's Perspective

1. Edit `modules/stripe/` in your project (add endpoints, change logic, whatever)
2. `git commit && git push` — normal push to your project repo
3. Done. That's the entire workflow for making changes.

### Behind the Scenes (Server-Side)

4. GitHub webhook fires → Compozerr backend receives push event
5. Backend diffs the push, detects changed files under `modules/stripe/`
6. Backend extracts those changes and applies them to the stripe module source repo
7. Module source repo updated at a new commit

### Versioning

8. Developer runs `compozerr module release`
9. Interactive Ink UI shows modules with unpublished changes
10. Developer selects module, enters version number
11. Backend verifies: latest push matches what it has via GitHub
12. Backend tags the version on the module source repo
13. Marketplace displays new version

## Dependency Resolution

### How `compozerr module add stripe` Works

1. Fetch stripe's `compozerr.json` from source repo
2. Read `modules` field → needs `auth@^2.0.0`, `jobs@^1.0.0`
3. Recursively resolve: auth needs `database@^1.0.0`
4. Full dependency tree:
   ```
   stripe@1.2.0
   ├── auth@2.1.3
   │   └── database@1.0.0
   └── jobs@1.0.0
   ```
5. Check lockfile for already-installed modules → skip those
6. For each new module (in dependency order):
   - `git subtree add --prefix=modules/<name> <repo> <ref> --squash`
   - Add .csproj to `backend/Base.sln`
   - Add `<ProjectReference>` to `backend/Api/Api.csproj`
   - Wire frontend workspace if applicable
7. Generate/update `compozerr.lock`
8. Commit all changes

### Version Conflict Resolution

If stripe needs `auth@^2.0.0` but project has `auth@1.5.0`:
```
⚠ Version conflict:
  stripe requires auth@^2.0.0
  Your project has auth@1.5.0

? How to resolve?
  ❯ Upgrade auth to 2.1.3 (latest compatible)
    Keep auth@1.5.0 (stripe may not work correctly)
    Cancel adding stripe
```

### Auto-Wiring

The CLI reads module's `compozerr.json` to auto-wire:
- **Backend**: `backend.projects` → adds to .sln, adds ProjectReferences to Api.csproj
- **Frontend**: `frontend.routesDir` → already auto-discovered by `virtual-route-config.ts`
- **Frontend packages**: `frontend.package` → added to npm workspace
- **Services**: `services` → registered for dev orchestration

## What Changes Where

### CLI Repo (`/Users/milo/milodev/gits/cli`)

**Restructure**: Move module commands under `compozerr module` subcommand group:
- `module/new.command.tsx` — create new module (rewrite from submodule to subtree)
- `module/add.command.tsx` — add existing module (rewrite from submodule to subtree)
- `module/remove.command.tsx` — remove module (rewrite for subtree)
- `module/update.command.tsx` — NEW: pull upstream updates
- `module/status.command.tsx` — NEW: show all modules + warnings
- `module/release.command.tsx` — NEW: interactive versioning + marketplace publish
- `module/publish.command.tsx` — NEW: create source repo from local module
- `module/search.command.tsx` — NEW: search marketplace
- `module/info.command.tsx` — NEW: show module details

**Utilities**:
- `utils/lockfile.ts` — NEW: lockfile management
- `utils/dependency-resolver.ts` — NEW: recursive dependency resolution
- `utils/auto-wirer.ts` — NEW: .csproj/.sln/frontend wiring
- `utils/adder.ts` — rewrite (subtree instead of submodule)
- `utils/remover.ts` — rewrite (subtree removal)
- `utils/git.ts` — add subtree methods, remove submodule methods
- `types/compozerr-file.ts` — update schema to new format

### Web Repo (`/Users/milo/milodev/gits/web`)

- Update root `compozerr.json` to new schema
- Add `compozerr.lock` (generated by CLI)
- Update each module's `compozerr.json` to new schema
- Migration guide for converting existing submodules to subtrees

### Backend API (Web Repo Backend)

- New/updated endpoints for module registry metadata
- GitHub webhook handler for auto-syncing module changes to source repos
- Marketplace metadata storage and query endpoints

### What Stays The Same

- Backend `IFeature` discovery — zero changes (scans DLLs via reflection)
- Frontend route merging via `virtual-route-config.ts` — zero changes (reads `compozerr.json`)
- MediatR/Carter patterns — untouched
- Turbo build system — untouched
- Dev orchestration (`scripts/dev.ts`) — minimal changes (reads new `services` field)

## Migration Path

Since modules are only used in one project (web), migration is manual:

1. Convert 4 git submodules to git subtrees (see migration guide)
2. Update compozerr.json files to new schema
3. Generate compozerr.lock from current state
4. Update CLI to new commands

See `docs/plans/2026-02-20-submodule-to-subtree-migration-guide.md` for step-by-step instructions.
