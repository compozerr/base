# Environment ID Deploy Plumbing — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Route the `environmentId` already on the `Deployment` row through to the env-vars lookup on the build VM, eliminating branch-based env resolution in the hosting manager. Fixes the 409 "Sequence contains more than one element" error when two environments share a branch.

**Architecture:** The backend deploy resolution chain (`EnvironmentId → EnvironmentName → branch → production`) already produces the correct `EnvironmentId` and persists it on the `Deployment`. This plan adds (1) a new hosting-side endpoint `GET /hosting/projects/{projectId}/environments/{environmentId}/variables` keyed by env ID, (2) a new `GET /hosting/projects/{projectId}/environments/production` fallback for legacy callers, and (3) updates the hosting manager to use the env ID from the `DeploymentJob` instead of rediscovering the env via git branch on the build VM.

**Tech Stack:** .NET 9 / Carter / MediatR / EF Core (backend), Node/TypeScript (hosting manager), xUnit / NSubstitute (backend tests).

**Spec:** `docs/superpowers/specs/2026-04-23-environment-id-deploy-plumbing-design.md`

**Worktrees (already created):**
- web: `/Users/milo/milodev/gits/web/.worktrees/fix-environment-id-plumbing` on branch `fix-environment-id-plumbing`
- hosting: `/Users/milo/milodev/gits/hosting/.worktrees/fix-environment-id-plumbing` on branch `fix-environment-id-plumbing`

---

## File Structure

**Backend (web repo):**

```
backend/Api.Data/Repositories/ProjectEnvironmentRepository.cs              (modify — add GetByIdWithVariablesAsync)
backend/Api.Hosting/Endpoints/Projects/EnvironmentVariables/                (new folder)
  GetEnvironmentVariablesByIdCommand.cs                                     (new)
  GetEnvironmentVariablesByIdCommandHandler.cs                              (new)
  GetEnvironmentVariablesByIdRoute.cs                                       (new)
  EnvironmentVariablesGroup.cs                                              (new)
backend/Api.Hosting/Endpoints/Projects/ProductionEnvironment/               (new folder)
  GetProductionEnvironmentRoute.cs                                          (new)
  ProductionEnvironmentGroup.cs                                             (new)
backend/Api.Hosting/Endpoints/Projects/ProjectsGroup.cs                     (modify — register two new groups)
backend/Api.Tests/Endpoints/Projects/EnvironmentVariables/
  GetEnvironmentVariablesByIdCommandHandlerTests.cs                         (new)
backend/Api.Hosting/Events/DeploymentQueuedEventPayload.cs                  (verify — confirm EnvironmentId on wire)
```

**Hosting manager (hosting repo):**

```
hosting/manager/services/deployment-api-client.ts                          (modify — add 2 methods)
hosting/manager/services/environment-variables-service.ts                  (modify — require envId, remove branch discovery)
hosting/manager/services/deployment-queue-service.ts                       (modify — pass job.environmentId)
hosting/manager/services/deployment-service.ts                             (modify — thread envId through legacy path)
```

---

## Task 1: Verify `environmentId` is on the wire payload to hosting manager

**Goal:** Before changing code, confirm the backend serializes `environmentId` in the payload it sends to the hosting manager when queueing deployments. If missing, add it. This is the assumption the whole plan rests on.

**Files:**
- Inspect: `backend/Api.Hosting/` (any file that constructs the queue/HTTP payload sent to the hosting manager with deployment details)
- Possibly modify: the payload type if `EnvironmentId` is missing.

- [ ] **Step 1: Find the outgoing payload type**

Run from `/Users/milo/milodev/gits/web/.worktrees/fix-environment-id-plumbing`:

```bash
grep -rn "deploymentId\|DeploymentQueued\|EnqueueDeployment\|deploy-queue\|/deploy\b" backend/Api.Hosting/ 2>/dev/null | head -40
```

Expected: find the event handler / HTTP sender that pushes the deployment to the hosting manager (likely in `Events/` or `Jobs/`).

Also check:

```bash
grep -rn "EnvironmentId\|environmentId" backend/Api.Hosting/ 2>/dev/null | grep -v "\.cs:.*//" | head -30
```

- [ ] **Step 2: Confirm the payload contains `EnvironmentId`**

Open the file(s) found in Step 1. Look for the outgoing DTO/record/anonymous type being sent to the hosting manager. Confirm it includes a field like `EnvironmentId` (Guid or nullable Guid).

If **present**: nothing to do. Write a short note in the task checkbox confirming which file/type carries it and move to Step 4.

If **absent**: add it. Use existing fields on the `Deployment` entity (`EnvironmentId`) as the source. Match the casing convention used by adjacent fields in that DTO.

- [ ] **Step 3 (only if missing): Add `EnvironmentId` to the outgoing payload**

Add the field to the payload record/DTO. Populate it from `deployment.EnvironmentId` wherever the payload is constructed.

- [ ] **Step 4: Build**

```bash
cd backend && dotnet build
```

Expected: success.

- [ ] **Step 5: Commit (only if Step 3 was needed)**

```bash
git add backend/Api.Hosting/
git commit -m "feat(hosting): include environmentId in deployment queue payload"
```

If nothing changed, skip the commit.

---

## Task 2: Add `GetByIdWithVariablesAsync` to the project environment repository

**Files:**
- Modify: `backend/Api.Data/Repositories/ProjectEnvironmentRepository.cs`

- [ ] **Step 1: Add interface method**

In `backend/Api.Data/Repositories/ProjectEnvironmentRepository.cs`, add to the `IProjectEnvironmentRepository` interface after the existing `GetProjectEnvironmentByNameAsync` line (it currently ends with `GetProjectEnvironmentByNameAsync(ProjectId projectId, string name);`):

```csharp
    public Task<ProjectEnvironment?> GetByIdWithVariablesAsync(ProjectEnvironmentId environmentId);
```

- [ ] **Step 2: Implement the method**

In the same file, add inside the `ProjectEnvironmentRepository` class, immediately after the existing `GetProjectEnvironmentByNameAsync` implementation:

```csharp
    public Task<ProjectEnvironment?> GetByIdWithVariablesAsync(ProjectEnvironmentId environmentId)
        => Query()
            .Include(x => x.ProjectEnvironmentVariables)
            .SingleOrDefaultAsync(x => x.Id == environmentId);
```

- [ ] **Step 3: Build**

```bash
cd backend && dotnet build
```

Expected: success.

- [ ] **Step 4: Commit**

```bash
git add backend/Api.Data/Repositories/ProjectEnvironmentRepository.cs
git commit -m "feat: add GetByIdWithVariablesAsync to project environment repository"
```

---

## Task 3: Write failing tests for `GetEnvironmentVariablesByIdCommandHandler`

**Files:**
- Create: `backend/Api.Tests/Endpoints/Projects/EnvironmentVariables/GetEnvironmentVariablesByIdCommandHandlerTests.cs`

The test references types (`GetEnvironmentVariablesByIdCommand`, `GetEnvironmentVariablesByIdResponse`, `GetEnvironmentVariablesByIdCommandHandler`) that we'll create in Task 4. The tests MUST fail to compile at this point — that's the red in red-green-refactor. We wire them up in Task 4.

- [ ] **Step 1: Create the test file**

Create the directory and file:

```bash
mkdir -p backend/Api.Tests/Endpoints/Projects/EnvironmentVariables
```

Create `backend/Api.Tests/Endpoints/Projects/EnvironmentVariables/GetEnvironmentVariablesByIdCommandHandlerTests.cs`:

```csharp
using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Endpoints.Projects.EnvironmentVariables;
using Api.ProjectTypes;
using NSubstitute;

namespace Api.Tests.Endpoints.Projects.EnvironmentVariables;

public class GetEnvironmentVariablesByIdCommandHandlerTests
{
    private readonly IProjectEnvironmentRepository _environmentRepository = Substitute.For<IProjectEnvironmentRepository>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IDefaultEnvironmentVariablesAppender _variablesAppender = Substitute.For<IDefaultEnvironmentVariablesAppender>();
    private readonly IProjectTypeConfigurationFactory _configurationFactory = Substitute.For<IProjectTypeConfigurationFactory>();

    private GetEnvironmentVariablesByIdCommandHandler Handler() =>
        new(_environmentRepository, _projectRepository, _variablesAppender, _configurationFactory);

    [Fact]
    public async Task Handle_WhenEnvironmentExists_ReturnsVariablesAndConfig()
    {
        var projectId = ProjectId.CreateNew();
        var environmentId = ProjectEnvironmentId.CreateNew();

        var env = new ProjectEnvironment
        {
            Id = environmentId,
            ProjectId = projectId,
            AutoDeploy = true,
            ProjectEnvironmentVariables = new List<ProjectEnvironmentVariable>
            {
                new() { Key = "FOO", Value = "bar", SystemType = SystemType.Backend }
            }
        };
        _environmentRepository.GetByIdWithVariablesAsync(environmentId).Returns(env);

        var project = new Project { Id = projectId, Type = ProjectType.Compozerr };
        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);

        var config = Substitute.For<IProjectTypeConfiguration>();
        config.ToDto().Returns(new ProjectTypeConfigurationDto("start", [], []));
        _configurationFactory.GetConfiguration(ProjectType.Compozerr).Returns(config);

        _variablesAppender
            .AppendDefaultVariablesAsync(Arg.Any<List<ProjectEnvironmentVariableDto>>(), projectId)
            .Returns(ci => ci.Arg<List<ProjectEnvironmentVariableDto>>());

        var response = await Handler().Handle(
            new GetEnvironmentVariablesByIdCommand(projectId, environmentId),
            CancellationToken.None);

        Assert.True(response.AutoDeploy);
        Assert.Equal(ProjectType.Compozerr, response.ProjectType);
        Assert.Single(response.Variables);
        Assert.Equal("FOO", response.Variables[0].Key);
        Assert.Equal("bar", response.Variables[0].Value);
    }

    [Fact]
    public async Task Handle_WhenEnvironmentNotFound_ReturnsNull()
    {
        var projectId = ProjectId.CreateNew();
        var environmentId = ProjectEnvironmentId.CreateNew();

        _environmentRepository.GetByIdWithVariablesAsync(environmentId).Returns((ProjectEnvironment?)null);

        var response = await Handler().Handle(
            new GetEnvironmentVariablesByIdCommand(projectId, environmentId),
            CancellationToken.None);

        Assert.Null(response);
    }

    [Fact]
    public async Task Handle_WhenEnvironmentBelongsToDifferentProject_ReturnsNull()
    {
        var requestedProjectId = ProjectId.CreateNew();
        var otherProjectId = ProjectId.CreateNew();
        var environmentId = ProjectEnvironmentId.CreateNew();

        var env = new ProjectEnvironment
        {
            Id = environmentId,
            ProjectId = otherProjectId,
            AutoDeploy = true,
            ProjectEnvironmentVariables = []
        };
        _environmentRepository.GetByIdWithVariablesAsync(environmentId).Returns(env);

        var response = await Handler().Handle(
            new GetEnvironmentVariablesByIdCommand(requestedProjectId, environmentId),
            CancellationToken.None);

        Assert.Null(response);
    }
}
```

- [ ] **Step 2: Try to build — expect compile errors**

```bash
cd backend && dotnet build
```

Expected: build fails. Compiler can't find `GetEnvironmentVariablesByIdCommand`, `GetEnvironmentVariablesByIdCommandHandler`, etc. That's the intended red state.

- [ ] **Step 3: Commit the failing tests**

```bash
git add backend/Api.Tests/Endpoints/Projects/EnvironmentVariables/
git commit -m "test: failing tests for get environment variables by id handler"
```

---

## Task 4: Implement `GetEnvironmentVariablesByIdCommand` + handler + route

**Files:**
- Create: `backend/Api.Hosting/Endpoints/Projects/EnvironmentVariables/GetEnvironmentVariablesByIdCommand.cs`
- Create: `backend/Api.Hosting/Endpoints/Projects/EnvironmentVariables/GetEnvironmentVariablesByIdCommandHandler.cs`
- Create: `backend/Api.Hosting/Endpoints/Projects/EnvironmentVariables/GetEnvironmentVariablesByIdRoute.cs`
- Create: `backend/Api.Hosting/Endpoints/Projects/EnvironmentVariables/EnvironmentVariablesGroup.cs`

Note: the handler returns `GetProjectEnvironmentResponse?` (the existing shape already consumed by hosting) — reusing the shape keeps the hosting client change to a single URL swap. When the env is not found or belongs to a different project, the handler returns `null`; the route converts that to 404.

- [ ] **Step 1: Create the command**

Create `backend/Api.Hosting/Endpoints/Projects/EnvironmentVariables/GetEnvironmentVariablesByIdCommand.cs`:

```csharp
using Api.Abstractions;
using Api.Data;
using Core.MediatR;

namespace Api.Hosting.Endpoints.Projects.EnvironmentVariables;

public sealed record GetEnvironmentVariablesByIdCommand(
    ProjectId ProjectId,
    ProjectEnvironmentId EnvironmentId) : ICommand<GetProjectEnvironmentResponse?>;
```

- [ ] **Step 2: Create the handler**

Create `backend/Api.Hosting/Endpoints/Projects/EnvironmentVariables/GetEnvironmentVariablesByIdCommandHandler.cs`:

```csharp
using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.ProjectTypes;
using Core.MediatR;

namespace Api.Hosting.Endpoints.Projects.EnvironmentVariables;

public sealed class GetEnvironmentVariablesByIdCommandHandler(
    IProjectEnvironmentRepository projectEnvironmentRepository,
    IProjectRepository projectRepository,
    IDefaultEnvironmentVariablesAppender variablesAppender,
    IProjectTypeConfigurationFactory configurationFactory)
    : ICommandHandler<GetEnvironmentVariablesByIdCommand, GetProjectEnvironmentResponse?>
{
    public async Task<GetProjectEnvironmentResponse?> Handle(
        GetEnvironmentVariablesByIdCommand command,
        CancellationToken cancellationToken = default)
    {
        var environment = await projectEnvironmentRepository.GetByIdWithVariablesAsync(command.EnvironmentId);

        if (environment is null) return null;
        if (environment.ProjectId != command.ProjectId) return null;

        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken);

        var environmentVariables = environment.ProjectEnvironmentVariables?
            .Select(x => new ProjectEnvironmentVariableDto(x.SystemType, x.Key, x.Value, false))
            .ToList() ?? [];

        environmentVariables = await variablesAppender.AppendDefaultVariablesAsync(
            environmentVariables,
            command.ProjectId);

        var projectType = project?.Type ?? ProjectType.Compozerr;
        var configuration = configurationFactory.GetConfiguration(projectType);

        return new GetProjectEnvironmentResponse(
            environment.AutoDeploy,
            projectType,
            configuration.ToDto(),
            environmentVariables);
    }
}
```

- [ ] **Step 3: Create the route**

Create `backend/Api.Hosting/Endpoints/Projects/EnvironmentVariables/GetEnvironmentVariablesByIdRoute.cs`:

```csharp
using Api.Abstractions;
using Api.Data;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.EnvironmentVariables;

public static class GetEnvironmentVariablesByIdRoute
{
    public const string Route = "{environmentId:guid}/variables";

    public static RouteHandlerBuilder AddGetEnvironmentVariablesByIdRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<Results<Ok<GetProjectEnvironmentResponse>, NotFound>> ExecuteAsync(
        ProjectId projectId,
        ProjectEnvironmentId environmentId,
        IMediator mediator)
    {
        var response = await mediator.Send(new GetEnvironmentVariablesByIdCommand(projectId, environmentId));
        return response is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(response);
    }
}
```

- [ ] **Step 4: Create the route group**

Create `backend/Api.Hosting/Endpoints/Projects/EnvironmentVariables/EnvironmentVariablesGroup.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.EnvironmentVariables;

public static class EnvironmentVariablesGroup
{
    public const string Route = "{projectId:guid}/environments";

    public static RouteGroupBuilder AddEnvironmentVariablesGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetEnvironmentVariablesByIdRoute();

        return group;
    }
}
```

- [ ] **Step 5: Register the group in `ProjectsGroup`**

Find the file that aggregates hosting-side project endpoints:

```bash
grep -rn "AddProjectsGroup\|ProjectsGroup" backend/Api.Hosting/Endpoints/Projects/ 2>/dev/null | head
```

Open the `ProjectsGroup.cs` file found (expected path: `backend/Api.Hosting/Endpoints/Projects/ProjectsGroup.cs`). Add the import:

```csharp
using Api.Hosting.Endpoints.Projects.EnvironmentVariables;
```

And inside the `AddProjectsGroup` method, add after the existing group registrations:

```csharp
        group.AddEnvironmentVariablesGroup();
```

If `ProjectsGroup.cs` doesn't register each subgroup explicitly (e.g. it uses a different composition pattern), match that pattern — the point is the new `EnvironmentVariablesGroup` has to be mounted so the route goes live.

- [ ] **Step 6: Build and run tests**

```bash
cd backend && dotnet build
```

Expected: success.

```bash
dotnet test --filter "FullyQualifiedName~GetEnvironmentVariablesByIdCommandHandlerTests"
```

Expected: all three tests pass.

- [ ] **Step 7: Commit**

```bash
git add backend/Api.Hosting/Endpoints/Projects/EnvironmentVariables/
git add backend/Api.Hosting/Endpoints/Projects/ProjectsGroup.cs
git commit -m "feat(hosting): add GET /hosting/projects/{id}/environments/{envId}/variables endpoint"
```

---

## Task 5: Add `GET /hosting/projects/{projectId}/environments/production`

Legacy fallback for `deployLegacyAsync` on the hosting side when `environmentId` is absent. Lightweight endpoint returning just `{ id, name }`.

**Files:**
- Create: `backend/Api.Hosting/Endpoints/Projects/ProductionEnvironment/GetProductionEnvironmentRoute.cs`
- Create: `backend/Api.Hosting/Endpoints/Projects/ProductionEnvironment/ProductionEnvironmentGroup.cs`
- Modify: `backend/Api.Hosting/Endpoints/Projects/ProjectsGroup.cs`

- [ ] **Step 1: Create the route**

Create `backend/Api.Hosting/Endpoints/Projects/ProductionEnvironment/GetProductionEnvironmentRoute.cs`:

```csharp
using Api.Abstractions;
using Api.Data.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.ProductionEnvironment;

public sealed record GetProductionEnvironmentResponse(Guid Id, string? Name);

public static class GetProductionEnvironmentRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddGetProductionEnvironmentRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<Results<Ok<GetProductionEnvironmentResponse>, NotFound>> ExecuteAsync(
        ProjectId projectId,
        IProjectEnvironmentRepository environmentRepository)
    {
        var env = await environmentRepository.GetProductionEnvironmentAsync(projectId);
        return env is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new GetProductionEnvironmentResponse(env.Id.Value, env.Name));
    }
}
```

- [ ] **Step 2: Create the group**

Create `backend/Api.Hosting/Endpoints/Projects/ProductionEnvironment/ProductionEnvironmentGroup.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.ProductionEnvironment;

public static class ProductionEnvironmentGroup
{
    public const string Route = "{projectId:guid}/environments/production";

    public static RouteGroupBuilder AddProductionEnvironmentGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetProductionEnvironmentRoute();

        return group;
    }
}
```

- [ ] **Step 3: Register the group**

In the same `ProjectsGroup.cs` file edited in Task 4 Step 5, add the import:

```csharp
using Api.Hosting.Endpoints.Projects.ProductionEnvironment;
```

And in `AddProjectsGroup`, add after the `AddEnvironmentVariablesGroup()` line added in Task 4:

```csharp
        group.AddProductionEnvironmentGroup();
```

- [ ] **Step 4: Build**

```bash
cd backend && dotnet build
```

Expected: success.

- [ ] **Step 5: Commit**

```bash
git add backend/Api.Hosting/Endpoints/Projects/ProductionEnvironment/
git add backend/Api.Hosting/Endpoints/Projects/ProjectsGroup.cs
git commit -m "feat(hosting): add GET /hosting/projects/{id}/environments/production endpoint"
```

---

## Task 6: Web-repo backend — push branch and open PR

- [ ] **Step 1: Confirm working tree is clean**

Run from the web worktree:

```bash
cd /Users/milo/milodev/gits/web/.worktrees/fix-environment-id-plumbing
git status
```

Expected: nothing to commit, working tree clean.

- [ ] **Step 2: Push branch and open PR**

```bash
git push -u origin fix-environment-id-plumbing
gh pr create --title "fix(hosting): plumb environmentId to env-vars lookup on build VM" --body "$(cat <<'EOF'
## Summary
- Adds `GET /hosting/projects/{id}/environments/{envId}/variables` keyed by environment ID
- Adds `GET /hosting/projects/{id}/environments/production` lightweight lookup for legacy-path fallback
- Adds `GetByIdWithVariablesAsync` on `IProjectEnvironmentRepository`
- Handler returns 404 on unknown env or cross-project ID mismatch

Companion PR on the hosting repo switches the build-VM env-vars lookup to use these endpoints, removing the branch-based lookup that throws `Sequence contains more than one element` when two environments share a branch.

Spec: `docs/superpowers/specs/2026-04-23-environment-id-deploy-plumbing-design.md`

## Test plan
- [x] Unit tests for handler (happy path, env not found, cross-project guard)
- [ ] After hosting PR merges: redeploy kurvo staging end-to-end
- [ ] Verify logs show new endpoint being called, no `?branch=main` env-vars calls
EOF
)"
```

Expected: PR URL printed. Capture it — it goes in the hosting PR body.

---

## Task 7: Hosting manager — add new API client methods

**Files:**
- Modify: `/Users/milo/milodev/gits/hosting/.worktrees/fix-environment-id-plumbing/manager/services/deployment-api-client.ts`

Work from the hosting worktree:

```bash
cd /Users/milo/milodev/gits/hosting/.worktrees/fix-environment-id-plumbing
```

- [ ] **Step 1: Add `getProjectEnvironmentVariablesByIdAsync`**

In `manager/services/deployment-api-client.ts`, find the existing `getProjectEnvironmentVariablesAsync` method (around line 158). Immediately below it, add:

```ts
    public static async getProjectEnvironmentVariablesByIdAsync(projectId: string, environmentId: string) {
        return await getHttpClient().getAsync<ProjectEnvironmentVariablesResponse>(
            `/projects/${projectId}/environments/${environmentId}/variables`
        );
    }
```

- [ ] **Step 2: Add `getProductionEnvironmentIdAsync`**

Below the method just added, append:

```ts
    public static async getProductionEnvironmentIdAsync(projectId: string) {
        return await getHttpClient().getAsync<{ id: string; name: string | null }>(
            `/projects/${projectId}/environments/production`
        );
    }
```

- [ ] **Step 3: Typecheck**

```bash
cd manager && npm run typecheck 2>&1 || npx tsc --noEmit 2>&1
```

(Use whichever command the repo uses — check `manager/package.json` scripts first.)

Expected: passes.

- [ ] **Step 4: Commit**

```bash
git add manager/services/deployment-api-client.ts
git commit -m "feat: add env-vars-by-id and production-env-id client methods"
```

---

## Task 8: Hosting manager — switch env-vars service to env ID

**Files:**
- Modify: `manager/services/environment-variables-service.ts`

- [ ] **Step 1: Change signature to accept `environmentId`**

In `manager/services/environment-variables-service.ts`, replace the `upsertEnvironmentVariablesInVmAsync` method signature:

From:
```ts
public static async upsertEnvironmentVariablesInVmAsync(vm: VMWithIp): Promise<ProjectConfiguration | null> {
```

To:
```ts
public static async upsertEnvironmentVariablesInVmAsync(vm: VMWithIp, environmentId: string): Promise<ProjectConfiguration | null> {
```

- [ ] **Step 2: Replace the branch-discovery block with env-ID lookup**

Still inside the same method, replace the block starting at `const branches = await this.getBranchesForCurrentCommitAsync(vm);` through `const environmentVariables = await DeploymentApiClient.getProjectEnvironmentVariablesAsync(projectId, firstBranch);` with:

```ts
        logger
            .forContext("projectId", projectId)
            .forContext("environmentId", environmentId)
            .information("Getting environment variables for project and environment ID");

        const environmentVariables = await DeploymentApiClient.getProjectEnvironmentVariablesByIdAsync(projectId, environmentId);

        logger.forContext("environmentVariables", environmentVariables).information("Got environment variables for project and environment ID");
```

Leave everything after `if (!environmentVariables) return null;` unchanged — the response shape is identical.

- [ ] **Step 3: Delete the now-unused `getBranchesForCurrentCommitAsync` private method**

Delete the entire `private static async getBranchesForCurrentCommitAsync(vm: VMWithIp): Promise<string[]> { ... }` method from the file.

- [ ] **Step 4: Typecheck (expect errors)**

```bash
cd manager && npm run typecheck 2>&1 || npx tsc --noEmit 2>&1
```

Expected: errors — callers of `upsertEnvironmentVariablesInVmAsync(vm)` now pass too few arguments. These callers are fixed in Tasks 9 and 10. Continue to those tasks before retrying typecheck.

- [ ] **Step 5: Commit**

```bash
git add manager/services/environment-variables-service.ts
git commit -m "feat: require environmentId in env-vars service, drop branch discovery"
```

---

## Task 9: Hosting manager — pass `job.environmentId` in `deployment-queue-service.ts`

**Files:**
- Modify: `manager/services/deployment-queue-service.ts`

- [ ] **Step 1: Update the `upsertEnvironmentVariablesInVmAsync` call**

In `manager/services/deployment-queue-service.ts`, find the call to `EnvironmentVariablesService.upsertEnvironmentVariablesInVmAsync(vm)` (around line 399). The enclosing function already destructures `environmentId` from the job (around line 585). Confirm `environmentId` is in scope at line 399 — if not, destructure it from the job/context available at that line.

Replace:
```ts
        const projectConfig = await EnvironmentVariablesService.upsertEnvironmentVariablesInVmAsync(vm);
```

With:
```ts
        if (!environmentId) {
            throw new Error("environmentId is required on DeploymentJob but was missing");
        }
        const projectConfig = await EnvironmentVariablesService.upsertEnvironmentVariablesInVmAsync(vm, environmentId);
```

If `environmentId` is destructured further down the function (not at line 399's scope), move the destructuring up or accept it as a parameter — whatever preserves the existing function shape with minimal churn.

- [ ] **Step 2: Typecheck**

```bash
cd manager && npm run typecheck 2>&1 || npx tsc --noEmit 2>&1
```

Expected: one remaining error (in `deployment-service.ts`, fixed in Task 10).

- [ ] **Step 3: Commit**

```bash
git add manager/services/deployment-queue-service.ts
git commit -m "feat: pass job.environmentId to env-vars service from deploy queue"
```

---

## Task 10: Hosting manager — thread `environmentId` through legacy deploy path

**Files:**
- Modify: `manager/services/deployment-service.ts`

- [ ] **Step 1: Thread `environmentId` into `startDockerComposeAsync`**

In `manager/services/deployment-service.ts`, locate the `startDockerComposeAsync(vm, projectId, environmentId?)` method (around line 208) and its call from `deployLegacyAsync` (around line 42).

Inside `startDockerComposeAsync`, replace the `EnvironmentVariablesService.upsertEnvironmentVariablesInVmAsync(vm)` call (around line 217) with:

```ts
        let resolvedEnvId = environmentId;
        if (!resolvedEnvId) {
            const prod = await DeploymentApiClient.getProductionEnvironmentIdAsync(projectId);
            if (!prod) {
                throw new Error(`No production environment found for project ${projectId}`);
            }
            resolvedEnvId = prod.id;
        }
        const projectConfig = await EnvironmentVariablesService.upsertEnvironmentVariablesInVmAsync(vm, resolvedEnvId);
```

If `DeploymentApiClient` isn't already imported in this file, add the import at the top.

- [ ] **Step 2: Typecheck**

```bash
cd manager && npm run typecheck 2>&1 || npx tsc --noEmit 2>&1
```

Expected: clean.

- [ ] **Step 3: Run any hosting tests that exist**

```bash
cd manager && npm test 2>&1 || true
```

If tests run, expect pass. If no tests exist for this path, skip.

- [ ] **Step 4: Commit**

```bash
git add manager/services/deployment-service.ts
git commit -m "feat: thread environmentId through legacy deploy path with production fallback"
```

---

## Task 11: Hosting repo — push branch and open PR

- [ ] **Step 1: Confirm working tree is clean**

```bash
cd /Users/milo/milodev/gits/hosting/.worktrees/fix-environment-id-plumbing
git status
```

Expected: nothing to commit.

- [ ] **Step 2: Push branch and open PR**

Substitute `<WEB_PR_URL>` with the URL captured in Task 6 Step 2.

```bash
git push -u origin fix-environment-id-plumbing
gh pr create --title "fix: use environmentId from DeploymentJob for env-vars lookup" --body "$(cat <<'EOF'
## Summary
- Switches the build-VM env-vars lookup from branch-based (`?branch=X`) to ID-based (`/environments/{envId}/variables`)
- Uses `job.environmentId` already carried on the `DeploymentJob` — no more `git branch -r --contains HEAD` discovery
- Legacy `deployLegacyAsync` falls back to `GET /environments/production` when `environmentId` is absent
- Fixes the 409 "Sequence contains more than one element" when two environments share a branch (seen on kurvo staging)

Requires the companion backend PR: <WEB_PR_URL>

## Test plan
- [ ] Typecheck passes
- [ ] Merge backend PR first, then this PR
- [ ] Redeploy kurvo staging — expect success and logs showing ID-based env-vars lookup
- [ ] Verify production deploy on another project still works (uses production fallback on the backend's deploy command handler, hosting receives envId the same way)
EOF
)"
```

Expected: PR URL printed.

---

## Task 12: End-to-end verification

Do this **after both PRs are merged** (hosting auto-deploys to production on push to main; backend deploy is manual per the web repo's `Deployment` section).

- [ ] **Step 1: Redeploy kurvo staging**

Push a trivial commit to kurvo's main branch (or re-run the `build-preview.yml` workflow from the Actions UI). The github-action will call `compozerr deploy -e staging` which now succeeds.

- [ ] **Step 2: Inspect hosting logs**

```bash
ssh debian@fra1.compozerr.com "sudo journalctl -u manager --since '10 minutes ago' --no-pager" | grep -i "environment"
```

Expected:
- Logs show `Getting environment variables for project and environment ID` with an `environmentId` field.
- No calls to `/environment?branch=` during this deploy flow.

- [ ] **Step 3: Verify production deploy still works**

On any project that deploys to production (no `environment:` input on the github-action), trigger a deploy. Expected: success. The backend's `DeployProjectCommandHandler` resolves the production env, persists `EnvironmentId` on the `Deployment`, hosting manager uses it for env-vars lookup.

- [ ] **Step 4: Report back**

If Step 1 or Step 3 fails, open a follow-up issue with the error and hosting logs. Otherwise, the plan is complete.

---

## Self-review

**Spec coverage:**

| Spec section | Task(s) |
|---|---|
| Arch: envId through to build VM | 1, 8, 9, 10 |
| Backend: GET /projects/{id}/environments/{envId}/variables | 3, 4 |
| Backend: GetByIdWithVariablesAsync repo method | 2 |
| Backend: GET /projects/{id}/environments/production | 5 |
| Backend: payload wire check | 1 |
| Hosting: new client methods | 7 |
| Hosting: env-vars service env-ID switch | 8 |
| Hosting: pass job.environmentId | 9 |
| Hosting: thread envId through legacy path w/ production fallback | 10 |
| Rollout: worktrees + PRs on both repos | 6, 11 |
| Verification | 12 |

**Risks acknowledged:** payload-wire check in Task 1, grep pass on other callers (deferred to cleanup PR per spec).

**Placeholder scan:** none found — every step has exact paths and complete code.

**Type consistency:** command/handler/response names match across Tasks 3 and 4 (`GetEnvironmentVariablesByIdCommand`, `GetEnvironmentVariablesByIdCommandHandler`, returning `GetProjectEnvironmentResponse?`). Hosting client methods named `getProjectEnvironmentVariablesByIdAsync` and `getProductionEnvironmentIdAsync` used consistently in Tasks 7, 8, 10.
