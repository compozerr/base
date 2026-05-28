# Spec B → Spec A: Codex adversarial review

Branch under review: `task/spec-b-per-env-server-allocation-20260528-151212`.
Diff vs Spec A: 76 files, +3837/-514. Saved at `/tmp/specb-review/spec-b-vs-spec-a.diff`.

## Findings

### 1. Resolver correctness

- **[P1]** `backend/Api.Hosting/Services/EnvironmentTargetResolver/EnvironmentTargetResolver.cs:54`
  Resolver trusts `Domain.ProjectEnvironmentId` without verifying the env belongs to `domain.ProjectId`. `AddDomainCommandHandler.cs:26` and `ChangeDomainEnvironmentCommandHandler.cs:24` assign arbitrary env IDs. A caller can attach a domain for project A to project B's env/server → cross-project routing.
  **Fix:** Validate `EnvironmentId` belongs to `ProjectId` on add/change, and have the resolver query `e.Id == envId && e.ProjectId == domain.ProjectId`.

- **[P2]** `backend/Api.Hosting/Services/EnvironmentTargetResolver/EnvironmentTargetResolver.cs:56`
  Env-scoped domain with missing/null env server falls through to production/project hint at lines 60–62; even a missing env causes `TryGetServerAsync` to return null at line 34 and routes to prod. Silent misrouting for a domain explicitly pinned to an env.
  **Fix:** For non-null `ProjectEnvironmentId`, fail unless that exact env resolves to a server within the same project.

### 2. Read-site rerouting completeness

Clean. No remaining non-test reads of `project.ServerId` / `project.Server.*` outside resolver and allocator.

### 3. InternalDomain correctness

- **[P2]** `backend/Api.Data/InternalDomain.cs:12`
  `public string Value => $"{Subdomain}.sites.{ResolveHostName()}.compozerr.com";`
  `ResolveHostName()` at line 39 returns `Project?.Server?.HostName ?? string.Empty`. When env is not loaded, env server is null, or project server is null, silently emits `subdomain.sites..compozerr.com` (malformed).
  **Fix:** Throw or return null/unresolved when no hostname is available; do not emit a malformed domain string.

### 4. EF migration safety

Clean. `Down()` reverses, backfill is deterministic by `ProjectId`, index is on the right column.

### 5. `Project.ServerId` write sites

Clean. Non-test writes are limited to the deprecated creation hint at `AllocateServer_ProjectCreatedEventHandler.cs:15` and `DevSeeder.cs:249`.

### 6. UpdateDomains fan-out

Clean. New handler fans out via `GetDistinctHostingApisForProjectAsync`; zero servers skips the foreach; all-null env servers use project hint via resolver lines 91–100.

### 7. DeleteProject fan-out semantics

- **[P1]** `backend/Api/Endpoints/Projects/Project/Delete/DeleteProjectCommandHandler.cs:33`
  Delete fans out per distinct server and calls `DeleteProjectAsync(project.Id)` for each, but preview VMs use a different identifier (`projectId--preview--environmentName`) and require `DeletePreviewEnvironmentAsync`. Preview env VMs on non-prod servers will be left alive while DB row is deleted at line 48.
  **Fix:** Load envs, delete production with `DeleteProjectAsync` and previews with `DeletePreviewEnvironmentAsync(projectId, env.Name)`, and fail/compensate before DB deletion if teardown fails.

### 8. Stripe + allocator ordering

- **[P1]** `backend/Api/Endpoints/Projects/Environments/Create/CreateEnvironmentCommandHandler.cs:45`
  Subscription created at line 45–49, allocation runs at line 56, but the compensation `try/catch` starts at line 58. If `ResolveServerIdAsync` throws, the Stripe subscription is not cancelled — contradicting the comment at lines 54–55.
  **Fix:** Wrap subscription creation, allocation, and `AddAsync` in one compensation scope; cancel the subscription on any post-create failure.

### 9. N+1 risk

Clean. Env list eager-loads `e.Server.Location`; project list does not render per-env server and returns `Server: null`.

### 10. CLI wire compat

- **[P2]** `modules/cli/backend/Cli/Endpoints/Projects/ProjectDto.cs:22`
  `ProjectDto.ServerId` does not fall back to `project.ServerId`; if there are zero envs or production env has `ServerId == null`, line 43 throws `InvalidOperationException("Project has no allocated server")`. Breaks old CLI downgrade safety in exactly the transition cases this field is meant to cover.
  **Fix:** `effectiveServerId = productionEnvServerId ?? prodEnv?.ServerId ?? project.ServerId`.

### 11. Other critical findings

- **[P1]** `backend/Api/Endpoints/Projects/Environments/Create/CreateEnvironmentCommandHandler.cs:89`
  Explicit `ServerId` only checks existence via `serverRepository.GetByIdAsync`; `IServerVisibilityFilter` is not applied. A non-admin can bypass the picker and create an env on an AdminOnly server.
  **Fix:** Inject `IServerVisibilityFilter` and reject servers not visible to the current user before accepting the explicit ID.

- **[P2]** `backend/Api/Endpoints/Projects/Project/Start/StartProjectCommandHandler.cs:18`
  Project-wide start loops env IDs but calls `StartProjectAsync(command.ProjectId, command.EnvironmentName)` with a null `EnvironmentName` on each server. Preview VMs require the env name in `HostingApi.BuildVmIdentifier` at `HostingApi.cs:221`; they will not be started correctly.
  **Fix:** Carry env name/type in the fan-out and pass preview env names to hosting.

## Verdict

**VERDICT: block**

P1 summary (4):
1. Cross-project domain/server routing — resolver doesn't validate env belongs to domain's project
2. Preview VMs orphaned on delete — delete fan-out uses wrong identifier for preview envs
3. Stripe subscription not compensated if server allocation throws in `CreateEnvironmentCommandHandler`
4. AdminOnly server bypass — explicit `ServerId` in env create skips visibility filter
