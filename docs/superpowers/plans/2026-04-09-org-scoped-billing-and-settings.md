# Organization-Scoped Billing & Settings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate all billing (Stripe) and GitHub integration settings from user-scoped to organization-scoped, add a Members page to the main sidebar navigation.

**Architecture:** The `CurrentStripeCustomerIdAccessor` resolves the current organization from the `X-Organization-Id` header (via `IOrganizationContextAccessor`) instead of the user from JWT claims. GitHub settings get an `OrganizationId` column replacing `UserId`. A data migration backfills existing records by mapping each user to their personal org. Frontend adds a `/members` route with a sidebar nav item.

**Tech Stack:** .NET 9 / EF Core / PostgreSQL / Stripe API / React / TanStack Router / OpenAPI Qraft

**Spec:** `docs/superpowers/specs/2026-04-09-org-scoped-billing-and-settings-design.md`

---

## File Structure

### Files to Modify

| File | Change |
|------|--------|
| `modules/stripe/backend/Stripe/Services/ICurrentStripeCustomerIdAccessor.cs` | Remove `GetOrCreateStripeCustomerId(string)` overload, update `GetOrCreateStripeCustomerId()` docs |
| `modules/stripe/backend/Stripe/Services/CurrentStripeCustomerIdAccessor.cs` | Route parameterless method through org context, remove user-based methods |
| `modules/stripe/backend/Stripe/Services/SubscriptionService.cs` | Replace `GetSubscriptionsForUserAsync(string userId)` with `GetSubscriptionsForOrganizationAsync(OrganizationId)` |
| `modules/stripe/backend/Stripe/Data/Models/StripeCustomer.cs` | Update XML comment on `InternalId` |
| `backend/Api/EventHandlers/Stripe/PaymentMethodMissingActions/TerminateAllProjects_PaymentMethodMissingAction.cs` | Use org ID from event instead of user ID |
| `backend/Api/Endpoints/Projects/Project/Delete/DeleteProjectCommandHandler.cs` | Use org ID for subscription lookup in skipOwnerCheck path |
| `modules/github/backend/Github/Models/GithubUserSettings.cs` | Add `OrganizationId`, keep `UserId` temporarily |
| `modules/github/backend/Github/Repositories/GithubUserSettingsRepository.cs` | Add org-based query methods |
| `modules/github/backend/Github/Endpoints/SetDefaultInstallationRoute.cs` | Use org context instead of user |
| `modules/github/backend/Github/Endpoints/GetInstallationsRoute.cs` | Use org context instead of user |
| `modules/github/backend/Github/Endpoints/SetDefaultInstallationId/SetDefaultInstallationIdCommand.cs` | Replace `UserId` with `OrganizationId` |
| `modules/github/backend/Github/Endpoints/SetDefaultInstallationId/SetDefaultInstallationIdCommandHandler.cs` | Use org-based repository method |
| `modules/github/backend/Github/Features/CreateDefaultSettings_FirstUserLoginEventHandler.cs` | Look up personal org, create settings with org ID |
| `modules/github/backend/Github/Data/GithubDbContext.cs` | Update entity registration after rename |
| `frontend/src/components/app-sidebar.tsx` | Add Members nav item |
| `frontend/src/components/mobile-org-picker.tsx` | Verify no members-related links need updating |

### Files to Create

| File | Purpose |
|------|---------|
| `modules/github/backend/Github/Migrations/<timestamp>_AddOrganizationIdToSettings.cs` | EF migration: add OrganizationId, backfill from personal orgs, drop UserId |
| `modules/stripe/backend/Stripe/Migrations/<timestamp>_BackfillOrgInternalIds.cs` | EF migration: backfill StripeCustomer InternalId with org IDs |
| `frontend/src/routes/_auth/_dashboard/members.tsx` | New `/members` route using org context |

### Files for Cleanup Phase (separate migration after verification)

| File | Purpose |
|------|---------|
| `modules/github/backend/Github/Migrations/<timestamp>_RenameToOrganizationSettings.cs` | Rename table `GithubUserSettings` → `GithubOrganizationSettings` |

---

## Task 1: Update Stripe Interface and Accessor

**Files:**
- Modify: `modules/stripe/backend/Stripe/Services/ICurrentStripeCustomerIdAccessor.cs`
- Modify: `modules/stripe/backend/Stripe/Services/CurrentStripeCustomerIdAccessor.cs`
- Modify: `modules/stripe/backend/Stripe/Data/Models/StripeCustomer.cs`

- [ ] **Step 1: Update the interface**

Remove the `GetOrCreateStripeCustomerId(string internalId)` overload. Keep the parameterless version and the `ForOrganization` version. Update comments.

```csharp
// ICurrentStripeCustomerIdAccessor.cs — full file replacement
using Organizations.Abstractions;

namespace Stripe.Services;

public interface ICurrentStripeCustomerIdAccessor
{
    /// <summary>
    /// Gets the Stripe customer ID for the current organization (from X-Organization-Id header).
    /// Creates a new Stripe customer for the org if one doesn't exist.
    /// </summary>
    Task<string> GetOrCreateStripeCustomerId();

    /// <summary>
    /// Gets or creates a Stripe customer for a specific organization.
    /// Use this in background/webhook handlers where there is no HTTP context.
    /// </summary>
    Task<string> GetOrCreateStripeCustomerIdForOrganization(OrganizationId orgId);
}
```

- [ ] **Step 2: Update the implementation**

Rewrite `CurrentStripeCustomerIdAccessor`. The parameterless `GetOrCreateStripeCustomerId()` now resolves the org from `IOrganizationContextAccessor` and delegates to `GetOrCreateStripeCustomerIdForOrganization`. Remove `GetOrCreateStripeCustomerId(string userId)`, `GetUserIdFromContext()`, `SetStripeCustomerIdAsync()`.

In `CurrentStripeCustomerIdAccessor.cs`, replace the parameterless method and remove the user-based overload:

```csharp
// Replace lines 36-62 with:
public async Task<string> GetOrCreateStripeCustomerId()
{
    var orgId = await organizationContextAccessor.GetCurrentOrganizationIdAsync();
    return await GetOrCreateStripeCustomerIdForOrganization(orgId);
}

// Delete: GetOrCreateStripeCustomerId(string userId) method (old lines 42-62)
// Delete: SetStripeCustomerIdAsync() method (old lines 64-69)
// Delete: GetUserIdFromContext() method (old lines 71-77)
```

Also update `EnsureCustomerHasEmailAsync` (lines 112-128) — it currently calls `GetUserAsync(internalId)` to get an email. After migration, `internalId` is an org ID. Change it to look up the org's owner user for the email:

```csharp
private async Task EnsureCustomerHasEmailAsync(string stripeCustomerId, string internalId, CancellationToken cancellationToken = default)
{
    // internalId is now an org ID — look up the org's owner for email
    if (!OrganizationId.TryParse(internalId, out var orgId))
        return;

    var org = await organizationRepository.GetByIdAsync(orgId, cancellationToken);
    if (org is null) return;

    var owner = await userRepository.GetByIdAsync(org.OwnerUserId, cancellationToken);
    if (owner is null || string.IsNullOrEmpty(owner.Email))
        return;

    var customerService = new CustomerService(stripeClient);
    var updateOptions = new CustomerUpdateOptions
    {
        Email = owner.Email,
        Name = org.Name
    };

    await customerService.UpdateAsync(stripeCustomerId, updateOptions, cancellationToken: cancellationToken);
}
```

Remove `IHttpContextAccessor` from the constructor (no longer needed).

- [ ] **Step 3: Update StripeCustomer model comment**

In `modules/stripe/backend/Stripe/Data/Models/StripeCustomer.cs`, update the XML comment:

```csharp
/// <summary>
/// The internal ID — organization ID (as string) that this Stripe customer belongs to.
/// </summary>
public required string InternalId { get; set; }
```

- [ ] **Step 4: Build and verify compilation**

Run: `cd /Users/milo/milodev/gits/web/.worktrees/feature-org-scoped-billing/backend && dotnet build`

Expected: Build errors in callers that use the removed `GetOrCreateStripeCustomerId(string)` overload. That's expected — we fix those in Task 2.

- [ ] **Step 5: Commit**

```bash
git add modules/stripe/backend/Stripe/Services/ICurrentStripeCustomerIdAccessor.cs \
       modules/stripe/backend/Stripe/Services/CurrentStripeCustomerIdAccessor.cs \
       modules/stripe/backend/Stripe/Data/Models/StripeCustomer.cs
git commit -m "refactor(stripe): route Stripe customer lookup through org context

GetOrCreateStripeCustomerId() now resolves the current organization
from X-Organization-Id header instead of the user from JWT claims.
Removed the user-based string overload from the interface."
```

---

## Task 2: Update Stripe Service Callers

**Files:**
- Modify: `modules/stripe/backend/Stripe/Services/SubscriptionService.cs` (interface + implementation)
- Modify: `backend/Api/EventHandlers/Stripe/PaymentMethodMissingActions/TerminateAllProjects_PaymentMethodMissingAction.cs`
- Modify: `backend/Api/Endpoints/Projects/Project/Delete/DeleteProjectCommandHandler.cs`

- [ ] **Step 1: Update SubscriptionService interface and implementation**

In `SubscriptionService.cs`, replace the `GetSubscriptionsForUserAsync(string userId)` overload with an org-based version:

Replace interface declaration (lines 22-24):
```csharp
// Old:
Task<List<SubscriptionDto>> GetSubscriptionsForUserAsync(
    string userId,
    CancellationToken cancellationToken = default);

// New:
Task<List<SubscriptionDto>> GetSubscriptionsForOrganizationAsync(
    OrganizationId organizationId,
    CancellationToken cancellationToken = default);
```

Replace implementation (lines 65-71):
```csharp
// Old:
public async Task<List<SubscriptionDto>> GetSubscriptionsForUserAsync(
    string userId,
    CancellationToken cancellationToken = default)
{
    var stripeCustomerId = await currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerId(userId);
    return await GetSubscriptionsForStripeCustomerId(stripeCustomerId, cancellationToken);
}

// New:
public async Task<List<SubscriptionDto>> GetSubscriptionsForOrganizationAsync(
    OrganizationId organizationId,
    CancellationToken cancellationToken = default)
{
    var stripeCustomerId = await currentStripeCustomerIdAccessor.GetOrCreateStripeCustomerIdForOrganization(organizationId);
    return await GetSubscriptionsForStripeCustomerId(stripeCustomerId, cancellationToken);
}
```

Add `using Organizations.Abstractions;` to the imports.

- [ ] **Step 2: Update TerminateAllProjects action**

In `TerminateAllProjects_PaymentMethodMissingAction.cs`, the action currently passes `User.Id.Value.ToString()` to `GetSubscriptionsForUserAsync`. It needs to look up the user's personal org and use `GetSubscriptionsForOrganizationAsync`.

Replace line 23:
```csharp
// Old:
var subscriptions = await subscriptionsService.GetSubscriptionsForUserAsync(User.Id.Value.ToString(), cancellationToken);

// New:
var organizationRepository = scope.ServiceProvider.GetRequiredService<IOrganizationRepository>();
var personalOrg = await organizationRepository.GetPersonalOrgForUserAsync(User.Id, cancellationToken);
if (personalOrg is null)
{
    _logger.Warning("No personal organization found for user {UserId}. Cannot terminate projects.", User.Id);
    return;
}
var subscriptions = await subscriptionsService.GetSubscriptionsForOrganizationAsync(personalOrg.Id, cancellationToken);
```

Add `using Organizations.Data.Repositories;` to imports.

- [ ] **Step 3: Update DeleteProjectCommandHandler**

In `DeleteProjectCommandHandler.cs` (around lines 61-65), the `skipOwnerCheck` path passes a userId string. Replace with org-based lookup.

Replace:
```csharp
// Old (lines 61-65):
var subscriptions = skipOwnerCheck
    ? await subscriptionService.GetSubscriptionsForUserAsync(
        userId.Value.ToString(),
        cancellationToken)
    : await subscriptionService.GetSubscriptionsForUserAsync(cancellationToken);

// New:
var subscriptions = await subscriptionService.GetSubscriptionsForUserAsync(cancellationToken);
```

The `skipOwnerCheck` path no longer needs a separate overload — both paths use the current org context from the HTTP request. The parameterless `GetSubscriptionsForUserAsync()` resolves the org from the header, which is correct for both cases. If this handler is called from a background context (like the saga), the caller should set the org context appropriately.

**Note:** If `DeleteProjectCommandHandler` is invoked from TerminateAllProjects (a background action without HTTP context), it dispatches via MediatR `ISender`. Check if the HTTP context is available in that flow. If not, the handler needs an alternative path. Read the `DeleteProjectCommand` to check if it receives `ProjectId` and can look up the project's org directly.

Actually, looking at the code more carefully: `DeleteProjectCommand` has `SkipOwnerCheck: true` from the saga path. In that path, it should look up the project's organization directly:

```csharp
var subscriptions = skipOwnerCheck
    ? await subscriptionService.GetSubscriptionsForOrganizationAsync(
        project.OrganizationId,  // project already loaded earlier in this handler
        cancellationToken)
    : await subscriptionService.GetSubscriptionsForUserAsync(cancellationToken);
```

Verify that the `project` variable (the loaded project entity) has an `OrganizationId` property — it should since projects belong to organizations.

- [ ] **Step 4: Build and verify compilation**

Run: `cd /Users/milo/milodev/gits/web/.worktrees/feature-org-scoped-billing/backend && dotnet build`

Expected: PASS — all callers of the removed overload are now updated.

- [ ] **Step 5: Commit**

```bash
git add modules/stripe/backend/Stripe/Services/SubscriptionService.cs \
       backend/Api/EventHandlers/Stripe/PaymentMethodMissingActions/TerminateAllProjects_PaymentMethodMissingAction.cs \
       backend/Api/Endpoints/Projects/Project/Delete/DeleteProjectCommandHandler.cs
git commit -m "refactor(stripe): update subscription callers to use org context

TerminateAllProjects looks up the user's personal org.
DeleteProjectCommand uses the project's OrganizationId for skipOwnerCheck.
Removed the userId-based GetSubscriptionsForUserAsync overload."
```

---

## Task 3: GitHub Settings — Add OrganizationId Column

**Files:**
- Modify: `modules/github/backend/Github/Models/GithubUserSettings.cs`
- Modify: `modules/github/backend/Github/Data/GithubDbContext.cs`

- [ ] **Step 1: Add OrganizationId to the entity**

In `GithubUserSettings.cs`, add an `OrganizationId` property alongside the existing `UserId` (we keep `UserId` temporarily for the backfill migration):

```csharp
using Database.Models;
using Organizations.Abstractions;
using Auth.Abstractions;

namespace Github.Models;

public sealed class GithubUserSettings : BaseEntityWithId<GithubUserSettingsId>
{
    // Kept temporarily for migration — will be removed in cleanup phase
    public UserId? UserId { get; set; }

    public OrganizationId? OrganizationId { get; set; }

    public required string? SelectedProjectsInstallationId { get; set; }
    public required string? SelectedModulesInstallationId { get; set; }
}
```

- [ ] **Step 2: Check that the GithubDbContext entity registration still works**

The `GithubDbContext` has `public DbSet<GithubUserSettings> GithubUserSettings { get; set; }` — no change needed yet since we haven't renamed the entity.

- [ ] **Step 3: Generate the EF migration**

Run from the worktree root:

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-org-scoped-billing/backend && \
dotnet ef migrations add AddOrganizationIdToSettings \
  --project ../modules/github/backend/Github/Github.csproj \
  --startup-project Api
```

This creates a migration that adds the `OrganizationId` column (nullable) and makes `UserId` nullable.

- [ ] **Step 4: Edit the generated migration to include data backfill**

Open the generated migration file and add SQL to backfill `OrganizationId` from the user's personal org. Add this after the `AddColumn` for `OrganizationId` and the `AlterColumn` for `UserId`:

```csharp
// After the schema changes, backfill OrganizationId from personal orgs
migrationBuilder.Sql(@"
    UPDATE github.""GithubUserSettings"" gs
    SET ""OrganizationId"" = o.""Id""
    FROM organizations.""Organizations"" o
    WHERE o.""OwnerUserId"" = gs.""UserId""
      AND o.""IsPersonal"" = true;
");

// Validate: fail if any rows still have null OrganizationId
migrationBuilder.Sql(@"
    DO $$
    BEGIN
        IF EXISTS (
            SELECT 1 FROM github.""GithubUserSettings""
            WHERE ""OrganizationId"" IS NULL
        ) THEN
            RAISE EXCEPTION 'Migration validation failed: some GithubUserSettings rows have NULL OrganizationId after backfill. These users may not have personal organizations.';
        END IF;
    END $$;
");
```

- [ ] **Step 5: Build and verify**

Run: `cd /Users/milo/milodev/gits/web/.worktrees/feature-org-scoped-billing/backend && dotnet build`

Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add modules/github/backend/Github/Models/GithubUserSettings.cs \
       modules/github/backend/Github/Migrations/ \
       modules/github/backend/Github/Data/GithubDbContext.cs
git commit -m "feat(github): add OrganizationId to GithubUserSettings with backfill migration

Adds nullable OrganizationId column, backfills from users' personal
organizations, validates no nulls remain. UserId kept temporarily."
```

---

## Task 4: Stripe Customer InternalId Backfill Migration

**Files:**
- New migration in: `modules/stripe/backend/Stripe/Migrations/`

- [ ] **Step 1: Generate an empty EF migration for the Stripe module**

No schema change needed — just a data migration. Create an empty migration:

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-org-scoped-billing/backend && \
dotnet ef migrations add BackfillOrgInternalIds \
  --project ../modules/stripe/backend/Stripe/Stripe.csproj \
  --startup-project Api
```

- [ ] **Step 2: Edit the migration to backfill InternalId**

Replace the empty `Up` method body with:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Pre-flight: verify all StripeCustomer InternalIds that look like user IDs
    // have a corresponding personal org
    migrationBuilder.Sql(@"
        DO $$
        BEGIN
            IF EXISTS (
                SELECT 1 FROM stripe.""StripeCustomers"" sc
                LEFT JOIN organizations.""Organizations"" o
                    ON o.""OwnerUserId"" = sc.""InternalId""::uuid
                    AND o.""IsPersonal"" = true
                WHERE NOT EXISTS (
                    SELECT 1 FROM organizations.""Organizations"" o2
                    WHERE o2.""Id""::text = sc.""InternalId""
                )
                AND o.""Id"" IS NULL
            ) THEN
                RAISE EXCEPTION 'Pre-flight failed: some StripeCustomer InternalIds are user IDs without personal organizations';
            END IF;
        END $$;
    ");

    // Backfill: replace user IDs with their personal org IDs
    // Only update rows where InternalId matches a user ID (not already an org ID)
    migrationBuilder.Sql(@"
        UPDATE stripe.""StripeCustomers"" sc
        SET ""InternalId"" = o.""Id""::text
        FROM organizations.""Organizations"" o
        JOIN auth.""Users"" u ON o.""OwnerUserId"" = u.""Id""
        WHERE o.""IsPersonal"" = true
          AND sc.""InternalId"" = u.""Id""::text;
    ");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // Reverse: restore user IDs from personal org owner
    migrationBuilder.Sql(@"
        UPDATE stripe.""StripeCustomers"" sc
        SET ""InternalId"" = o.""OwnerUserId""::text
        FROM organizations.""Organizations"" o
        WHERE o.""IsPersonal"" = true
          AND sc.""InternalId"" = o.""Id""::text;
    ");
}
```

- [ ] **Step 3: Build and verify**

Run: `cd /Users/milo/milodev/gits/web/.worktrees/feature-org-scoped-billing/backend && dotnet build`

Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add modules/stripe/backend/Stripe/Migrations/
git commit -m "feat(stripe): backfill StripeCustomer InternalId with org IDs

Data migration that replaces user IDs with their personal organization
IDs in the StripeCustomers table. Includes pre-flight validation."
```

---

## Task 5: Update GitHub Endpoints and Repository

**Files:**
- Modify: `modules/github/backend/Github/Repositories/GithubUserSettingsRepository.cs`
- Modify: `modules/github/backend/Github/Endpoints/GetInstallationsRoute.cs`
- Modify: `modules/github/backend/Github/Endpoints/SetDefaultInstallationRoute.cs`
- Modify: `modules/github/backend/Github/Endpoints/SetDefaultInstallationId/SetDefaultInstallationIdCommand.cs`
- Modify: `modules/github/backend/Github/Endpoints/SetDefaultInstallationId/SetDefaultInstallationIdCommandHandler.cs`

- [ ] **Step 1: Add org-based repository methods**

In `GithubUserSettingsRepository.cs`, add methods that query by `OrganizationId`:

```csharp
// Add to interface IGithubUserSettingsRepository:
Task<GithubUserSettings?> GetOrDefaultByOrganizationIdAsync(OrganizationId organizationId);
Task<string> SetSelectedOrganizationForOrgAsync(
    OrganizationId organizationId,
    string selectedInstallationId,
    DefaultInstallationIdSelectionType defaultInstallationIdSelectionType);

// Add implementations:
public Task<GithubUserSettings?> GetOrDefaultByOrganizationIdAsync(OrganizationId organizationId)
    => Query().Where(g => g.OrganizationId == organizationId)
              .FirstOrDefaultAsync();

public async Task<string> SetSelectedOrganizationForOrgAsync(
    OrganizationId organizationId,
    string selectedInstallationId,
    DefaultInstallationIdSelectionType defaultInstallationIdSelectionType)
{
    var settings = await GetOrDefaultByOrganizationIdAsync(organizationId);
    if (settings is null)
    {
        settings = new GithubUserSettings
        {
            OrganizationId = organizationId,
            SelectedProjectsInstallationId = defaultInstallationIdSelectionType == DefaultInstallationIdSelectionType.Projects
                ? selectedInstallationId : null,
            SelectedModulesInstallationId = defaultInstallationIdSelectionType == DefaultInstallationIdSelectionType.Modules
                ? selectedInstallationId : null
        };
        await AddAsync(settings);
    }
    else
    {
        if (defaultInstallationIdSelectionType == DefaultInstallationIdSelectionType.Projects)
            settings.SelectedProjectsInstallationId = selectedInstallationId;
        else
            settings.SelectedModulesInstallationId = selectedInstallationId;
        await UpdateAsync(settings);
    }
    return selectedInstallationId;
}
```

Add `using Organizations.Abstractions;` to imports.

- [ ] **Step 2: Update SetDefaultInstallationIdCommand**

Replace `UserId` with `OrganizationId`:

```csharp
// SetDefaultInstallationIdCommand.cs
using Organizations.Abstractions;

public sealed record SetDefaultInstallationIdCommand(
    OrganizationId OrganizationId,
    string InstallationId,
    DefaultInstallationIdSelectionType DefaultInstallationIdSelectionType
) : ICommand<string>;
```

- [ ] **Step 3: Update SetDefaultInstallationIdCommandHandler**

Change the handler to call the org-based repository method:

```csharp
// In Handle method, replace:
// await githubUserSettingsRepository.SetSelectedOrganizationForUserAsync(command.UserId, ...)
// With:
await githubUserSettingsRepository.SetSelectedOrganizationForOrgAsync(
    command.OrganizationId,
    command.InstallationId,
    command.DefaultInstallationIdSelectionType);
```

- [ ] **Step 4: Update SetDefaultInstallationRoute**

Change the route to resolve org from context instead of user:

```csharp
// Replace the user ID extraction with org context:
// Old: var userId = currentUserAccessor.CurrentUserId!;
// New:
var orgId = await organizationContextAccessor.GetCurrentOrganizationIdAsync();

// Update the command creation:
// Old: new SetDefaultInstallationIdCommand(userId, ...)
// New:
new SetDefaultInstallationIdCommand(orgId, request.InstallationId, (DefaultInstallationIdSelectionType)request.Type)
```

Inject `IOrganizationContextAccessor` instead of `ICurrentUserAccessor`.

- [ ] **Step 5: Update GetInstallationsRoute**

Change to query by org instead of user:

```csharp
// Replace user-based settings lookup with org-based:
// Old: var settings = await githubUserSettingsRepository.GetOrDefaultByUserIdAsync(userId);
// New:
var orgId = await organizationContextAccessor.GetCurrentOrganizationIdAsync();
var settings = await githubUserSettingsRepository.GetOrDefaultByOrganizationIdAsync(orgId);
```

Inject `IOrganizationContextAccessor` instead of `ICurrentUserAccessor`.

- [ ] **Step 6: Build and verify**

Run: `cd /Users/milo/milodev/gits/web/.worktrees/feature-org-scoped-billing/backend && dotnet build`

Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add modules/github/backend/Github/
git commit -m "refactor(github): scope installation settings to organization

Endpoints now resolve org from X-Organization-Id header.
Repository methods query by OrganizationId instead of UserId."
```

---

## Task 6: Update GitHub New-User Event Handler

**Files:**
- Modify: `modules/github/backend/Github/Features/CreateDefaultSettings_FirstUserLoginEventHandler.cs`

- [ ] **Step 1: Update the event handler to use org ID**

The handler fires on `FirstUserLoginEvent` (BeforeSaveAsync). By this point, the personal org already exists (created in UserCreatedEvent's AfterSaveAsync, which completes before UpsertUserLogin runs).

```csharp
// In HandleBeforeSaveAsync, after getting installations:
// Look up the user's personal org
var orgRepo = /* resolve from DI or inject */;
var personalOrg = await orgRepo.GetPersonalOrgForUserAsync(domainEvent.UserId, cancellationToken);
if (personalOrg is null)
{
    // Personal org should always exist by this point
    logger.Warning("No personal org found for user {UserId} during first login", domainEvent.UserId);
    return;
}

// Create settings with OrganizationId instead of UserId
var settings = new GithubUserSettings
{
    OrganizationId = personalOrg.Id,
    SelectedProjectsInstallationId = installations.FirstOrDefault()?.InstallationId,
    SelectedModulesInstallationId = installations.FirstOrDefault()?.InstallationId,
};
```

Add `IOrganizationRepository` to the constructor dependencies.

- [ ] **Step 2: Build and verify**

Run: `cd /Users/milo/milodev/gits/web/.worktrees/feature-org-scoped-billing/backend && dotnet build`

Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add modules/github/backend/Github/Features/CreateDefaultSettings_FirstUserLoginEventHandler.cs
git commit -m "refactor(github): create settings with org ID on first login

Looks up user's personal org (guaranteed to exist from prior event)
and creates GithubUserSettings with OrganizationId."
```

---

## Task 7: Stripe API Metadata Sync

**Files:**
- Modify: `modules/stripe/backend/Stripe/StripeFeature.cs`
- Create: `modules/stripe/backend/Stripe/Services/StripeCustomerMetadataSyncService.cs`

- [ ] **Step 1: Create the sync service**

```csharp
// StripeCustomerMetadataSyncService.cs
using Microsoft.Extensions.Options;
using Organizations.Data.Repositories;
using Stripe.Data.Repositories;
using Stripe.Options;

namespace Stripe.Services;

public sealed class StripeCustomerMetadataSyncService(
    IStripeCustomerRepository stripeCustomerRepository,
    IOrganizationRepository organizationRepository,
    IOptions<StripeOptions> options)
{
    private static readonly Serilog.ILogger Logger = Serilog.Log.ForContext<StripeCustomerMetadataSyncService>();

    public async Task SyncAllCustomerMetadataAsync(CancellationToken cancellationToken = default)
    {
        var stripeClient = new StripeClient(options.Value.ApiKey);
        var customerService = new CustomerService(stripeClient);

        var allCustomers = await stripeCustomerRepository.GetAllAsync(cancellationToken);

        foreach (var customer in allCustomers)
        {
            try
            {
                var stripeCustomer = await customerService.GetAsync(
                    customer.StripeCustomerId, cancellationToken: cancellationToken);

                // Skip if already synced
                if (stripeCustomer.Metadata.TryGetValue("Type", out var type) && type == "organization")
                    continue;

                // Look up org name for the customer description
                string orgName = customer.InternalId;
                if (Organizations.Abstractions.OrganizationId.TryParse(customer.InternalId, out var orgId))
                {
                    var org = await organizationRepository.GetByIdAsync(orgId, cancellationToken);
                    if (org is not null)
                        orgName = org.Name;
                }

                await customerService.UpdateAsync(customer.StripeCustomerId, new CustomerUpdateOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "InternalId", customer.InternalId },
                        { "Type", "organization" }
                    },
                    Description = $"Organization: {orgName}",
                    Name = orgName,
                }, cancellationToken: cancellationToken);

                Logger.Information("Synced Stripe metadata for customer {CustomerId} (org: {OrgId})",
                    customer.StripeCustomerId, customer.InternalId);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to sync metadata for Stripe customer {CustomerId}",
                    customer.StripeCustomerId);
            }
        }
    }
}
```

- [ ] **Step 2: Add a `GetAllAsync` method to the repository if missing**

Check `IStripeCustomerRepository` — if it doesn't have `GetAllAsync`, add it:

```csharp
// In StripeCustomerRepository.cs, the base GenericRepository likely already has GetAllAsync.
// If not, add:
public Task<List<StripeCustomer>> GetAllAsync(CancellationToken cancellationToken = default)
    => Query().ToListAsync(cancellationToken);
```

- [ ] **Step 3: Register the service and call it on startup**

In `StripeFeature.cs`, register the service in `ConfigureServices`:

```csharp
services.AddScoped<StripeCustomerMetadataSyncService>();
```

In `ConfigureApp`, after the migration runs, add the one-time sync. Use a simple check — if any customer lacks `Type: organization` metadata, run the sync:

```csharp
// In ConfigureApp, after context.Database.Migrate():
var syncService = scope.ServiceProvider.GetRequiredService<StripeCustomerMetadataSyncService>();
await syncService.SyncAllCustomerMetadataAsync();
```

The sync is idempotent (skips already-synced customers), so running it on every startup is safe and simple. Once all customers are synced, it becomes a no-op.

- [ ] **Step 4: Build and verify**

Run: `cd /Users/milo/milodev/gits/web/.worktrees/feature-org-scoped-billing/backend && dotnet build`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add modules/stripe/backend/Stripe/Services/StripeCustomerMetadataSyncService.cs \
       modules/stripe/backend/Stripe/StripeFeature.cs
git commit -m "feat(stripe): sync Stripe customer metadata to reflect org ownership

Idempotent startup task that updates Stripe customer metadata
with Type=organization and the org name. Skips already-synced."
```

---

## Task 8: Frontend — Add Members to Sidebar

**Files:**
- Modify: `frontend/src/components/app-sidebar.tsx`

- [ ] **Step 1: Add Users icon import and Members nav item**

In `app-sidebar.tsx`, add `Users` to the lucide-react import (line 8):

```typescript
import { Building2, Check, ChevronDown, ChevronsUpDown, FileText, FolderGit2, Home, LogOut, Package, Settings, User, Users } from "lucide-react"
```

Add the Members menu item between Modules and Settings in the Navigation SidebarGroup (after the Modules SidebarMenuItem, before the Settings SidebarMenuItem):

```tsx
<SidebarMenuItem>
  <SidebarMenuButton
    onClick={() =>
      navigate({ viewTransition: true, to: "/members" })
    }
    isActive={isActive("/members")}
    tooltip="Members"
    className={
      isActive("/members")
        ? "relative text-white before:absolute before:left-0 before:top-1/2 before:h-5 before:-translate-y-1/2 before:w-[2px] before:rounded-r before:bg-[hsl(var(--nav-accent))]"
        : "text-white/60 hover:text-white"
    }
  >
    <Users className="h-4 w-4" />
    <span>Members</span>
  </SidebarMenuButton>
</SidebarMenuItem>
```

- [ ] **Step 2: Verify the `isActive` helper handles the new route**

Check how `isActive` is implemented in the file. It likely checks `pathname.startsWith(path)`. The `/members` route should match correctly.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/app-sidebar.tsx
git commit -m "feat(frontend): add Members nav item to sidebar

New sidebar entry between Modules and Settings, links to /members."
```

---

## Task 9: Frontend — Create /members Route

**Files:**
- Create: `frontend/src/routes/_auth/_dashboard/members.tsx`

- [ ] **Step 1: Create the members route**

This route adapts the existing `/organizations/$orgId/members` page to use the org context from `useOrganization()` instead of route params. It also uses the generated `api` client instead of raw `orgFetch`.

```tsx
// frontend/src/routes/_auth/_dashboard/members.tsx
import { createFileRoute } from '@tanstack/react-router'
import { Plus, Shield, User, UserMinus, Search, Github } from 'lucide-react'
import { useAuth } from '@/hooks/use-dynamic-auth'
import { Button } from '@/components/ui/button'
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { useToast } from '@/hooks/use-toast'
import { api } from '@/api-client'
import { useOrganization } from '@repo/organizations/org-provider'
import * as React from 'react'

export const Route = createFileRoute('/_auth/_dashboard/members')({
  component: MembersPage,
})

function MembersPage() {
  const { currentOrganization } = useOrganization()
  const orgId = currentOrganization?.id
  const { toast } = useToast()
  const { user: currentUser } = useAuth()
  const [inviteDialogOpen, setInviteDialogOpen] = React.useState(false)
  const [searchQuery, setSearchQuery] = React.useState('')
  const [debouncedQuery, setDebouncedQuery] = React.useState('')
  const [selectedUser, setSelectedUser] = React.useState<{ login: string; avatarUrl: string; name?: string; email?: string } | null>(null)
  const [inviteEmail, setInviteEmail] = React.useState('')
  const [inviteRole, setInviteRole] = React.useState('Member')
  const [removeMemberId, setRemoveMemberId] = React.useState<string | null>(null)

  React.useEffect(() => {
    const timer = setTimeout(() => setDebouncedQuery(searchQuery), 300)
    return () => clearTimeout(timer)
  }, [searchQuery])

  const { data: membersData, isLoading: membersLoading } = api.v1.getOrganizationsOrganizationIdMembers.useQuery(
    { path: { organizationId: orgId! } },
    { enabled: !!orgId },
  )

  const { data: invitationsData } = api.v1.getOrganizationsOrganizationIdInvitations.useQuery(
    { path: { organizationId: orgId! } },
    { enabled: !!orgId },
  )

  const inviteMutation = api.v1.postOrganizationsOrganizationIdInvitations.useMutation(
    { path: { organizationId: orgId! } },
    {
      onSuccess: () => {
        const email = selectedUser?.email || inviteEmail
        toast({ title: 'Invitation sent', description: `Invitation sent to ${email}`, variant: 'success' })
        api.v1.getOrganizationsOrganizationIdInvitations.invalidateQueries(
          { parameters: { path: { organizationId: orgId! } } },
        )
        handleCloseInviteDialog()
      },
      onError: (error: Error) => {
        toast({ title: 'Error', description: error.message, variant: 'destructive' })
      },
    },
  )

  const removeMemberMutation = api.v1.deleteOrganizationsOrganizationIdMembersMemberId.useMutation(
    undefined as void,
    {
      onSuccess: () => {
        toast({ title: 'Member removed', variant: 'success' })
        api.v1.getOrganizationsOrganizationIdMembers.invalidateQueries(
          { parameters: { path: { organizationId: orgId! } } },
        )
        setRemoveMemberId(null)
      },
      onError: (error: Error) => {
        toast({ title: 'Error', description: error.message, variant: 'destructive' })
      },
    },
  )

  const handleCloseInviteDialog = () => {
    setInviteDialogOpen(false)
    setSearchQuery('')
    setDebouncedQuery('')
    setSelectedUser(null)
    setInviteEmail('')
    setInviteRole('Member')
  }

  const handleInvite = () => {
    const email = selectedUser?.email || inviteEmail
    if (!email?.trim()) {
      toast({ title: 'Email required', description: 'Please enter an email address or select a GitHub user with a public email.', variant: 'destructive' })
      return
    }
    inviteMutation.mutate({ email: email.trim(), role: inviteRole === 'Admin' ? 0 : 1 })
  }

  const handleSelectUser = (user: typeof selectedUser & {}) => {
    setSelectedUser(user)
    setSearchQuery(user.login)
    if (user.email) {
      setInviteEmail(user.email)
    }
  }

  const formatDate = (dateStr: string) => new Date(dateStr).toLocaleDateString()

  // Cast response data since some endpoints may have untyped responses
  const members = membersData as unknown as Array<{
    id: string; userId: string; name: string; email: string;
    avatarUrl?: string | null; role: 'Admin' | 'Member'; joinedAtUtc: string;
  }> | undefined

  const invitations = invitationsData as unknown as Array<{
    id: string; email: string; role: 'Admin' | 'Member';
    status: string; expiresAtUtc: string; createdAtUtc: string;
  }> | undefined

  if (!orgId) {
    return (
      <main>
        <h1 className="text-3xl font-bold mb-6">Members</h1>
        <p className="text-muted-foreground">Select an organization to manage members.</p>
      </main>
    )
  }

  return (
    <main>
      <header className="mb-6">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold mb-2">Members</h1>
            <p className="text-muted-foreground">
              Manage who has access to this organization
            </p>
          </div>
          <Button onClick={() => setInviteDialogOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            Invite Member
          </Button>
        </div>
      </header>

      <div className="space-y-4">
        <Card>
          <CardHeader>
            <CardTitle>Team Members</CardTitle>
            <CardDescription>
              {membersLoading ? (
                <Skeleton className="h-4 w-32" />
              ) : (
                `${members?.length ?? 0} member${(members?.length ?? 0) !== 1 ? 's' : ''}`
              )}
            </CardDescription>
          </CardHeader>
          <CardContent>
            {membersLoading ? (
              <div className="space-y-3">
                <Skeleton className="h-12 w-full" />
                <Skeleton className="h-12 w-full" />
              </div>
            ) : members && members.length > 0 ? (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Member</TableHead>
                    <TableHead>Role</TableHead>
                    <TableHead>Joined</TableHead>
                    <TableHead className="w-[50px]"></TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {members.map((member) => (
                    <TableRow key={member.id}>
                      <TableCell>
                        <div className="flex items-center gap-3">
                          {member.avatarUrl ? (
                            <img src={member.avatarUrl} alt="" className="h-8 w-8 rounded-full ring-1 ring-white/10" />
                          ) : (
                            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-muted">
                              <User className="h-4 w-4 text-muted-foreground" />
                            </div>
                          )}
                          <div>
                            <p className="text-sm font-medium">{member.name}</p>
                            <p className="text-xs text-muted-foreground">{member.email}</p>
                          </div>
                        </div>
                      </TableCell>
                      <TableCell>
                        <Badge variant={member.role === 'Admin' ? 'default' : 'secondary'}>
                          {member.role === 'Admin' && <Shield className="h-3 w-3 mr-1" />}
                          {member.role}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-muted-foreground text-sm">
                        {formatDate(member.joinedAtUtc)}
                      </TableCell>
                      <TableCell>
                        {member.userId !== currentUser?.id && (
                          <Button
                            variant="ghost"
                            size="sm"
                            className="h-8 w-8 p-0"
                            onClick={() => setRemoveMemberId(member.id)}
                          >
                            <UserMinus className="h-4 w-4 text-destructive" />
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            ) : (
              <p className="text-muted-foreground text-sm">No members found.</p>
            )}
          </CardContent>
        </Card>

        {invitations && invitations.length > 0 && (
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Pending Invitations</CardTitle>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Email</TableHead>
                    <TableHead>Role</TableHead>
                    <TableHead>Expires</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {invitations.map((inv) => (
                    <TableRow key={inv.id}>
                      <TableCell className="font-mono text-sm">{inv.email}</TableCell>
                      <TableCell>
                        <Badge variant="outline">{inv.role}</Badge>
                      </TableCell>
                      <TableCell className="text-muted-foreground text-sm">
                        {formatDate(inv.expiresAtUtc)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        )}
      </div>

      {/* Invite Dialog */}
      <Dialog open={inviteDialogOpen} onOpenChange={(open) => { if (!open) handleCloseInviteDialog() }}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Invite a Member</DialogTitle>
            <DialogDescription>
              Search by GitHub username or enter an email address directly.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">GitHub username or email</label>
              <div className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  value={searchQuery}
                  onChange={(e) => { setSearchQuery(e.target.value); setSelectedUser(null) }}
                  placeholder="Search GitHub username or enter email..."
                  className="pl-9"
                  onKeyDown={(e) => e.key === 'Enter' && handleInvite()}
                />
              </div>

              {selectedUser && (
                <div className="flex items-center gap-3 rounded-md border bg-muted/30 px-3 py-2">
                  <img src={selectedUser.avatarUrl} alt="" className="h-8 w-8 rounded-full ring-1 ring-white/10" />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium">{selectedUser.login}</p>
                    {selectedUser.name && <p className="text-xs text-muted-foreground">{selectedUser.name}</p>}
                  </div>
                  <Button variant="ghost" size="sm" className="h-7 text-xs" onClick={() => { setSelectedUser(null); setSearchQuery(''); setInviteEmail('') }}>
                    Change
                  </Button>
                </div>
              )}
            </div>

            {selectedUser && !selectedUser.email && (
              <div className="space-y-2">
                <label className="text-sm font-medium">Email address</label>
                <Input type="email" value={inviteEmail} onChange={(e) => setInviteEmail(e.target.value)} placeholder={`Enter ${selectedUser.login}'s email address`} />
                <p className="text-xs text-muted-foreground">This user doesn't have a public email on GitHub.</p>
              </div>
            )}

            {!selectedUser && searchQuery.includes('@') && (
              <div className="flex items-center gap-2 rounded-md border bg-muted/30 px-3 py-2">
                <p className="text-sm text-muted-foreground">Inviting by email: <strong className="text-foreground">{searchQuery}</strong></p>
              </div>
            )}

            <div className="space-y-2">
              <label className="text-sm font-medium">Role</label>
              <Select value={inviteRole} onValueChange={setInviteRole}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="Member">Member - can view and deploy projects</SelectItem>
                  <SelectItem value="Admin">Admin - full access including billing and members</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={handleCloseInviteDialog}>Cancel</Button>
            <Button
              onClick={() => {
                if (!selectedUser && searchQuery.includes('@')) {
                  inviteMutation.mutate({ email: searchQuery.trim(), role: inviteRole === 'Admin' ? 0 : 1 })
                } else {
                  handleInvite()
                }
              }}
              disabled={inviteMutation.isPending || (!selectedUser?.email && !inviteEmail.trim() && !searchQuery.includes('@'))}
            >
              {inviteMutation.isPending ? 'Sending...' : 'Send Invitation'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Remove Member Confirmation */}
      <AlertDialog open={!!removeMemberId} onOpenChange={(open) => { if (!open) setRemoveMemberId(null) }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove Member</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to remove this member? They will lose access to all projects in this organization immediately.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => removeMemberId && removeMemberMutation.mutate({ path: { organizationId: orgId!, memberId: removeMemberId } })}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Remove Member
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </main>
  )
}
```

**Note:** The GitHub user search feature from the original page had a broken implementation (line 102 references undefined `res`). This version omits the search dropdown — it can be added as a follow-up once the search endpoint is fixed. The invite-by-email flow works correctly.

- [ ] **Step 2: Verify the route auto-generates**

The file is at `frontend/src/routes/_auth/_dashboard/members.tsx` which maps to the `/members` URL under the auth + dashboard layout. TanStack Router will auto-generate the route tree entry on the next dev server start.

- [ ] **Step 3: Run frontend typecheck**

Run: `cd /Users/milo/milodev/gits/web/.worktrees/feature-org-scoped-billing/frontend && npm run typecheck`

Expected: PASS (or known pre-existing errors only). The generated API client hooks used (`getOrganizationsOrganizationIdMembers`, `getOrganizationsOrganizationIdInvitations`, `postOrganizationsOrganizationIdInvitations`, `deleteOrganizationsOrganizationIdMembersMemberId`) should exist in the generated types. If any hook name doesn't match, check `frontend/src/generated/` for the correct name and update.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/routes/_auth/_dashboard/members.tsx
git commit -m "feat(frontend): add /members route using org context

Adapted from the existing org members page. Uses the generated API
client and reads org from useOrganization() context instead of URL params."
```

---

## Task 10: End-to-End Verification

- [ ] **Step 1: Start the dev environment**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-org-scoped-billing && compozerr dev
```

Wait for both frontend and backend to start.

- [ ] **Step 2: Test in the browser**

1. Navigate to `http://localhost:1234`
2. Verify the sidebar shows: Projects, Modules, **Members**, Settings
3. Click Members — verify it loads the member list for the current org
4. Switch orgs in the dropdown — verify Members page updates
5. Go to Settings — verify billing section loads (payment methods, subscriptions, invoices)
6. Switch orgs — verify billing data changes per org (if different orgs exist)

- [ ] **Step 3: Verify backend builds clean**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-org-scoped-billing/backend && dotnet build
```

- [ ] **Step 4: Run tests**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-org-scoped-billing && dotnet test
```

---

## Task 11: Cleanup — Rename GitHub Settings Table (after verification)

**This task should only be done after Task 10 passes and the feature is confirmed working.**

**Files:**
- Modify: `modules/github/backend/Github/Models/GithubUserSettings.cs` (rename to `GithubOrganizationSettings.cs`)
- Modify: `modules/github/backend/Github/Data/GithubDbContext.cs`
- Modify: `modules/github/backend/Github/Repositories/GithubUserSettingsRepository.cs` (rename to `GithubOrganizationSettingsRepository.cs`)
- Modify: `modules/github/backend/Github/GithubFeature.cs`
- New migration

- [ ] **Step 1: Rename the entity**

Rename `GithubUserSettings` to `GithubOrganizationSettings`. Rename `GithubUserSettingsId` to `GithubOrganizationSettingsId`. Remove the now-unused `UserId` property. Make `OrganizationId` required (non-nullable).

```csharp
// GithubOrganizationSettings.cs (renamed from GithubUserSettings.cs)
using Database.Models;
using Organizations.Abstractions;

namespace Github.Models;

public sealed class GithubOrganizationSettings : BaseEntityWithId<GithubOrganizationSettingsId>
{
    public required OrganizationId OrganizationId { get; set; }
    public required string? SelectedProjectsInstallationId { get; set; }
    public required string? SelectedModulesInstallationId { get; set; }
}
```

- [ ] **Step 2: Update DbContext**

```csharp
// In GithubDbContext.cs, change:
public DbSet<GithubOrganizationSettings> GithubOrganizationSettings { get; set; } = null!;
```

- [ ] **Step 3: Rename repository**

Rename `GithubUserSettingsRepository` to `GithubOrganizationSettingsRepository`. Update interface and implementation names. Remove user-based methods (`GetOrDefaultByUserIdAsync`, `SetSelectedOrganizationForUserAsync`).

- [ ] **Step 4: Update all references**

Update `GithubFeature.cs` service registration, all endpoint files, and the event handler to use the new names.

- [ ] **Step 5: Generate cleanup migration**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-org-scoped-billing/backend && \
dotnet ef migrations add RenameToOrganizationSettings \
  --project ../modules/github/backend/Github/Github.csproj \
  --startup-project Api
```

Edit the generated migration to:
1. Drop `UserId` column (if not already dropped)
2. Make `OrganizationId` non-nullable
3. Rename table from `GithubUserSettings` to `GithubOrganizationSettings`

```csharp
migrationBuilder.DropColumn(name: "UserId", table: "GithubUserSettings", schema: "github");

migrationBuilder.AlterColumn<Guid>(
    name: "OrganizationId",
    table: "GithubUserSettings",
    schema: "github",
    nullable: false,
    oldNullable: true);

migrationBuilder.RenameTable(
    name: "GithubUserSettings",
    schema: "github",
    newName: "GithubOrganizationSettings",
    newSchema: "github");
```

- [ ] **Step 6: Build and verify**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-org-scoped-billing/backend && dotnet build
```

- [ ] **Step 7: Commit**

```bash
git add modules/github/backend/Github/
git commit -m "refactor(github): rename GithubUserSettings to GithubOrganizationSettings

Final cleanup: rename entity, table, repository, and ID type.
Drop UserId column, make OrganizationId non-nullable."
```
