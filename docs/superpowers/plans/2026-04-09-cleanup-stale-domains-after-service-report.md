# Cleanup Stale Internal Domains After Service Report

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** After the service reporter runs, soft-delete orphaned internal domains and filter stale services from the UI — making the service reporter the single source of truth for what's running.

**Architecture:** Extend the existing domain cleanup in both `ReportEnvironmentServicesCommandHandler` and `ReportServicesCommandHandler` to soft-delete **all** internal domains for unreported services (not just `"Frontend"`/`"Backend"`). Fix the services GET endpoints to exclude soft-deleted records. External/custom domains (like `blog.mappso.com`) are never touched by this cleanup.

**Tech Stack:** .NET 9, EF Core, MediatR, xUnit

---

## Context

### The bug that caused the outage

The service report handlers have a domain cleanup block:

```csharp
// ReportEnvironmentServicesCommandHandler.cs:127-132
var systemDomainsToRemove = project.Domains?
    .Where(d => d.DeletedAtUtc == null
             && d.ProjectEnvironmentId == command.EnvironmentId
             && SystemServices.Contains(d.ServiceName)   // ← only "Frontend", "Backend"
             && !reportedPorts.Contains(d.Port))
    .ToList();
```

`SystemServices` is `["Frontend", "Backend"]`. Services with custom names like `"Web"` or `"App"` are excluded from cleanup. When a deploy reports only `App:3000`, the old `Web:1234` internal domains (and attached external domains like `blog.mappso.com`) linger. The `eca774d` hosting commit then wipes the Traefik config for the VM IP and rebuilds it from the API — but the stale internal domains still appear in the response, causing routing conflicts.

Separately: the `GetProjectServicesCommandHandler` and `GetEnvironmentServicesCommandHandler` query `context.ProjectServices` directly (bypassing the repository's `DeletedAtUtc` filter), so soft-deleted services still appear in the UI.

### Key invariant

**External domains (custom domains) must NEVER be auto-deleted by this cleanup.** Only `InternalDomain` records are touched. External domains are manually managed by users and represent DNS configuration that's expensive to recreate (verification, health checks, DNS propagation).

---

## File Structure

| File | Action | Responsibility |
|------|--------|----------------|
| `backend/Api.Hosting/Endpoints/Projects/Services/ReportEnvironmentServicesCommandHandler.cs` | Modify | Extend domain cleanup to all internal domains |
| `backend/Api.Hosting/Endpoints/Projects/Services/ReportServicesCommandHandler.cs` | Modify | Same cleanup extension for legacy handler |
| `backend/Api/Endpoints/Projects/Services/Get/GetProjectServicesCommandHandler.cs` | Modify | Filter soft-deleted services |
| `backend/Api/Endpoints/Projects/Environments/Services/GetEnvironmentServicesCommandHandler.cs` | Modify | Filter soft-deleted services |
| `backend/Api.Tests/Hosting/ReportEnvironmentServicesCommandHandlerTests.cs` | Create | Tests for domain cleanup logic |

---

### Task 1: Filter soft-deleted services from GET endpoints

The simplest fix. Both handlers query `context.ProjectServices` without filtering `DeletedAtUtc`.

**Files:**
- Modify: `backend/Api/Endpoints/Projects/Services/Get/GetProjectServicesCommandHandler.cs:23-27`
- Modify: `backend/Api/Endpoints/Projects/Environments/Services/GetEnvironmentServicesCommandHandler.cs:23-29`

- [ ] **Step 1: Add DeletedAtUtc filter to GetProjectServicesCommandHandler**

In `GetProjectServicesCommandHandler.cs`, add `.Where(s => s.DeletedAtUtc == null)` to the services query:

```csharp
var services = await context.ProjectServices
    .Where(s => s.ProjectId == command.ProjectId && s.DeletedAtUtc == null)
    .OrderByDescending(s => s.IsSystem)
    .ThenBy(s => s.Name)
    .Select(s => new { s.Name, s.Port, s.Protocol, s.IsSystem })
    .ToListAsync(cancellationToken);
```

- [ ] **Step 2: Add DeletedAtUtc filter to GetEnvironmentServicesCommandHandler**

In `GetEnvironmentServicesCommandHandler.cs`, same change:

```csharp
var services = await context.ProjectServices
    .Where(s => s.ProjectId == command.ProjectId
             && s.ProjectEnvironmentId == command.EnvironmentId
             && s.DeletedAtUtc == null)
    .OrderByDescending(s => s.IsSystem)
    .ThenBy(s => s.Name)
    .Select(s => new { s.Name, s.Port, s.Protocol, s.IsSystem })
    .ToListAsync(cancellationToken);
```

- [ ] **Step 3: Build and verify**

Run: `cd backend && dotnet build`
Expected: Build succeeds with no errors.

- [ ] **Step 4: Commit**

```bash
git add backend/Api/Endpoints/Projects/Services/Get/GetProjectServicesCommandHandler.cs \
       backend/Api/Endpoints/Projects/Environments/Services/GetEnvironmentServicesCommandHandler.cs
git commit -m "fix: filter soft-deleted services from GET endpoints

The project and environment services handlers queried context.ProjectServices
directly, bypassing the repository's DeletedAtUtc filter. Soft-deleted services
(cleaned up by service reporting) still appeared in the UI."
```

---

### Task 2: Extend domain cleanup to all internal domains in ReportEnvironmentServicesCommandHandler

Replace the `SystemServices.Contains(d.ServiceName)` filter with a type check on `InternalDomain` and match against reported service names instead of ports.

**Files:**
- Modify: `backend/Api.Hosting/Endpoints/Projects/Services/ReportEnvironmentServicesCommandHandler.cs:115-138`

- [ ] **Step 1: Replace the systemDomainsToRemove block**

In `ReportEnvironmentServicesCommandHandler.cs`, replace the existing domain cleanup block (lines 115-138) with:

```csharp
// Soft-delete internal domains in the reporting env whose service is no
// longer in the report. This replaces the old SystemServices-only check
// that missed custom service names like "Web" or "App", leaving orphaned
// internal domains that caused routing conflicts in Traefik.
//
// Only InternalDomain records are touched — ExternalDomain (custom domains
// like blog.mappso.com) are never auto-deleted; they're user-managed.
//
// Defense-in-depth: re-apply soft-delete and env-scope filters explicitly
// because EF Core relationship fix-up can re-attach domains from other
// queries in the same scoped DbContext (see earlier comment).
var reportedServiceNames = command.Services.Select(s => s.Name).ToHashSet();
var staleInternalDomains = project.Domains?
    .OfType<InternalDomain>()
    .Where(d => d.DeletedAtUtc == null
             && d.ProjectEnvironmentId == command.EnvironmentId
             && !reportedServiceNames.Contains(d.ServiceName))
    .ToList();

if (staleInternalDomains != null && staleInternalDomains.Count > 0)
{
    Log.ForContext("projectId", command.ProjectId)
       .ForContext("environmentId", command.EnvironmentId)
       .ForContext("staleDomainServices", staleInternalDomains.Select(d => $"{d.ServiceName}:{d.Port}").ToArray())
       .Information("Removing {Count} stale internal domains from env-scoped report", staleInternalDomains.Count);

    await staleInternalDomains.Select(x => x.Id)
                              .ApplyAsync(domainRepository.DeleteAsync, cancellationToken);
}
```

Also add `using Serilog;` at the top if not already present (it is — used by the stale system services block below).

- [ ] **Step 2: Build and verify**

Run: `cd backend && dotnet build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add backend/Api.Hosting/Endpoints/Projects/Services/ReportEnvironmentServicesCommandHandler.cs
git commit -m "fix: clean up all stale internal domains after service report

The domain cleanup only targeted services named Frontend or Backend. Custom
service names like Web or App were missed, leaving orphaned internal domains.
When the Traefik config was rebuilt from the API response, these stale domains
caused routing conflicts.

Now soft-deletes all InternalDomain records whose ServiceName is not in the
current service report. ExternalDomain records (custom domains) are never
touched — they are user-managed."
```

---

### Task 3: Apply same fix to legacy ReportServicesCommandHandler

Mirror the change from Task 2 in the non-env-scoped handler for backwards compatibility.

**Files:**
- Modify: `backend/Api.Hosting/Endpoints/Projects/Services/ReportServicesCommandHandler.cs:122-142`

- [ ] **Step 1: Replace the systemDomainsToRemove block**

In `ReportServicesCommandHandler.cs`, replace the domain cleanup block (lines 122-142) with the same pattern, using `productionEnvId` instead of `command.EnvironmentId`:

```csharp
// Soft-delete internal domains in prod env whose service is no longer in
// the report. See ReportEnvironmentServicesCommandHandler for full
// rationale — this is the legacy endpoint equivalent.
var reportedServiceNames = command.Services.Select(s => s.Name).ToHashSet();
var staleInternalDomains = project.Domains?
    .OfType<InternalDomain>()
    .Where(d => d.DeletedAtUtc == null
             && d.ProjectEnvironmentId == productionEnvId
             && !reportedServiceNames.Contains(d.ServiceName))
    .ToList();

if (staleInternalDomains != null && staleInternalDomains.Count > 0)
{
    Log.ForContext("projectId", command.ProjectId)
       .ForContext("productionEnvId", productionEnvId)
       .ForContext("staleDomainServices", staleInternalDomains.Select(d => $"{d.ServiceName}:{d.Port}").ToArray())
       .Information("Removing {Count} stale internal domains from legacy prod-env report", staleInternalDomains.Count);

    await staleInternalDomains.Select(x => x.Id)
                              .ApplyAsync(domainRepository.DeleteAsync, cancellationToken);
}
```

Note: `reportedServiceNames` is already defined later in this method (line 148 in the current code) for the stale system services cleanup. Since that variable is now used earlier, move/merge the declaration. The stale system services block below already uses `reportedServiceNames`, so just remove its duplicate declaration.

- [ ] **Step 2: Verify no duplicate variable declaration**

The stale system services block (around line 148) also declares `var reportedServiceNames`. Since we now declare it earlier, remove the second `var` and reuse the existing variable. The stale system services block becomes:

```csharp
// Bug 9: Clean up stale IsSystem=true services in prod env...
// reportedServiceNames is already declared above
var staleSystemServices = existingServices.Values
    .Where(s => s.IsSystem && !reportedServiceNames.Contains(s.Name))
    .ToList();
```

- [ ] **Step 3: Build and verify**

Run: `cd backend && dotnet build`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add backend/Api.Hosting/Endpoints/Projects/Services/ReportServicesCommandHandler.cs
git commit -m "fix: mirror stale internal domain cleanup in legacy service report handler"
```

---

### Task 4: Add tests for stale domain cleanup

No tests exist for the service report handlers. Add focused tests for the domain cleanup behavior.

**Files:**
- Create: `backend/Api.Tests/Hosting/ReportEnvironmentServicesCommandHandlerTests.cs`

- [ ] **Step 1: Check test project structure and available helpers**

Run: `ls backend/Api.Tests/` to see existing test conventions, then check for any test helpers, fixtures, or base classes:

```bash
ls backend/Api.Tests/
find backend/Api.Tests -name "*.cs" -maxdepth 2 | head -20
```

Adapt the test structure to match existing conventions in the project.

- [ ] **Step 2: Write test — internal domains for unreported services are soft-deleted**

Create `backend/Api.Tests/Hosting/ReportEnvironmentServicesCommandHandlerTests.cs`. The exact test setup depends on the project's test patterns (integration tests with real DB, or unit tests with mocks). Follow whatever pattern existing tests use.

The test should verify:
- Given: A project with `Web:1234` internal domain and `blog.mappso.com` external domain, both for the same environment
- When: Service report comes in with only `[App:3000]`
- Then: The `Web:1234` internal domain is soft-deleted
- And: The `blog.mappso.com` external domain is NOT deleted

- [ ] **Step 3: Write test — external domains are never deleted by cleanup**

Verify that even when NO services are reported for a service name, the external domain attached to that service name survives. This is the critical invariant.

- [ ] **Step 4: Write test — domains for reported services are preserved**

Given: Internal domain for `App:3000`
When: Service report includes `App:3000`
Then: Domain is NOT deleted.

- [ ] **Step 5: Run tests**

Run: `cd backend && dotnet test --filter "ReportEnvironmentServices"`
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add backend/Api.Tests/Hosting/ReportEnvironmentServicesCommandHandlerTests.cs
git commit -m "test: add tests for stale internal domain cleanup after service report"
```

---

## Summary of changes

| What | Before | After |
|------|--------|-------|
| Domain cleanup scope | Only `"Frontend"`, `"Backend"` service names | All `InternalDomain` records for unreported services |
| External domains | Could be orphaned when service disappears | Never touched by auto-cleanup |
| Soft-deleted services in UI | Visible (no `DeletedAtUtc` filter) | Hidden |
| Test coverage | None | Core cleanup logic covered |
