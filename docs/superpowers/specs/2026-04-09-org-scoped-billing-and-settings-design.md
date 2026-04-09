# Organization-Scoped Billing & Settings

## Problem

All billing (Stripe customers, payment methods, invoices, subscriptions) and GitHub integration settings (default project/modules organization) are currently scoped to the **user**, not the **organization**. This means:

- A user with multiple organizations cannot have separate billing per org
- Payment methods, invoices, and subscriptions are tied to the user's personal Stripe customer
- GitHub default org selections are stored per-user, not per-org
- The settings page is user-level with no org awareness
- Organization member management is buried behind the org dropdown, not in the main navigation

## Scope Changes

| Area | Current Scope | New Scope | Notes |
|------|--------------|-----------|-------|
| Stripe Customer | User | Organization | 1 Stripe customer per org |
| Payment Methods | User | Organization | Attached to org's Stripe customer |
| Invoices | User | Organization | Fetched for org's Stripe customer |
| Subscriptions | User (with project metadata) | Organization | Projects already belong to orgs |
| GitHub defaults (project/modules org) | User (`GithubUserSettings`) | Organization | Each org picks its own default GitHub installation |
| API Tokens | User | **User (stays)** | Tokens are auth credentials, not org config |
| Notification Preferences | User | **User (stays)** | Personal preferences |

## Architecture

### Backend: Stripe Module

**`CurrentStripeCustomerIdAccessor`** is the central change point. The parameterless `GetOrCreateStripeCustomerId()` currently resolves the user from HttpContext claims. After this change, it resolves the **current organization** via `IOrganizationContextAccessor` (which reads the `X-Organization-Id` header) and delegates to `GetOrCreateStripeCustomerIdForOrganization(orgId)`.

- `GetOrCreateStripeCustomerId()` -> reads org from `IOrganizationContextAccessor` -> calls `GetOrCreateStripeCustomerIdForOrganization(orgId)`
- The user-based overload `GetOrCreateStripeCustomerId(string userId)` is removed
- All downstream services (`PaymentMethodsService`, `InvoicesService`, `SubscriptionService`) need no changes -- they call `GetOrCreateStripeCustomerId()` and use whatever Stripe customer ID comes back
- `SetStripeCustomerIdAsync` and the `CreateCustomerAsync` private method update to use org context instead of user context

**Webhook handling**: `ProcessWebhookCommandHandler` resolves `CustomerId` -> `InternalId` via `StripeCustomerRepository`. After migration, `InternalId` is an org ID string. Webhook events store Stripe-level `CustomerId` -- no schema change needed. Business logic that assumes `InternalId` is a user ID must be updated.

**Sagas** (`PaymentFailureSaga`, `PaymentMethodMissingSaga`): Keyed by Stripe `CustomerId`, not internal ID. No change needed.

### Backend: GitHub Module

**`GithubUserSettings`** entity renamed to `GithubOrganizationSettings`. Table gets `OrganizationId` column replacing `UserId`. Each org has its own default project/modules GitHub installation.

**Endpoints**:
- `SetDefaultInstallationRoute` reads org from `X-Organization-Id` header via `IOrganizationContextAccessor` instead of user from JWT claims
- `GetInstallationsRoute` returns installations and selected defaults for the current org

**New user flow**: `CreateDefaultSettings_FirstUserLoginEventHandler` changes to look up the user's personal org (guaranteed to exist from prior event handler) and creates settings with `OrganizationId` instead of `UserId`.

### Backend: Event Ordering (verified safe)

The user creation flow runs in this order:
1. `UserCreatedEvent` fires -> `CreatePersonalOrg_UserCreatedEventHandler.HandleAfterSaveAsync()` creates personal org (committed to DB)
2. `UpsertUserLogin` runs -> `FirstUserLoginEvent` fires -> `CreateDefaultSettings_FirstUserLoginEventHandler.HandleBeforeSaveAsync()` creates GitHub settings

By step 2, the personal org from step 1 is already committed. The GitHub settings handler can safely query for the org ID.

## Migration Strategy

### Phase 1: Pre-flight Validation (read-only)

Before any data changes, validate:
- Every user with a `StripeCustomer` record has a personal org (`IsPersonal = true`)
- Every user with a `GithubUserSettings` record has a personal org
- No orphaned records exist

If any user lacks a personal org, create one (the seed migration `20260318210000_SeedPersonalOrganizations` should have handled this, but we double-check). If validation fails, the migration aborts with a descriptive error.

### Phase 2: GitHub Settings Migration (EF migration)

1. Add nullable `OrganizationId` column to `github.GithubUserSettings`
2. SQL backfill:
   ```sql
   UPDATE github."GithubUserSettings" gs
   SET "OrganizationId" = o."Id"
   FROM organizations."Organizations" o
   WHERE o."OwnerUserId" = gs."UserId"
     AND o."IsPersonal" = true
   ```
3. Validate no null `OrganizationId` values remain (fail if so)
4. Make `OrganizationId` non-nullable
5. Drop `UserId` column

### Phase 3: Stripe Customers Migration (same EF migration)

1. SQL backfill:
   ```sql
   UPDATE stripe."StripeCustomers" sc
   SET "InternalId" = o."Id"::text
   FROM organizations."Organizations" o
   JOIN auth."Users" u ON o."OwnerUserId" = u."Id"
   WHERE o."IsPersonal" = true
     AND sc."InternalId" = u."Id"::text
   ```
2. Validate no `InternalId` values still match user IDs

### Phase 4: Stripe API Metadata Sync (application startup)

Runs as a one-time startup task (gated by a flag in the database so it only executes once):
- For each `StripeCustomer` record, update Stripe API customer metadata to `{ "InternalId": orgId, "Type": "organization" }` and update the customer Name/Description to reflect the org name
- Idempotent: checks if metadata already has `Type: organization` before updating each customer
- After all customers are synced, sets the flag so subsequent startups skip this step

### Phase 5: Table Rename (final cleanup, separate EF migration)

After everything is verified working:
- Rename table `github.GithubUserSettings` -> `github.GithubOrganizationSettings`
- Rename entity `GithubUserSettings` -> `GithubOrganizationSettings`
- Rename `GithubUserSettingsId` -> `GithubOrganizationSettingsId`
- Update repository interface and implementation
- Update all references across the codebase

## Frontend Changes

### Sidebar Navigation

Current: `Projects > Modules > Settings`
New: `Projects > Modules > Members > Settings`

Add a "Members" item with a `Users` icon between Modules and Settings in `app-sidebar.tsx`. Links to `/members`.

### Members Page (`/members`)

A new route that shows the current org's member list, invite UI, and role management. This is the existing `/organizations/$orgId/members` page functionality, re-routed to use the current org from the `OrgProvider` context instead of a URL param.

**Design**: Use the `frontend-design` skill during implementation to ensure the Members page and sidebar item match the existing design system -- dark sidebar styling, active state indicators with left border highlight, card-based layout. No new design patterns; must look like it belongs in the current UI.

### Settings Page

Stays at `/settings`. Sections:
1. **GitHub Integration** -- default project/modules org selections (now org-scoped via backend)
2. **Billing & Subscriptions** -- payment methods, subscriptions, invoices (now org-scoped via backend)
3. **API Tokens** -- stays user-scoped
4. **Notification Preferences** -- stays user-scoped

No frontend code changes needed for billing/GitHub sections -- the `api` client already sends `X-Organization-Id` on every request. Once the backend reads that header, the correct org-scoped data is returned automatically.

### Organization Dropdown

The "Manage Organizations" link stays -- it goes to `/organizations` for creating/deleting orgs. Day-to-day member management moves to the sidebar `/members` route.

## Files Affected

### Stripe Module (Backend)
- `modules/stripe/backend/Stripe/Services/CurrentStripeCustomerIdAccessor.cs` -- main change
- `modules/stripe/backend/Stripe/Services/PaymentMethodsService.cs` -- minimal (if any)
- `modules/stripe/backend/Stripe/Services/InvoicesService.cs` -- minimal (if any)
- `modules/stripe/backend/Stripe/Services/SubscriptionService.cs` -- minimal (if any)
- `modules/stripe/backend/Stripe/Endpoints/Webhooks/ProcessWebhook/ProcessWebhookCommandHandler.cs` -- verify InternalId handling
- `modules/stripe/backend/Stripe/Data/Models/StripeCustomer.cs` -- update comment (InternalId is now org ID)

### GitHub Module (Backend)
- `modules/github/backend/Github/Models/GithubUserSettings.cs` -- rename + change UserId to OrganizationId
- `modules/github/backend/Github/Repositories/GithubUserSettingsRepository.cs` -- rename + update queries
- `modules/github/backend/Github/Endpoints/SetDefaultInstallationRoute.cs` -- use org context
- `modules/github/backend/Github/Endpoints/GetInstallationsRoute.cs` -- use org context
- `modules/github/backend/Github/Endpoints/SetDefaultInstallationId/SetDefaultInstallationIdCommand.cs` -- org ID instead of user ID
- `modules/github/backend/Github/Endpoints/SetDefaultInstallationId/SetDefaultInstallationIdCommandHandler.cs` -- org ID
- `modules/github/backend/Github/Features/CreateDefaultSettings_FirstUserLoginEventHandler.cs` -- use org ID
- `modules/github/backend/Github/Data/GithubDbContext.cs` -- update entity registration

### Frontend
- `frontend/src/components/app-sidebar.tsx` -- add Members nav item
- `frontend/src/routes/_auth/_dashboard/members.tsx` -- new route (adapted from org members page)
- `modules/organizations/frontend/src/routes/_auth/_dashboard/organizations/$orgId/members.tsx` -- source for Members page logic
- `frontend/src/components/mobile-org-picker.tsx` -- potentially update if it references members

### Migrations
- New EF migration in `modules/github/backend/Github/Migrations/` -- Phase 2+3
- New EF migration in `modules/stripe/backend/Stripe/Migrations/` -- if stripe schema changes needed
- Cleanup migration for table rename -- Phase 5
