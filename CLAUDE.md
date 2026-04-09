# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

This is a modular monorepo combining a .NET 9.0 backend with a React + TypeScript frontend. The architecture uses a feature-based module system where each module can contribute both backend functionality and frontend routes/components.

**Related Repository**: There is a separate hosting repository at `/Users/milo/milodev/gits/hosting` that manages the deployment infrastructure. This repository contains:
- Node.js/TypeScript services that run on hosting servers
- Docker Compose management for deployed projects
- Port detection and service discovery
- Traefik dynamic configuration for HTTP and TCP routing
- Deployment orchestration and monitoring

## Prerequisites

- Node.js >= 18
- .NET SDK 9.0.200 (specified in global.json)
- Deno (for dev scripts)

## Common Commands

### Development

```bash
# Start everything (installs deps + copies .env automatically if needed)
compozerr dev
# Frontend: http://localhost:1234
# Backend: http://localhost:1235 (Swagger: http://localhost:1235/swagger/v1/swagger.json)

# If compozerr CLI is not installed:
curl -fsSL https://compozerr.com/install | bash
```

`compozerr dev` handles everything automatically:
- Detects missing `node_modules` → runs `npm install`
- Detects missing `backend/.env` in worktrees → copies from main worktree
- Starts Docker (Postgres + backend with hot-reload) and frontend (Vite)

**Requires**: Docker Desktop running. The backend runs inside a Docker container.

**Important**: Always run `compozerr dev` and test changes in the browser before pushing frontend changes to production.

### Backend

```bash
# Run backend only
cd backend && dotnet watch run --project Api --urls http://localhost:5000

# Run tests
dotnet test backend/Api.Tests/Api.Tests.csproj
dotnet test  # Run all tests in solution

# Run a single test
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Build backend
cd backend && dotnet build

# Restore dependencies
cd backend && dotnet restore
```

### Frontend

```bash
# Run frontend only
cd frontend && npm run dev

# Type checking
cd frontend && npm run typecheck

# Build for production
cd frontend && npm run build

# Regenerate API client (requires backend running on port 5000)
cd frontend && npx openapi-qraft --plugin tanstack-query-react --plugin openapi-typescript http://localhost:5000/swagger/v1/swagger.json --clean -o src/generated --export-type --properties-required-by-default --default-non-nullable
```

### Deployment

```bash
# Push to main, tag with nbgv, and push the tag to trigger deploy
git push compozerr main && nbgv tag && git push compozerr $(git describe --tags --abbrev=0)
```

## Architecture

**Database**: PostgreSQL with Entity Framework Core

### Feature-Based Backend Module System

The backend uses a custom **Feature** pattern for modular architecture:

- **Core Interface**: All features implement `IFeature` (located in `backend/Core/Feature/IFeature.cs`)
- **Lifecycle Methods**:
  - `ConfigureBuilder(WebApplicationBuilder)` - Configure builder-level settings
  - `ConfigureServices(IServiceCollection, IConfiguration)` - Register DI services
  - `ConfigureApp(WebApplication)` - Configure middleware/app setup
- **Auto-Discovery**: Features are automatically discovered via reflection at startup
- **Location**: Feature implementations are in `backend/Core/Feature/Features.cs`

**Example Feature Structure**:
```csharp
public class MyFeature : IFeature
{
    public bool IsEnabled => true;

    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<MyDbContext>();
        services.AddScoped<IMyService, MyService>();
    }
}
```

### Module Organization

Modules live in `modules/` directory with this structure:

```
modules/
  ├── auth/
  │   ├── backend/
  │   │   ├── Auth/            (Main feature + endpoints)
  │   │   └── Auth.Abstractions/
  │   ├── frontend/            (@repo/auth package)
  │   └── compozerr.json
  ├── stripe/
  ├── database/
  ├── github/
  └── ...
```

**Key Files**:
- `compozerr.json` - Module configuration (dependencies, routes, env vars)
- Backend implements `IFeature` for service registration
- Frontend exposes components via package.json exports

### Backend Patterns

**1. MediatR Command Pattern**

All business logic uses MediatR commands/queries:

```csharp
// Command definition
public sealed record GetInvoicesCommand : ICommand<GetInvoicesResponse>;

// Handler
public sealed class GetInvoicesCommandHandler : ICommandHandler<GetInvoicesCommand, GetInvoicesResponse>
{
    public async Task<GetInvoicesResponse> Handle(GetInvoicesCommand command, CancellationToken ct)
    {
        // Implementation
    }
}

// Endpoint (using Carter)
public class InvoicesEndpoints : CarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/invoices", async (GetInvoicesCommand cmd, IMediator mediator)
            => await mediator.Send(cmd));
    }
}
```

**2. Endpoint Routing with Carter**

Endpoints are defined using Carter modules:
- Extend `CarterModule` base class
- Implement `AddRoutes(IEndpointRouteBuilder)`
- Located in module's `Endpoints/` directory
- Example: `modules/stripe/backend/Stripe/Endpoints/Invoices/`

**3. FluentValidation**

Request validation uses FluentValidation:
- Validators automatically registered via `RegisterValidatorsInAssemblyFeatureConfigureCallback`
- Create validator classes: `public class MyCommandValidator : AbstractValidator<MyCommand>`

**4. Accessing Current User**

In command handlers, access the authenticated user via:
```csharp
var userId = CurrentUserAccessor.CurrentUserId!;
```

### Frontend Architecture

**1. Routing: TanStack Router**

- File-based routing with auto-generated route tree
- Routes defined in `frontend/src/routes/`
- Generated file: `frontend/src/routeTree.gen.ts` (do not edit manually)
- Naming conventions:
  - `_layout.tsx` - Layout routes (no path segment)
  - `$param.tsx` - Dynamic route parameters
  - `index.tsx` - Index route for directory

**2. Module Route Integration**

Modules can contribute routes via `compozerr.json`:

```json
{
  "frontend": {
    "routesDir": "frontend/src/routes"
  }
}
```

Routes are discovered via `virtual-route-config.ts` and merged into the main route tree automatically.

**3. API Client: OpenAPI Qraft**

Type-safe API client generated from backend OpenAPI spec:

- **Generated files**: `frontend/src/generated/`
- **Client setup**: `frontend/src/api-client.ts`
- **Usage**:
  ```typescript
  import { api } from "./api-client";

  // React Query hook
  const { data, isLoading } = api.v1.getAuthMe.useQuery();

  // Mutations
  const { mutate } = api.v1.postLogin.useMutation();
  ```
- **Regeneration**: Run when backend OpenAPI spec changes

**4. Command Palette Actions**

The app has a global `⌘K` command palette. When adding new pages or interactive features, register contextual actions so they're discoverable via keyboard:

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

### Configuration

**Environment Variables**:
- Backend: `backend/.env` (see `backend/.env.example` for required vars)
- Frontend: Vite env vars with `VITE_` prefix
- Module defaults: Defined in module's `compozerr.json` under `backend.defaultEnvProperties`

**Important**: The backend uses a custom `.env` file loader via `builder.Configuration.AddEnvFile(".env")` (not standard .NET configuration).

## Creating a New Module

### Backend Module

1. Create directory structure:
   ```
   modules/mymodule/
   ├── backend/
   │   └── MyModule/
   │       ├── MyModuleFeature.cs
   │       ├── Endpoints/
   │       ├── Commands/
   │       └── MyModule.csproj
   ├── compozerr.json
   ```

2. Implement `IFeature` in `MyModuleFeature.cs`

3. Add project reference to `backend/Base.sln`

4. Configure `compozerr.json`:
   ```json
   {
     "type": "module",
     "name": "mymodule",
     "dependencies": {},
     "backend": {
       "defaultEnvProperties": {}
     }
   }
   ```

### Frontend Module Integration

1. Add frontend configuration to `compozerr.json`:
   ```json
   {
     "frontend": {
       "routesDir": "frontend/src/routes"
     }
   }
   ```

2. Create module frontend:
   ```
   modules/mymodule/frontend/
   ├── package.json          (with exports)
   ├── src/
   │   ├── routes/           (TanStack Router routes)
   │   └── components/       (Reusable components)
   ```

3. Export components in `package.json`:
   ```json
   {
     "name": "@repo/mymodule",
     "exports": {
       "./my-component": "./src/components/my-component.tsx"
     }
   }
   ```

4. Use in main app: `import { MyComponent } from "@repo/mymodule/my-component"`

## Development Workflow

### Working on Backend

1. Entry point: `backend/Api/Program.cs`
2. Features automatically discovered from `backend/Core/Feature/Features.cs`
3. Use `dotnet watch` for hot reload (configured in dev script)
4. Attach debugger: Press F5 in VS Code after backend starts

### Working on Frontend

1. Routes auto-regenerate when files change in `frontend/src/routes/`
2. API client types in `frontend/src/generated/` - regenerate when backend changes
3. Vite dev server has HMR enabled
4. Import module components from `@repo/<module-name>/...`

### Adding Dependencies

- **Backend**: Add NuGet package to appropriate `.csproj` file
- **Frontend**: Run `npm install <package>` from `frontend/` directory
- **Module**: Add to module's `compozerr.json` dependencies section

### Database Migrations

Modules with Entity Framework contexts typically run migrations in their Feature's `ConfigureApp` method:

```csharp
void IFeature.ConfigureApp(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    context.Database.Migrate();
}
```

## Important Files

| File | Purpose | Edit? |
|------|---------|-------|
| `backend/Api/Program.cs` | Backend entry point | Yes |
| `backend/Core/Feature/Features.cs` | Feature discovery and lifecycle | Rarely |
| `frontend/src/routeTree.gen.ts` | Generated route tree | No - auto-generated |
| `frontend/src/api-client.ts` | API client configuration | Yes |
| `frontend/virtual-route-config.ts` | Module route integration | Rarely |
| `modules/*/compozerr.json` | Module configuration | Yes |
| `scripts/dev.ts` | Development orchestration | Rarely |
| `turbo.json` | Build task configuration | Rarely |

## Troubleshooting

### Backend won't start

- Check `backend/.env` has required variables (see `.env.example`)
- Verify .NET SDK version: `dotnet --version` (should be 9.0.x)
- Check for port conflicts on 5000

### Frontend build errors

- Regenerate route tree: Delete `frontend/src/routeTree.gen.ts` and restart dev server
- Regenerate API client: Check that backend OpenAPI endpoint is accessible
- Clear Vite cache: `rm -rf frontend/node_modules/.vite`

### Module not loading

- Verify module is referenced in `backend/Base.sln`
- Check `compozerr.json` syntax
- Ensure Feature class implements `IFeature` and `IsEnabled` returns true
- Check for compilation errors: `dotnet build backend/Base.sln`

### Type errors in API calls

- Backend OpenAPI spec changed - regenerate frontend API client
- Check that `frontend/src/generated/` is up to date
- Verify API endpoint exists in backend

## Running from Git Worktrees

Worktrees work out of the box — just run `compozerr dev`. The CLI auto-handles everything:

```bash
git worktree add .worktrees/my-feature -b feature/my-feature
cd .worktrees/my-feature
compozerr dev
# Automatically: npm install → copy .env → start Docker + frontend
```

Modules are regular git-tracked directories (subtrees, not submodules) — no `git submodule update` needed.

### Notes

- **macOS port 5000 conflict**: AirTunes/AirPlay uses port 5000. The docker-compose maps backend to port 1235 externally. Don't try to access `localhost:5000` — use `localhost:1235`.
- **`dotnet watch` is stricter than `dotnet build`**: If `Api.csproj` references a project file that doesn't exist, `dotnet build` logs a warning but `dotnet watch` (used inside Docker) treats it as fatal. Keep csproj references clean.
- **Fresh clones** (not worktrees): `backend/.env` must be created manually since there's no main worktree to copy from. See `backend/.env.example` for required variables. Future CLI improvement: bootstrap from module `envDefaults` in `compozerr.json`.

## Module System

Modules live in `modules/` as regular git-tracked directories (converted from submodules to subtrees). There are plans to further evolve the module system — see `docs/plans/2026-02-20-module-system-redesign.md` and the implementation plan.

**Migration scripts** (already run):
- `scripts/migrate-submodules-to-subtrees.sh` — converts submodules to subtrees
- `scripts/test-subtree-migration.sh` — validates the migration

## Production Infrastructure Access

### SSH Hosts

- **Backend + DB**: `ssh debian@145.239.0.233 -p 2222` (also `ssh debian@compozerr.com -p 2222`)
  - Backend container: `docker exec main-backend-1 ...`
  - Postgres: `docker exec main-db-1 psql -U postgres -d myapp -c '...'`
  - Backend logs: `docker logs main-backend-1 --since 5m`
- **Hosting manager (fra1)**: `ssh debian@fra1.compozerr.com`
  - Traefik dynamic config: `/opt/traefik/traefik/config/dynamic.yml`
  - Manager logs: `sudo journalctl -u manager --since '5 minutes ago' --no-pager`
- **Project VMs**: `compozerr ssh -e <env>` from within the project directory (e.g. `/Users/milo/milodev/gits/blog`)

### Related Local Repos

- **Blog (mappso-blog)**: `/Users/milo/milodev/gits/blog` — project ID `4744a6b5-a168-4d86-a323-f42022257cb1`
- **Hosting**: `/Users/milo/milodev/gits/hosting` — auto-deploys on push to main with `manager/**` changes
- **CLI**: `/Users/milo/milodev/gits/cli`

## Key Architectural Decisions

1. **Feature-based modularity**: Modules are auto-discovered via reflection
2. **MediatR for all business logic**: Commands/queries for consistency and testability
3. **Type-safety across stack**: OpenAPI generates TypeScript types from C# definitions
4. **File-based routing**: Frontend routes mirror file structure
5. **Strongly-typed IDs**: All entities use `IdBase<T>` pattern (e.g., `UserId`, `ProjectId`)
