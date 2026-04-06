# Module Marketplace Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Use frontend-design:frontend-design skill for ALL frontend page implementations (Tasks 8-13) to create distinctive, production-grade UI.

**Goal:** Build a full-stack module marketplace with backend registry, public browse/search pages (SEO-optimized), module detail pages, and a dashboard modules management page.

**Architecture:** Backend adds `ModuleRegistryEntry` + `ModuleVersion` entities to ApiDbContext with PostgreSQL full-text search. Existing stub endpoints (search, info) get real implementations. Frontend adds public routes at `/modules` and `/modules/{org}/{name}` with SEO, plus an authenticated dashboard page at `/dashboard/modules`. The search API is shared between CLI and web frontend.

**Tech Stack:** .NET 9 (EF Core, MediatR, Carter), PostgreSQL (full-text search via `tsvector`), React 19, TanStack Router/Query, OpenAPI Qraft, shadcn/ui, Tailwind CSS, react-helmet-async.

**Repos:**
- Web: `/Users/milo/milodev/gits/web`

---

## Phase 1: Backend Data Model

---

### Task 1: Create ModuleRegistryEntryId and ModuleVersionId

**Files:**
- Create: `backend/Api.Abstractions/ModuleRegistryEntryId.cs`
- Create: `backend/Api.Abstractions/ModuleVersionId.cs`

**Step 1: Create the ID types**

```csharp
// backend/Api.Abstractions/ModuleRegistryEntryId.cs
namespace Api.Abstractions;

public sealed record ModuleRegistryEntryId : IdBase<ModuleRegistryEntryId>, IId<ModuleRegistryEntryId>
{
    public ModuleRegistryEntryId(Guid value) : base(value) { }
    public static ModuleRegistryEntryId Create(Guid value) => new(value);
}
```

```csharp
// backend/Api.Abstractions/ModuleVersionId.cs
namespace Api.Abstractions;

public sealed record ModuleVersionId : IdBase<ModuleVersionId>, IId<ModuleVersionId>
{
    public ModuleVersionId(Guid value) : base(value) { }
    public static ModuleVersionId Create(Guid value) => new(value);
}
```

**Step 2: Build to verify**

Run: `cd backend && dotnet build`
Expected: Success

**Step 3: Commit**

```bash
git add backend/Api.Abstractions/ModuleRegistryEntryId.cs backend/Api.Abstractions/ModuleVersionId.cs
git commit -m "feat: add ModuleRegistryEntryId and ModuleVersionId types"
```

---

### Task 2: Create ModuleRegistryEntry and ModuleVersion entities

**Files:**
- Create: `backend/Api.Data/ModuleRegistryEntry.cs`
- Create: `backend/Api.Data/ModuleVersionEntry.cs`
- Modify: `backend/Api.Data/ApiDbContext.cs`

**Step 1: Create the entities**

```csharp
// backend/Api.Data/ModuleRegistryEntry.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Auth.Abstractions;
using Database.Models;

namespace Api.Data;

public class ModuleRegistryEntry : BaseEntityWithId<ModuleRegistryEntryId>
{
    [MaxLength(255)]
    public required string Name { get; set; }

    [MaxLength(255)]
    public required string Organization { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public required Uri RepoUrl { get; set; }

    [MaxLength(50)]
    public string? LatestVersion { get; set; }

    public string[] Tags { get; set; } = [];

    public int InstallCount { get; set; }

    [Column(TypeName = "jsonb")]
    public Dictionary<string, string>? Compatibility { get; set; }

    [MaxLength(500)]
    public string? IconUrl { get; set; }

    public UserId PublishedByUserId { get; set; }

    public List<ModuleVersionEntry> Versions { get; set; } = [];
}
```

```csharp
// backend/Api.Data/ModuleVersionEntry.cs
using System.ComponentModel.DataAnnotations;
using Database.Models;

namespace Api.Data;

public class ModuleVersionEntry : BaseEntityWithId<ModuleVersionId>
{
    public ModuleRegistryEntryId ModuleRegistryEntryId { get; set; }

    [MaxLength(50)]
    public required string Version { get; set; }

    [MaxLength(40)]
    public required string CommitHash { get; set; }

    public string? ReleaseNotes { get; set; }

    public ModuleRegistryEntry ModuleRegistryEntry { get; set; } = null!;
}
```

**Step 2: Register in ApiDbContext**

Add to `backend/Api.Data/ApiDbContext.cs` DbSets:

```csharp
public DbSet<ModuleRegistryEntry> ModuleRegistryEntries => Set<ModuleRegistryEntry>();
public DbSet<ModuleVersionEntry> ModuleVersionEntries => Set<ModuleVersionEntry>();
```

Add entity configuration in `OnModelCreating`:

```csharp
modelBuilder.Entity<ModuleRegistryEntry>(entity =>
{
    entity.HasIndex(e => new { e.Organization, e.Name }).IsUnique();
    entity.HasMany(e => e.Versions)
        .WithOne(v => v.ModuleRegistryEntry)
        .HasForeignKey(v => v.ModuleRegistryEntryId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

**Step 3: Build to verify**

Run: `cd backend && dotnet build`
Expected: Success

**Step 4: Commit**

```bash
git add backend/Api.Data/ModuleRegistryEntry.cs backend/Api.Data/ModuleVersionEntry.cs backend/Api.Data/ApiDbContext.cs
git commit -m "feat: add ModuleRegistryEntry and ModuleVersionEntry entities"
```

---

### Task 3: Create EF Core migration

**Step 1: Generate migration**

Run: `cd backend && dotnet ef migrations add AddModuleRegistry --project Api.Data --startup-project Api`

**Step 2: Review generated migration file**

Verify it creates the two tables with correct columns, the unique index on (Organization, Name), and the FK relationship.

**Step 3: Build and verify**

Run: `cd backend && dotnet build`
Expected: Success

**Step 4: Commit**

```bash
git add backend/Api.Data/Migrations/
git commit -m "feat: add migration for ModuleRegistryEntry and ModuleVersionEntry tables"
```

---

### Task 4: Create ModuleRegistryRepository with search

**Files:**
- Create: `backend/Api.Data/Repositories/ModuleRegistryRepository.cs`
- Modify: `backend/Api.Data/ApiDataFeature.cs` (register repository)

**Step 1: Create the repository**

```csharp
// backend/Api.Data/Repositories/ModuleRegistryRepository.cs
using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories;

public interface IModuleRegistryRepository : IGenericRepository<ModuleRegistryEntry, ModuleRegistryEntryId, ApiDbContext>
{
    Task<(List<ModuleRegistryEntry> Entries, int TotalCount)> SearchAsync(
        string? query, string[]? tags, int limit, int offset);

    Task<ModuleRegistryEntry?> GetByOrgAndNameAsync(string organization, string name);

    Task IncrementInstallCountAsync(ModuleRegistryEntryId id);
}

public sealed class ModuleRegistryRepository(
    ApiDbContext context) : GenericRepository<ModuleRegistryEntry, ModuleRegistryEntryId, ApiDbContext>(context), IModuleRegistryRepository
{
    private readonly ApiDbContext _context = context;

    public async Task<(List<ModuleRegistryEntry> Entries, int TotalCount)> SearchAsync(
        string? query, string[]? tags, int limit, int offset)
    {
        var q = Query()
            .Include(e => e.Versions.OrderByDescending(v => v.CreatedAtUtc).Take(1))
            .Where(e => e.DeletedAtUtc == null);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lower = query.ToLower();
            q = q.Where(e =>
                e.Name.ToLower().Contains(lower) ||
                (e.Description != null && e.Description.ToLower().Contains(lower)) ||
                e.Organization.ToLower().Contains(lower));
        }

        if (tags is { Length: > 0 })
        {
            q = q.Where(e => e.Tags.Any(t => tags.Contains(t)));
        }

        var totalCount = await q.CountAsync();

        var entries = await q
            .OrderByDescending(e => e.InstallCount)
            .ThenByDescending(e => e.UpdatedAtUtc)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (entries, totalCount);
    }

    public Task<ModuleRegistryEntry?> GetByOrgAndNameAsync(string organization, string name)
    {
        return Query()
            .Include(e => e.Versions.OrderByDescending(v => v.CreatedAtUtc))
            .FirstOrDefaultAsync(e =>
                e.Organization.ToLower() == organization.ToLower() &&
                e.Name.ToLower() == name.ToLower() &&
                e.DeletedAtUtc == null);
    }

    public async Task IncrementInstallCountAsync(ModuleRegistryEntryId id)
    {
        await _context.ModuleRegistryEntries
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.InstallCount, e => e.InstallCount + 1));
    }
}
```

**Step 2: Register in ApiDataFeature**

Add to `backend/Api.Data/ApiDataFeature.cs` in `ConfigureServices`:

```csharp
services.AddScoped<IModuleRegistryRepository, ModuleRegistryRepository>();
```

**Step 3: Build to verify**

Run: `cd backend && dotnet build`
Expected: Success

**Step 4: Commit**

```bash
git add backend/Api.Data/Repositories/ModuleRegistryRepository.cs backend/Api.Data/ApiDataFeature.cs
git commit -m "feat: add ModuleRegistryRepository with search, lookup, and install tracking"
```

---

## Phase 2: Backend API Endpoints

---

### Task 5: Implement SearchModulesCommandHandler (replace stub)

**Files:**
- Modify: `modules/cli/backend/Cli/Endpoints/Modules/Search/SearchModulesCommandHandler.cs`
- Modify: `modules/cli/backend/Cli/Endpoints/Modules/Search/SearchModulesCommand.cs` (add Tags parameter)

**Step 1: Update command to accept tags**

```csharp
// modules/cli/backend/Cli/Endpoints/Modules/Search/SearchModulesCommand.cs
public sealed record SearchModulesCommand(
    string? Query,
    string[]? Tags = null,
    int Limit = 20,
    int Offset = 0) : ICommand<SearchModulesResponse>;
```

**Step 2: Replace handler with real implementation**

```csharp
// modules/cli/backend/Cli/Endpoints/Modules/Search/SearchModulesCommandHandler.cs
using Api.Data.Repositories;
using Core.MediatR;

namespace Cli.Endpoints.Modules.Search;

public sealed class SearchModulesCommandHandler(
    IModuleRegistryRepository repository) :
    ICommandHandler<SearchModulesCommand, SearchModulesResponse>
{
    public async Task<SearchModulesResponse> Handle(
        SearchModulesCommand command,
        CancellationToken cancellationToken = default)
    {
        var (entries, totalCount) = await repository.SearchAsync(
            command.Query, command.Tags, command.Limit, command.Offset);

        var modules = entries.Select(e => new SearchModuleItem(
            e.Name,
            e.Description ?? "",
            e.LatestVersion ?? "0.0.0",
            e.RepoUrl.ToString(),
            e.InstallCount,
            e.Organization,
            e.Tags,
            e.IconUrl
        )).ToArray();

        return new SearchModulesResponse(modules, totalCount);
    }
}
```

**Step 3: Update response to include new fields**

```csharp
// modules/cli/backend/Cli/Endpoints/Modules/Search/SearchModulesResponse.cs
public sealed record SearchModulesResponse(
    SearchModuleItem[] Modules,
    int Total);

public sealed record SearchModuleItem(
    string Name,
    string Description,
    string LatestVersion,
    string RepoUrl,
    int InstallCount,
    string Organization,
    string[] Tags,
    string? IconUrl);
```

**Step 4: Update route to accept Tags parameter**

Check `SearchModulesRoute.cs` — if using `[AsParameters]`, the new `Tags` field on the command will be picked up automatically from query string `?tags=billing&tags=payments`.

**Step 5: Build and test**

Run: `cd backend && dotnet build && dotnet test`
Expected: Success

**Step 6: Commit**

```bash
git add modules/cli/backend/Cli/Endpoints/Modules/Search/
git commit -m "feat: implement module search with real DB queries"
```

---

### Task 6: Implement GetModuleInfoCommandHandler (replace stub)

**Files:**
- Modify: `modules/cli/backend/Cli/Endpoints/Modules/Info/GetModuleInfoCommandHandler.cs`
- Modify: `modules/cli/backend/Cli/Endpoints/Modules/Info/GetModuleInfoResponse.cs`

**Step 1: Update response to include version history**

```csharp
// modules/cli/backend/Cli/Endpoints/Modules/Info/GetModuleInfoResponse.cs
public sealed record GetModuleInfoResponse(
    bool Found,
    string? Name,
    string? Organization,
    string? Description,
    string? LatestVersion,
    string? RepoUrl,
    string[]? Tags,
    ModuleVersionDto[]? Versions,
    int InstallCount,
    string? IconUrl,
    Dictionary<string, string>? Compatibility,
    DateTime? PublishedAt);

public sealed record ModuleVersionDto(
    string Version,
    string CommitHash,
    string? ReleaseNotes,
    DateTime CreatedAt);
```

**Step 2: Replace handler**

```csharp
// modules/cli/backend/Cli/Endpoints/Modules/Info/GetModuleInfoCommandHandler.cs
using Api.Data.Repositories;
using Core.MediatR;

namespace Cli.Endpoints.Modules.Info;

public sealed class GetModuleInfoCommandHandler(
    IModuleRegistryRepository repository) :
    ICommandHandler<GetModuleInfoCommand, GetModuleInfoResponse>
{
    public async Task<GetModuleInfoResponse> Handle(
        GetModuleInfoCommand command,
        CancellationToken cancellationToken = default)
    {
        var entry = await repository.GetByOrgAndNameAsync(command.Organization, command.ModuleName);

        if (entry is null)
        {
            return new GetModuleInfoResponse(
                Found: false, null, null, null, null, null, null, null, 0, null, null, null);
        }

        var versions = entry.Versions.Select(v => new ModuleVersionDto(
            v.Version, v.CommitHash, v.ReleaseNotes, v.CreatedAtUtc
        )).ToArray();

        return new GetModuleInfoResponse(
            Found: true,
            entry.Name,
            entry.Organization,
            entry.Description,
            entry.LatestVersion,
            entry.RepoUrl.ToString(),
            entry.Tags,
            versions,
            entry.InstallCount,
            entry.IconUrl,
            entry.Compatibility,
            entry.CreatedAtUtc);
    }
}
```

**Step 3: Build and test**

Run: `cd backend && dotnet build && dotnet test`
Expected: Success

**Step 4: Commit**

```bash
git add modules/cli/backend/Cli/Endpoints/Modules/Info/
git commit -m "feat: implement module info with real DB queries and version history"
```

---

### Task 7: Add module release and install-tracking endpoints

**Files:**
- Modify: `modules/cli/backend/Cli/Endpoints/Modules/Release/ReleaseModuleCommandHandler.cs`
- Create: `modules/cli/backend/Cli/Endpoints/Modules/TrackInstall/TrackInstallCommand.cs`
- Create: `modules/cli/backend/Cli/Endpoints/Modules/TrackInstall/TrackInstallCommandHandler.cs`
- Create: `modules/cli/backend/Cli/Endpoints/Modules/TrackInstall/TrackInstallRoute.cs`
- Modify: `modules/cli/backend/Cli/Endpoints/Modules/ModulesGroup.cs` (add route)

**Step 1: Implement ReleaseModuleCommandHandler**

Replace the stub handler to upsert a `ModuleRegistryEntry` and add a `ModuleVersionEntry`:

```csharp
// modules/cli/backend/Cli/Endpoints/Modules/Release/ReleaseModuleCommandHandler.cs
using Api.Data;
using Api.Data.Repositories;
using Auth.Services;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cli.Endpoints.Modules.Release;

public sealed class ReleaseModuleCommandHandler(
    ApiDbContext apiDb,
    ICurrentUserAccessor currentUserAccessor,
    IModuleRegistryRepository registryRepository) :
    ICommandHandler<ReleaseModuleCommand, ReleaseModuleResponse>
{
    public async Task<ReleaseModuleResponse> Handle(
        ReleaseModuleCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserAccessor.CurrentUserId!;

        // Find or create registry entry
        var entry = await registryRepository.GetByOrgAndNameAsync(
            command.Organization, command.ModuleName);

        if (entry is null)
        {
            entry = new ModuleRegistryEntry
            {
                Name = command.ModuleName,
                Organization = command.Organization,
                Description = command.Description,
                RepoUrl = new Uri(command.RepoUrl),
                LatestVersion = command.Version,
                Tags = command.Tags ?? [],
                Compatibility = command.Compatibility,
                PublishedByUserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
            };
            apiDb.ModuleRegistryEntries.Add(entry);
        }
        else
        {
            entry.LatestVersion = command.Version;
            entry.UpdatedAtUtc = DateTime.UtcNow;
            if (command.Description is not null) entry.Description = command.Description;
            if (command.Tags is not null) entry.Tags = command.Tags;
            if (command.Compatibility is not null) entry.Compatibility = command.Compatibility;
            if (command.RepoUrl is not null) entry.RepoUrl = new Uri(command.RepoUrl);
        }

        // Add version entry
        var versionEntry = new ModuleVersionEntry
        {
            ModuleRegistryEntryId = entry.Id,
            Version = command.Version,
            CommitHash = command.CommitHash ?? "unknown",
            ReleaseNotes = command.ReleaseNotes,
            CreatedAtUtc = DateTime.UtcNow,
        };
        apiDb.ModuleVersionEntries.Add(versionEntry);

        await apiDb.SaveChangesAsync(cancellationToken);

        return new ReleaseModuleResponse(true, $"v{command.Version}", null);
    }
}
```

**Step 2: Update ReleaseModuleCommand to include new fields**

```csharp
// modules/cli/backend/Cli/Endpoints/Modules/Release/ReleaseModuleCommand.cs
public sealed record ReleaseModuleCommand(
    string Organization,
    string ModuleName,
    string Version,
    string? RepoUrl,
    string? Description,
    string? ReleaseNotes,
    string? CommitHash,
    string[]? Tags,
    Dictionary<string, string>? Compatibility) : ICommand<ReleaseModuleResponse>;
```

**Step 3: Create TrackInstall endpoint**

```csharp
// modules/cli/backend/Cli/Endpoints/Modules/TrackInstall/TrackInstallCommand.cs
using Core.MediatR;
namespace Cli.Endpoints.Modules.TrackInstall;

public sealed record TrackInstallCommand(
    string Organization,
    string ModuleName) : ICommand;
```

```csharp
// modules/cli/backend/Cli/Endpoints/Modules/TrackInstall/TrackInstallCommandHandler.cs
using Api.Data.Repositories;
using Core.MediatR;
namespace Cli.Endpoints.Modules.TrackInstall;

public sealed class TrackInstallCommandHandler(
    IModuleRegistryRepository repository) : ICommandHandler<TrackInstallCommand>
{
    public async Task Handle(TrackInstallCommand command, CancellationToken cancellationToken = default)
    {
        var entry = await repository.GetByOrgAndNameAsync(command.Organization, command.ModuleName);
        if (entry is not null)
            await repository.IncrementInstallCountAsync(entry.Id);
    }
}
```

```csharp
// modules/cli/backend/Cli/Endpoints/Modules/TrackInstall/TrackInstallRoute.cs
using MediatR;
namespace Cli.Endpoints.Modules.TrackInstall;

public static class TrackInstallRoute
{
    public const string Route = "{organization}/{moduleName}/track-install";

    public static RouteHandlerBuilder AddTrackInstallRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static async Task<IResult> ExecuteAsync(
        string organization,
        string moduleName,
        IMediator mediator)
    {
        await mediator.Send(new TrackInstallCommand(organization, moduleName));
        return Results.Ok();
    }
}
```

**Step 4: Add route to ModulesGroup**

In `modules/cli/backend/Cli/Endpoints/Modules/ModulesGroup.cs`, add:

```csharp
app.AddTrackInstallRoute();
```

**Step 5: Build and test**

Run: `cd backend && dotnet build && dotnet test`
Expected: Success

**Step 6: Commit**

```bash
git add modules/cli/backend/Cli/Endpoints/Modules/
git commit -m "feat: implement module release and install tracking endpoints"
```

---

### Task 7b: Seed sample marketplace modules for dev

**Files:**
- Modify: `backend/Api/Features/DevSeed/DevSeeder.cs`

**Step 1: Add marketplace seed data**

Add a new method `SeedMarketplaceAsync` to `DevSeeder` that creates 6-8 sample `ModuleRegistryEntry` records with versions. Call it from `EnsureInfrastructureAsync` or `SeedForUser`.

Sample modules to seed:
- `compozerr/stripe` — Stripe billing, subscriptions, and invoices (tags: billing, payments)
- `compozerr/auth` — Authentication with GitHub OAuth, API tokens (tags: auth, security)
- `compozerr/database` — PostgreSQL with Entity Framework Core (tags: database, orm)
- `compozerr/github` — GitHub App integration, webhooks (tags: github, git, webhooks)
- `compozerr/storage` — File storage with S3-compatible backends (tags: storage, files)
- `compozerr/mail` — Email sending abstraction (tags: email, notifications)
- `compozerr-lib/jobs` — Background job processing with Hangfire (tags: jobs, queue, background)
- `compozerr-lib/terminal` — Web-based terminal for remote access (tags: terminal, ssh)

Each should have 2-3 version entries and realistic install counts.

**Step 2: Build and test**

Run: `cd backend && dotnet build && dotnet test`
Expected: Success

**Step 3: Commit**

```bash
git add backend/Api/Features/DevSeed/DevSeeder.cs
git commit -m "feat: seed sample marketplace modules for dev environment"
```

---

## Phase 3: Frontend — Regenerate API Client & Setup

---

### Task 8: Regenerate OpenAPI client and add public modules routes

**Step 1: Start backend**

Run the backend so the OpenAPI spec is available at `/swagger/v1/swagger.json`.

**Step 2: Regenerate API client**

Run:
```bash
cd frontend && npx openapi-qraft --plugin tanstack-query-react --plugin openapi-typescript http://localhost:5000/swagger/v1/swagger.json --clean -o src/generated --export-type --properties-required-by-default --default-non-nullable
```

**Step 3: Verify new endpoints are generated**

Check that `frontend/src/generated/` contains types for the search, info, release, and track-install endpoints.

**Step 4: Commit**

```bash
git add frontend/src/generated/
git commit -m "chore: regenerate frontend API client with marketplace endpoints"
```

---

## Phase 4: Frontend — Public Marketplace Pages

> **For Claude:** REQUIRED SUB-SKILL: Use frontend-design:frontend-design for Tasks 9-13 to create distinctive, production-grade UI with high design quality.

---

### Task 9: Create public marketplace browse page

**Files:**
- Create: `frontend/src/routes/modules/index.tsx`
- Modify: `frontend/src/components/navbar.tsx` (add Marketplace nav link)

**Design Requirements:**
- Hero section with gradient background, search bar front and center
- Tag/category filter pills below search
- Module cards in responsive grid (1/2/3 columns)
- Each card: module icon/avatar, name, org, description (truncated), tags as badges, install count, latest version
- Infinite scroll or load more
- Smooth animations (stagger cards)
- Dark theme matching existing site
- SEO: title "Module Marketplace | Compozerr", description, structured data (ItemList)

**Data fetching:**
```tsx
const { data } = api.v1.getModulesSearch.useQuery({
  query: { query: searchTerm, tags: selectedTags, limit: 20, offset: 0 }
});
```

**Step 1: Create the route file**

Create `frontend/src/routes/modules/index.tsx` with:
- SEO component with marketplace-specific meta
- Search input with debounce
- Tag filter bar
- Module card grid using generated API types
- Loading skeletons
- Empty state for no results

**Step 2: Add Marketplace to navbar**

In `frontend/src/components/navbar.tsx`, add "Marketplace" to the nav items array between existing items.

**Step 3: Verify route auto-generated**

The TanStack Router file-based routing should auto-discover the new route.

**Step 4: Test locally**

Run: `cd frontend && npm run dev`
Navigate to `http://localhost:3001/modules` — verify the page loads.

**Step 5: Commit**

```bash
git add frontend/src/routes/modules/ frontend/src/components/navbar.tsx
git commit -m "feat: add public marketplace browse page with search and filtering"
```

---

### Task 10: Create module detail page

**Files:**
- Create: `frontend/src/routes/modules/$org.$name.tsx`

**Design Requirements:**
- Breadcrumb: Marketplace > org/name
- Module header: icon, name, org, description, install count badge, latest version badge
- Install command snippet (copy-to-clipboard): `compozerr module add org/name`
- Tags as clickable badges (link back to marketplace with tag filter)
- Compatibility badges (dotnet version, node version)
- Version history timeline/table with release notes (collapsible)
- Dependencies section if module has them
- "View on GitHub" button linking to RepoUrl
- SEO: dynamic title "{name} - Compozerr Marketplace", description from module, og:image

**Data fetching:**
```tsx
const { org, name } = Route.useParams();
const { data } = api.v1.getModulesInfoOrganizationModuleName.useQuery({
  path: { organization: org, moduleName: name }
});
```

**Step 1: Create the route file**

Create `frontend/src/routes/modules/$org.$name.tsx`. Use dot-notation in filename so TanStack Router captures both `org` and `name` as params from `/modules/compozerr/stripe` → `{ org: "compozerr", name: "stripe" }`.

Note: Check TanStack Router docs for the exact param syntax. May need `$org_.$name.tsx` or a layout with nested route.

**Step 2: Test locally**

Navigate to `http://localhost:3001/modules/compozerr/stripe`

**Step 3: Commit**

```bash
git add frontend/src/routes/modules/
git commit -m "feat: add module detail page with version history and install command"
```

---

### Task 11: Add SEO structured data for marketplace

**Files:**
- Modify: `frontend/src/routes/modules/index.tsx` (add ItemList structured data)
- Modify: `frontend/src/routes/modules/$org.$name.tsx` (add SoftwareApplication structured data)
- Modify: `frontend/vite.config.ts` (add `/modules` to sitemap)

**Step 1: Add structured data to browse page**

```tsx
<SEO
  title="Module Marketplace"
  description="Browse and install modules for your Compozerr project. Authentication, billing, databases, and more."
  canonical="/modules"
  structuredData={{
    "@context": "https://schema.org",
    "@type": "ItemList",
    "name": "Compozerr Module Marketplace",
    "itemListElement": modules?.map((m, i) => ({
      "@type": "ListItem",
      "position": i + 1,
      "item": {
        "@type": "SoftwareApplication",
        "name": m.name,
        "description": m.description,
        "url": `https://compozerr.com/modules/${m.organization}/${m.name}`,
      }
    }))
  }}
/>
```

**Step 2: Add structured data to detail page**

```tsx
<SEO
  title={`${data.name} by ${data.organization}`}
  description={data.description || `${data.name} module for Compozerr`}
  canonical={`/modules/${data.organization}/${data.name}`}
  structuredData={{
    "@context": "https://schema.org",
    "@type": "SoftwareApplication",
    "name": data.name,
    "description": data.description,
    "applicationCategory": "DeveloperApplication",
    "operatingSystem": "Linux",
    "softwareVersion": data.latestVersion,
  }}
/>
```

**Step 3: Add to sitemap**

In vite config sitemap generation, add `/modules` with priority 0.8.

**Step 4: Commit**

```bash
git add frontend/src/routes/modules/ frontend/vite.config.ts
git commit -m "feat: add SEO structured data and sitemap entries for marketplace"
```

---

## Phase 5: Frontend — Dashboard Modules Page

---

### Task 12: Add Modules page to dashboard

**Files:**
- Create: `frontend/src/routes/_auth/_dashboard/modules.tsx`
- Modify: `frontend/src/components/app-sidebar.tsx` (add Modules nav item)
- Modify: `frontend/src/components/mobile-bottom-nav.tsx` (add Modules nav item)

**Design Requirements:**
- Match existing dashboard design: dark background, card-based layout
- Header with title "Modules" and action buttons
- Two sections:
  1. "Your Published Modules" — modules the user has published to the marketplace
  2. "Browse Marketplace" — compact search/browse inline, or link to /modules
- Each module card: name, org, version, install count, status badge, actions (view, release new version)
- Quick link to public marketplace detail page
- Empty state with CTA to publish first module

**Data fetching:**
```tsx
// User's published modules (filter by current user)
const { data } = api.v1.getModulesSearch.useQuery({
  query: { query: "", limit: 50 }
});
// TODO: Once backend supports filtering by publisher, use that
```

**Step 1: Create the route file**

Create `frontend/src/routes/_auth/_dashboard/modules.tsx` matching existing dashboard page patterns (breadcrumb portal, card layout, loading states).

**Step 2: Add to sidebar navigation**

In `frontend/src/components/app-sidebar.tsx`, add a "Modules" item to the navigation section (below Projects, above/below Settings). Use `Package` icon from lucide-react.

```tsx
{ title: "Modules", url: "/modules", icon: Package }
```

Wait — this URL should be the dashboard route `/dashboard/modules` (or however the auth routes resolve). Check existing nav items for the correct URL pattern. The auth layout uses `_auth/_dashboard` prefix, so the route is mounted at the dashboard level.

**Step 3: Add to mobile nav**

In `frontend/src/components/mobile-bottom-nav.tsx`, add Modules between Projects and Settings.

**Step 4: Test locally**

Navigate to `http://localhost:3001/modules` in the authenticated dashboard.

**Step 5: Commit**

```bash
git add frontend/src/routes/_auth/_dashboard/modules.tsx frontend/src/components/app-sidebar.tsx frontend/src/components/mobile-bottom-nav.tsx
git commit -m "feat: add dashboard modules page with sidebar navigation"
```

---

### Task 13: Connect dashboard modules to marketplace

**Files:**
- Modify: `frontend/src/routes/_auth/_dashboard/modules.tsx`

**Step 1: Add "View in Marketplace" links**

Each published module card should link to `/modules/{org}/{name}` (public detail page).

**Step 2: Add "Browse Marketplace" section**

A compact section at the bottom or a prominent CTA that links to `/modules` (public marketplace).

**Step 3: Commit**

```bash
git add frontend/src/routes/_auth/_dashboard/modules.tsx
git commit -m "feat: connect dashboard modules to public marketplace pages"
```

---

## Phase 6: Integration Testing & Polish

---

### Task 14: End-to-end test

**Step 1: Start full dev environment**

Run: `compozerrbeta dev`

**Step 2: Seed dev data**

Login and trigger dev seed to populate marketplace entries.

**Step 3: Test public marketplace**

- Navigate to `/modules` — verify search works, cards render
- Navigate to `/modules/compozerr/stripe` — verify detail page renders
- Test search with various queries
- Test tag filtering
- Verify SEO tags in page source

**Step 4: Test dashboard**

- Navigate to dashboard `/modules` — verify page renders
- Verify sidebar navigation works
- Test links between dashboard and public pages

**Step 5: Test CLI integration**

Run: `compozerrbeta module search stripe`
Verify same results as web marketplace.

---

### Task 15: Final cleanup and commit

**Step 1: Run all tests**

Run: `cd backend && dotnet test`
Run: `cd frontend && npm run typecheck`

**Step 2: Fix any issues**

**Step 3: Final commit**

```bash
git add -A
git commit -m "feat: module marketplace - complete implementation"
```

---

## Implementation Order Summary

| # | Task | Area | Depends On |
|---|------|------|------------|
| 1 | ID types | Backend | — |
| 2 | Entity classes | Backend | 1 |
| 3 | EF migration | Backend | 2 |
| 4 | Repository | Backend | 2 |
| 5 | Search handler | Backend | 4 |
| 6 | Info handler | Backend | 4 |
| 7 | Release + track-install | Backend | 4 |
| 7b | Seed data | Backend | 2 |
| 8 | Regenerate API client | Frontend | 5, 6, 7 |
| 9 | Marketplace browse page | Frontend | 8 |
| 10 | Module detail page | Frontend | 8 |
| 11 | SEO structured data | Frontend | 9, 10 |
| 12 | Dashboard modules page | Frontend | 8 |
| 13 | Connect dashboard↔marketplace | Frontend | 9, 10, 12 |
| 14 | E2E testing | Integration | All |
| 15 | Cleanup | Polish | 14 |

Tasks 1-4 are sequential (entity→migration→repo). Tasks 5-7b can be parallelized after 4. Frontend tasks 9-12 can be partially parallelized after 8.
