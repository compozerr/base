# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

This is a modular monorepo combining a .NET 9.0 backend with a React + TypeScript frontend. The architecture uses a feature-based module system where each module can contribute both backend functionality and frontend routes/components.

## Prerequisites

- Node.js >= 18
- .NET SDK 9.0.200 (specified in global.json)
- Deno (for dev scripts)

## Common Commands

### Development

```bash
# Install all dependencies (backend + frontend)
npm run install:dep

# Start development servers (frontend + backend in parallel)
npm run dev
# Frontend: http://localhost:3001
# Backend: http://localhost:5000

# Build everything
npm run build
```

### Backend

```bash
# Run backend only
cd backend && dotnet watch run --project Api --urls http://localhost:5000

# Run tests
dotnet test backend/Api.Tests/Api.Tests.csproj
dotnet test  # Run all tests in solution

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
```

### Deployment

```bash
compozerr deploy project
```

## Architecture

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

## Key Architectural Decisions

1. **Feature-based modularity**: Modules are discovered automatically via reflection, no explicit registration needed
2. **MediatR everywhere**: All business logic goes through commands/queries for consistency and testability
3. **Type-safety across stack**: OpenAPI generates TypeScript types from C# definitions
4. **File-based routing**: Frontend routes mirror file structure, modules contribute routes via configuration
5. **Monorepo with workspaces**: npm workspaces for frontend packages, project references for backend
