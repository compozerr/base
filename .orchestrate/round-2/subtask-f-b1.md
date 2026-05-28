# Subtask f-b1 — Spec B P1 fixes

**Intent:** Resolve the four P1 findings on the Spec B branch. These are merge-blockers — every one is either a security bug (cross-project hijack, AdminOnly bypass) or a correctness bug that breaks the runtime guarantee of per-env routing (preview-VM orphan on delete, Stripe orphan on alloc throw).

**Branch:** `task/spec-b-per-env-server-allocation-20260528-151212` (the Spec B run branch), **after** f-a1 has merged into Spec A's branch and Spec B has been rebased onto the new Spec A tip. Confirm before editing: `git log --oneline -1` shows the rebase merge commit, and `git log task/multi-server-support-for-compozerr-plus--20260528-102504..HEAD --oneline` shows Spec B-specific commits on top.

**Read first:**

- `.orchestrate/subtask.md` (this file).
- `/Users/milo/milodev/gits/web/.worktrees/spec-b-per-env-server-allocation-20260528-151212/.orchestrate/round-2/codex-findings/spec-b-review.md` — full Codex findings (esp. P1 detail).
- `backend/Api.Hosting/Services/EnvironmentTargetResolver/EnvironmentTargetResolver.cs` — the resolver you're fixing.
- `backend/Api/Endpoints/Projects/Environments/Create/CreateEnvironmentCommandHandler.cs` — the env-create handler with two P1s (Stripe + AdminOnly bypass).
- `backend/Api/Endpoints/Projects/Project/Delete/DeleteProjectCommandHandler.cs` — fan-out delete with preview-VM bug.
- `backend/Api/Endpoints/Projects/Domains/Add/AddDomainCommandHandler.cs` + `ChangeEnvironment/ChangeDomainEnvironmentCommandHandler.cs` — domain assignment without env-belongs-to-project validation.

## Findings to address

### F-P1-1 — Cross-project domain hijack

**Files:**
- `backend/Api.Hosting/Services/EnvironmentTargetResolver/EnvironmentTargetResolver.cs:54`
- `backend/Api/Endpoints/Projects/Domains/Add/AddDomainCommandHandler.cs:26`
- `backend/Api/Endpoints/Projects/Domains/ChangeEnvironment/ChangeDomainEnvironmentCommandHandler.cs:24`

**Problem:** The resolver trusts `Domain.ProjectEnvironmentId` without verifying that env belongs to `domain.ProjectId`. The Add/Change handlers don't validate either. A user can attach project A's domain to project B's env's server.

**Fix (defense in depth, both sides):**
1. In `AddDomainCommandHandler` and `ChangeDomainEnvironmentCommandHandler`: load the target env and validate `env.ProjectId == domain.ProjectId` (or the request's `ProjectId`). Reject with 400 if mismatch.
2. In the resolver's `GetServerForDomainAsync`: query for the env with `e.Id == envId && e.ProjectId == domain.ProjectId`. If no match, fail loudly (`InvalidOperationException` with a clear message: `"Domain {DomainId} references environment {EnvId} that does not belong to project {ProjectId}"`).

Add a test: `EnvironmentTargetResolverTests.GetServerForDomainAsync_RejectsCrossProjectEnvironment`.

### F-P1-2 — Preview VM orphan on DeleteProject

**File:** `backend/Api/Endpoints/Projects/Project/Delete/DeleteProjectCommandHandler.cs:33`

**Problem:** Delete fans out per distinct server and calls `DeleteProjectAsync(project.Id)` on each, but preview env VMs have a different identifier shape (`projectId--preview--environmentName`) and require `DeletePreviewEnvironmentAsync(projectId, env.Name)`. Result: preview VMs on non-prod servers stay alive; DB row gets deleted; orphaned compute.

**Fix:** Rewrite the fan-out to load envs and pick the correct tear-down call per env:
- Production env → `DeleteProjectAsync(projectId)` on its server.
- Preview env → `DeletePreviewEnvironmentAsync(projectId, env.Name)` on its server.

If ANY tear-down fails: do NOT delete the DB row. Either (a) collect failures and surface them to the caller with a 207-style "partial success" response, or (b) abort with 500 and log. Pick (b) — simpler — and surface the failure clearly. The user can retry deletion after fixing whatever's wrong.

Add a test: project with prod env on Server A and preview env on Server B → verify both tear-down calls fire on the right servers with the right identifiers.

### F-P1-3 — Stripe subscription orphan on alloc throw

**File:** `backend/Api/Endpoints/Projects/Environments/Create/CreateEnvironmentCommandHandler.cs:45-58`

**Problem:** Subscription is created at L45–49. Allocation runs at L56. The compensation `try/catch` starts at L58. If `ResolveServerIdAsync` throws, subscription is NOT cancelled. Direct contradiction of the plan's R4 mitigation.

**Fix:** Restructure so subscription creation, allocation, and `AddAsync` are all inside ONE compensation scope. Concretely:

```csharp
SubscriptionId? subscriptionId = null;
try
{
    subscriptionId = await stripeService.CreateSubscriptionAsync(...);
    var serverId = await ResolveServerIdAsync(...);
    var env = new ProjectEnvironment(... ServerId: serverId ...);
    await environmentRepository.AddAsync(env);
    return env;
}
catch
{
    if (subscriptionId is not null)
        await stripeService.CancelSubscriptionAsync(subscriptionId);
    throw;
}
```

Verify the existing `CreateEnvironmentCommandHandlerTests` still pass; add one new test: server-allocation throws → assert subscription cancellation was called.

### F-P1-4 — AdminOnly bypass via explicit ServerId

**File:** `backend/Api/Endpoints/Projects/Environments/Create/CreateEnvironmentCommandHandler.cs:89`

**Problem:** Explicit `ServerId` only checks existence via `serverRepository.GetByIdAsync`. The visibility filter is not applied. A non-admin user can hand-craft a request with an AdminOnly `serverId` and succeed.

**Fix:** Inject `IServerVisibilityFilter` (already present from Spec A). When explicit `ServerId` is supplied, run the candidate through the filter for the current user. If not visible: reject with the same domain validation error as f-a1's F3 ("Server not available for your account"). Don't reveal that it exists.

Add a test: non-admin sends explicit AdminOnly `serverId` → assert rejection (without leaking that the server exists).

## Acceptance criteria

- [ ] All four P1s addressed, each with at least one new test asserting the fix.
- [ ] `dotnet build backend/Base.sln` clean.
- [ ] `dotnet test backend/Base.sln` runs `Api.Tests` and green.
- [ ] Existing tests (`EnvironmentTargetResolverTests`, `UpdateDomains_FanOutTests`, `InternalDomainValueTests`, `CreateEnvironmentCommandHandlerTests`) still pass.
- [ ] Grep gate: `rg 'serverRepository\.GetByIdAsync' backend/Api/Endpoints --glob '!**/Migrations/**'` — no remaining hits that accept the result without filter validation (in user-input paths).
- [ ] Commit message references the finding IDs (F-P1-1..4).
- [ ] **Do NOT change the resolver's `GetHostingApiAsync`/`GetServerAsync` signatures** — the 31 call sites all assume the current shape. The fix is INSIDE the resolver method, not on the interface.

## Verification

- Run the new tests in isolation: `dotnet test backend/Api.Tests/Api.Tests.csproj --filter 'FullyQualifiedName~RejectsCrossProjectEnvironment' --filter 'FullyQualifiedName~PreviewEnvFanout' --filter 'FullyQualifiedName~SubscriptionCompensation' --filter 'FullyQualifiedName~AdminOnlyBypassRejection'`.
- Re-run the full suite to confirm no regression.

If Puppeteer browser verification was BLOCKED in s-b3, it stays BLOCKED here — these are backend correctness fixes. Surface that in your done report; don't pretend to verify in the browser.

Follow `CHILD.md`. `/code-review` self-review before signaling done.
