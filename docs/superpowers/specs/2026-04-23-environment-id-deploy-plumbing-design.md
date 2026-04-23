# Environment ID Deploy Plumbing

**Date:** 2026-04-23
**Status:** Approved

## Problem

Deployments fail with HTTP 409 `Sequence contains more than one element` when two environments on the same project share a branch. Observed when `kurvo`'s `staging` environment was rebranched to `main` (same as production):

```
Deployment failed: GET /projects/517924f8-.../environment?branch=main failed with status 409
```

## Root cause

The deploy flow already resolves the correct `environmentId` in `DeployProjectCommandHandler` (via `EnvironmentId → EnvironmentName → branch → production` fallback) and persists it on the `Deployment` row. The `compozerr/github-action` and CLI correctly pass `environment: staging` end-to-end.

However, the **hosting manager** on the build VM discards that context and re-derives the environment from the VM's git branch:

```ts
// hosting/manager/services/environment-variables-service.ts:28-40
const branches = await this.getBranchesForCurrentCommitAsync(vm);
const firstBranch = branches[0];
DeploymentApiClient.getProjectEnvironmentVariablesAsync(projectId, firstBranch);
// → GET /projects/{id}/environment?branch=main
// → GetProjectEnvironmentByBranchAsync uses SingleOrDefaultAsync
// → throws when two envs match "main"
```

The `DeploymentJob` received by the hosting queue already contains `environmentId?: string` — the field is plumbed to the hosting side but ignored by the env-vars service.

## Goal

Route the `environmentId` already on the `Deployment` row through to the env-vars lookup on the build VM. The workflow's resolved environment (and therefore its commit hash) becomes the sole source of truth for what gets deployed; branch names on environments become cosmetic metadata used only by auto-deploy push-webhook matching.

## Architecture

```
CLI / github-action
  ↓
DeployProjectCommand (resolves envId: id → name → branch → production)
  ↓
Deployment row (EnvironmentId set — already working)
  ↓
Hosting manager receives DeploymentJob with environmentId (already working)
  ↓
EnvironmentVariablesService.upsertEnvironmentVariablesInVmAsync(vm, environmentId)   ← CHANGE
  ↓
GET /v1/projects/{projectId}/environments/{environmentId}/variables                  ← NEW
  ↓
Backend loads env by ID (unambiguous), returns vars + project config
```

No new resolution logic. No schema change. No data migration. The old branch-based endpoint (`GET /projects/{id}/environment?branch=X`) stays in place for one release cycle; a follow-up cleanup PR can delete it after telemetry confirms no callers remain.

## Backend changes (repo: `web`)

### New endpoint: `GET /v1/projects/{projectId}/environments/{environmentId}/variables`

Response shape matches the existing `GetProjectEnvironmentResponse` the hosting manager already consumes:

```csharp
public sealed record GetEnvironmentVariablesResponse(
    bool AutoDeploy,
    ProjectType ProjectType,
    ProjectTypeConfigurationDto Configuration,
    List<ProjectEnvironmentVariableDto> Variables);
```

Handler logic mirrors `GetProjectEnvironmentCommandHandler` but keyed by environment ID:

1. Load env by ID with `ProjectEnvironmentVariables` included
2. Return 404 if env not found
3. Verify `env.ProjectId == projectId` (guard against cross-project leakage via guessed IDs) — 404 on mismatch
4. Build variables list with `IDefaultEnvironmentVariablesAppender.AppendDefaultVariablesAsync`
5. Build configuration via `IProjectTypeConfigurationFactory`
6. Return response

### New repo method: `GetByIdWithVariablesAsync`

On `IProjectEnvironmentRepository`:

```csharp
Task<ProjectEnvironment?> GetByIdWithVariablesAsync(ProjectEnvironmentId environmentId);
```

Implementation: `Query().Include(x => x.ProjectEnvironmentVariables).SingleOrDefaultAsync(x => x.Id == environmentId)`.

Distinct from the existing generic `GetByIdAsync` so the `Include` is explicit at the call site.

### New endpoint: `GET /v1/projects/{projectId}/environments/production`

Used by the hosting manager's legacy deploy path as a branch-free fallback when `environmentId` is absent from the `DeploymentJob`. Response: `{ id: Guid, name: string }`. 404 if no production environment exists.

Uses the existing `IProjectEnvironmentRepository.GetProductionEnvironmentAsync`.

### Files

| File | Action |
|---|---|
| `backend/Api/Endpoints/Projects/Environments/GetEnvironmentVariables/GetEnvironmentVariablesRoute.cs` | new |
| `backend/Api/Endpoints/Projects/Environments/GetEnvironmentVariables/GetEnvironmentVariablesCommandHandler.cs` | new |
| `backend/Api/Endpoints/Projects/Environments/GetEnvironmentVariables/GetEnvironmentVariablesCommand.cs` | new |
| `backend/Api/Endpoints/Projects/Environments/GetProductionEnvironment/GetProductionEnvironmentRoute.cs` | new |
| `backend/Api/Endpoints/Projects/Environments/EnvironmentsGroup.cs` | register both new routes |
| `backend/Api.Data/Repositories/ProjectEnvironmentRepository.cs` | add `GetByIdWithVariablesAsync` |

### Unchanged

- `GetProjectEnvironmentRoute` (`{projectId}/environment?branch=X`) stays. Not called from deploy flow after this change; removed in a later cleanup PR.
- `GetProjectEnvironmentByBranchAsync` stays. Still used by `UpsertProjectEnvironmentAsync` internal flow.
- `DeployProjectCommandHandler` — already correct; no change.

### Deployment payload wire

`DeploymentJob.environmentId` is already present on the hosting side. Before implementation, grep the backend Hosting module (`backend/Api.Hosting/`) to confirm the serialized payload sent to the hosting manager includes `environmentId`. If not, add it — expected to be a one-liner in whatever constructs the outbound queue message.

## Hosting manager changes (repo: `hosting`)

### `deployment-api-client.ts`

Add two methods:

```ts
public static async getProjectEnvironmentVariablesByIdAsync(
    projectId: string,
    environmentId: string
) {
    return await getHttpClient().getAsync<ProjectEnvironmentVariablesResponse>(
        `/projects/${projectId}/environments/${environmentId}/variables`
    );
}

public static async getProductionEnvironmentIdAsync(projectId: string) {
    return await getHttpClient().getAsync<{ id: string; name: string }>(
        `/projects/${projectId}/environments/production`
    );
}
```

Keep `getProjectEnvironmentVariablesAsync(projectId, branch)` in place — no longer called from deploy flow, preserved until the cleanup PR.

### `environment-variables-service.ts`

Change signature:

```ts
public static async upsertEnvironmentVariablesInVmAsync(
    vm: VMWithIp,
    environmentId: string
): Promise<ProjectConfiguration | null>
```

- Delete the `getBranchesForCurrentCommitAsync` call at the top of the method.
- Delete the private `getBranchesForCurrentCommitAsync` method — no other callers expected; confirm with grep during implementation.
- Replace `DeploymentApiClient.getProjectEnvironmentVariablesAsync(projectId, firstBranch)` with `DeploymentApiClient.getProjectEnvironmentVariablesByIdAsync(projectId, environmentId)`.
- Rest of method (writing env files, returning project config) unchanged — the response shape is identical.

### `deployment-queue-service.ts`

Single call site at line 399. `environmentId` already destructured from `job` at line 585 and used elsewhere in the function. Pass it through:

```ts
const projectConfig = await EnvironmentVariablesService.upsertEnvironmentVariablesInVmAsync(vm, environmentId);
```

If `environmentId` is ever undefined in practice on this path, throw early with a clear message — the deploy flow should always populate it now that the backend resolution is complete. This surfaces any upstream gap fast instead of silently deploying with wrong env-vars.

### `deployment-service.ts` (legacy path)

`deployLegacyAsync(projectId, accessToken, repoName, commitHash, vmName?, environmentId?)` — last param already optional. Thread `environmentId` through to `startDockerComposeAsync` (already has it as an optional param at line 208) and down to the `upsertEnvironmentVariablesInVmAsync` call at line 217.

When `environmentId` is missing (old unmigrated callers):

```ts
let resolvedEnvId = environmentId;
if (!resolvedEnvId) {
    const prod = await DeploymentApiClient.getProductionEnvironmentIdAsync(projectId);
    if (!prod) throw new Error(`No production environment found for project ${projectId}`);
    resolvedEnvId = prod.id;
}
const projectConfig = await EnvironmentVariablesService.upsertEnvironmentVariablesInVmAsync(vm, resolvedEnvId);
```

This preserves the "production fallback" behavior that `DeployProjectCommandHandler` provides on the deploy endpoint, with zero branch dependency.

### Files

| File | Action |
|---|---|
| `hosting/manager/services/deployment-api-client.ts` | add 2 methods, keep old |
| `hosting/manager/services/environment-variables-service.ts` | require `environmentId`, delete branch-discovery |
| `hosting/manager/services/deployment-queue-service.ts` | pass `job.environmentId` |
| `hosting/manager/services/deployment-service.ts` | thread `environmentId` through `deployLegacyAsync` → `startDockerComposeAsync` → env-vars call; use production fallback when absent |

### Unchanged

- CLI (`/Users/milo/milodev/gits/cli`) — already passes `environmentName` correctly.
- `compozerr/github-action` (`/Users/milo/milodev/gits/github-action`) — already passes `-e $DEPLOY_ENVIRONMENT`.
- `kurvo` workflows — already pass `environment: staging`.

## Rollout

1. **Worktree `web`** branch `fix-environment-id-plumbing-backend`. PR: backend endpoints + repo method.
2. **Worktree `hosting`** branch `fix-environment-id-plumbing`. PR: hosting manager switches to ID-based lookup. Auto-deploys to production on merge to `main` per the hosting deploy setup.
3. **Verify** — redeploy `kurvo` staging. Expect logs to show ID-based lookup and no `?branch=main` env-vars call.
4. **Follow-up cleanup PR (not in this plan)** — after one release cycle with clean telemetry: delete `getProjectEnvironmentVariablesAsync` (hosting), `GetProjectEnvironmentRoute` and `GetProjectEnvironmentByBranchAsync` (backend, if no other callers).

## Risks

- **Other callers of the branch endpoint** — grep before cleanup. Current known callers: hosting env-vars service (removed by this PR) and the backend-internal `UpsertProjectEnvironmentAsync` (retains branch lookup for its own upsert logic; scoped fine).
- **Unit tests** — the deploy command handler has tests that cover environment resolution. The new env-vars-by-id endpoint should get a handler test covering: happy path, 404 on unknown ID, 404 on cross-project ID mismatch.
- **Deployment payload serialization** — confirm during implementation that `environmentId` is in the wire payload from backend to hosting, not just in the hosting-local type. One grep.

## Success criteria

- `kurvo` staging workflow deploys cleanly after the hosting PR merges.
- New env-vars-by-id endpoint has tests covering happy path + cross-project guard.
- No `?branch=` env-vars call appears in hosting logs during a deploy.
- No regression on production deploys (which omit `environment:` — resolved to production env via existing backend logic).
