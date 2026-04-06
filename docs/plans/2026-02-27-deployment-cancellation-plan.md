# Deployment Cancellation Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Allow users to cancel queued and actively running deployments, with the hosting manager refactored into a step-based pipeline with AbortController cancellation.

**Architecture:** The hosting manager's monolithic `executeDeployment` is refactored into discrete steps with `AbortSignal` checks between them. A new `POST /projects/cancel` endpoint on the hosting manager triggers abort. The web backend gets a `POST /v1/projects/{projectId}/deployments/{deploymentId}/cancel` endpoint that either cancels directly (queued) or forwards to the hosting manager (deploying). Frontend gets cancel buttons on both list and detail pages.

**Tech Stack:** TypeScript/Express (hosting), C#/.NET 9 (backend), React/TanStack Query (frontend)

**Design Doc:** `docs/plans/2026-02-27-deployment-cancellation-design.md`

---

### Task 1: Hosting Manager — DeploymentStep interface and pipeline runner

**Files:**
- Create: `/Users/milo/milodev/gits/hosting/manager/services/deployment-pipeline.ts`

**Step 1: Create the deployment pipeline module**

This module defines the step interface, context, and pipeline runner with AbortSignal support.

```typescript
import { BuildLogsService } from "./build-logs-service";
import { DeploymentApiClient } from "./deployment-api-client";
import { logger } from "./logger";

export interface DeploymentContext {
    projectId: string;
    accessToken: string;
    repoName: string;
    commitHash: string;
    deploymentId: string;
    serverTier: ServerTier;
    buildLogsService: BuildLogsService;
}

export interface DeploymentStep {
    name: string;
    execute: (context: DeploymentContext, signal: AbortSignal) => Promise<void>;
}

export class DeploymentCancelledError extends Error {
    public readonly stepName: string;
    constructor(stepName: string) {
        super(`Deployment cancelled at step: ${stepName}`);
        this.name = "DeploymentCancelledError";
        this.stepName = stepName;
    }
}

export async function runPipeline(
    steps: DeploymentStep[],
    context: DeploymentContext,
    signal: AbortSignal
): Promise<void> {
    for (const step of steps) {
        if (signal.aborted) {
            await context.buildLogsService.appendLogAsync(
                `Deployment cancelled by user`, "warning"
            );
            await context.buildLogsService.appendLogAsync(
                `Deployment stopped before step: ${step.name}`, "info"
            );
            await DeploymentApiClient.markDeploymentWithStatusAsync(
                context.deploymentId, "cancelled"
            );
            throw new DeploymentCancelledError(step.name);
        }

        logger.forContext("step", step.name)
            .forContext("deploymentId", context.deploymentId)
            .information(`Running deployment step: ${step.name}`);

        await step.execute(context, signal);
    }
}
```

**Step 2: Commit**

```bash
cd /Users/milo/milodev/gits/hosting
git add manager/services/deployment-pipeline.ts
git commit -m "feat: add deployment pipeline with step runner and AbortSignal support"
```

---

### Task 2: Hosting Manager — Add "cancelled" status to DeploymentApiClient

**Files:**
- Modify: `/Users/milo/milodev/gits/hosting/manager/services/deployment-api-client.ts:133`

**Step 1: Update `markDeploymentWithStatusAsync` to accept "cancelled"**

Change the type union on line 133 from:

```typescript
public static async markDeploymentWithStatusAsync(deploymentId: string, status: "deploying" | "failed" | "completed") {
```

to:

```typescript
public static async markDeploymentWithStatusAsync(deploymentId: string, status: "deploying" | "failed" | "completed" | "cancelled") {
```

**Step 2: Commit**

```bash
cd /Users/milo/milodev/gits/hosting
git add manager/services/deployment-api-client.ts
git commit -m "feat: add cancelled status to deployment API client"
```

---

### Task 3: Hosting Manager — Refactor executeDeployment into step pipeline

**Files:**
- Modify: `/Users/milo/milodev/gits/hosting/manager/services/deployment-queue-service.ts`

**Step 1: Add AbortController tracking and import pipeline**

Add imports and new properties to `DeploymentQueueService`:

At the top of the file, add:
```typescript
import { DeploymentStep, DeploymentContext, DeploymentCancelledError, runPipeline } from "./deployment-pipeline";
import { DomainService } from "./domain-service";
import { EnvironmentVariablesService } from "./environment-variables-service";
import { CompozerrConfigService } from "./compozerr-config-service";
import { ServiceReportingService } from "./service-reporting-service";
```

Add to the class properties (after line 31):
```typescript
private abortControllers: Map<string, AbortController> = new Map();
```

**Step 2: Add cancelDeployment method**

Add this public method to `DeploymentQueueService`:

```typescript
public cancelDeployment(deploymentId: string): { success: boolean; wasQueued: boolean } {
    // Check if it's in the pending queue
    const queueIndex = this.queue.findIndex(job => job.deploymentId === deploymentId);
    if (queueIndex !== -1) {
        const job = this.queue.splice(queueIndex, 1)[0];
        job.status = 'failed';
        job.error = 'Cancelled by user';
        this.completedJobs.unshift(job);
        if (this.completedJobs.length > this.MAX_COMPLETED_JOBS) {
            this.completedJobs.pop();
        }
        this.stateManager.removeDeployment(deploymentId);

        logger.forContext("deploymentId", deploymentId)
            .information("Deployment cancelled from queue");

        return { success: true, wasQueued: true };
    }

    // Check if it's the currently active deployment
    const controller = this.abortControllers.get(deploymentId);
    if (controller) {
        controller.abort();
        logger.forContext("deploymentId", deploymentId)
            .information("Deployment cancellation signal sent");
        return { success: true, wasQueued: false };
    }

    logger.forContext("deploymentId", deploymentId)
        .warning("Deployment not found for cancellation");
    return { success: false, wasQueued: false };
}
```

**Step 3: Refactor `executeDeployment` to use pipeline**

Replace the `executeDeployment` method (lines 168-255) with:

```typescript
private async executeDeployment(job: DeploymentJob, signal: AbortSignal): Promise<void> {
    const { projectId, accessToken, repoName, commitHash, deploymentId, serverTier } = job;

    const buildLogsService = new BuildLogsService(deploymentId);
    const context: DeploymentContext = {
        projectId, accessToken, repoName, commitHash, deploymentId, serverTier, buildLogsService
    };

    const deploymentService = new DeploymentService(buildLogsService);
    const domainService = ServiceProvider.get("domainService");
    const vmService = new VmsService(buildLogsService);
    const proxmoxApiClient = ServiceProvider.get("proxmoxApiClient");

    let vm: any;

    const steps: DeploymentStep[] = [
        {
            name: "markDeploying",
            execute: async (ctx) => {
                await DeploymentApiClient.markDeploymentWithStatusAsync(ctx.deploymentId, "deploying");
            }
        },
        {
            name: "provisionVm",
            execute: async (ctx) => {
                await ctx.buildLogsService.appendLogAsync("Getting or creating VM", "info");
                vm = await vmService.getOrCreateVmAsync(ctx.projectId, ctx.serverTier);
            }
        },
        {
            name: "verifyVm",
            execute: async () => {
                const vmStatus = await proxmoxApiClient.getVmStatusAsync(vm.vmid);
                if (vmStatus !== "running") {
                    throw new Error(`VM ${vm.vmid} is not running (status: ${vmStatus})`);
                }
                logger.forContext("vmId", vm.vmid)
                    .forContext("projectId", projectId)
                    .information("VM is running, proceeding with deployment");
            }
        },
        {
            name: "configureDomains",
            execute: async (ctx) => {
                await ctx.buildLogsService.appendLogAsync("Configuring domains", "info");
                const domainResponse = await domainService.upsertDomainsAsync(ctx.projectId, vm);
                if (!domainResponse.success) {
                    throw new Error("Failed to create domain");
                }
            }
        },
        {
            name: "waitForVm",
            execute: async (ctx, sig) => {
                await ctx.buildLogsService.appendLogAsync("Waiting for VM to be ready", "info");
                await vm.waitUntilReadyAsync(vm.isNewVm);
                logger.forContext("projectId", ctx.projectId).information("VM is ready");
            }
        },
        {
            name: "cloneRepo",
            execute: async (ctx) => {
                await ctx.buildLogsService.appendLogAsync("Deploying application", "info");
                await deploymentService.upsertGitRepoInsideVmAsync(
                    ctx.projectId, ctx.accessToken, ctx.repoName, ctx.commitHash
                );
            }
        },
        {
            name: "verifyServices",
            execute: async (ctx) => {
                await ctx.buildLogsService.appendLogAsync("Verifying services are running", "info");
                await new Promise(resolve => setTimeout(resolve, 5000));
                const servicesResult = await PortDetectionService.getRunningServicesAsync(vm);
                if (servicesResult.services.length === 0) {
                    logger.forContext("projectId", ctx.projectId)
                        .warning("No services detected after deployment, but continuing");
                } else {
                    logger.forContext("projectId", ctx.projectId)
                        .forContext("serviceCount", servicesResult.services.length)
                        .information("Services detected and running");
                }
            }
        },
        {
            name: "markCompleted",
            execute: async (ctx) => {
                await DeploymentApiClient.markDeploymentWithStatusAsync(ctx.deploymentId, "completed");
                await ctx.buildLogsService.appendLogAsync("Deployment completed successfully", "success");
                logger.forContext("projectId", ctx.projectId)
                    .information("Deployment completed successfully");
            }
        }
    ];

    logger.forContext("jobId", job.jobId)
        .forContext("projectId", projectId)
        .information("Starting deployment execution");

    try {
        await runPipeline(steps, context, signal);
    } catch (error) {
        if (error instanceof DeploymentCancelledError) {
            logger.forContext("jobId", job.jobId)
                .forContext("projectId", projectId)
                .forContext("cancelledAtStep", error.stepName)
                .information("Deployment was cancelled by user");
            throw error;
        }

        const errorMessage = error instanceof Error ? error.message : "Unknown error";
        await buildLogsService.appendLogAsync(`Deployment failed: ${errorMessage}`, "error");
        await DeploymentApiClient.markDeploymentWithStatusAsync(deploymentId, "failed");

        logger.forContext("error", error)
            .forContext("jobId", job.jobId)
            .forContext("projectId", projectId)
            .error("Deployment execution failed");

        throw error;
    }
}
```

**Step 4: Update `processQueue` to use AbortController**

Replace the `processQueue` method (lines 111-155) to create and manage AbortControllers:

```typescript
private async processQueue(): Promise<void> {
    if (this.isProcessing || this.queue.length === 0) {
        return;
    }

    this.isProcessing = true;

    try {
        while (this.queue.length > 0) {
            const job = this.queue.shift();
            if (!job) continue;

            this.currentJob = job;
            job.status = 'in_progress';
            this.stateManager.markActive(job.deploymentId);

            const controller = new AbortController();
            this.abortControllers.set(job.deploymentId, controller);

            try {
                await this.executeWithTimeout(job, controller.signal);
                job.status = 'completed';
                this.stateManager.removeDeployment(job.deploymentId);
                logger.forContext("jobId", job.jobId)
                    .forContext("projectId", job.projectId)
                    .information("Deployment job completed successfully");
            } catch (error) {
                if (error instanceof DeploymentCancelledError) {
                    job.status = 'failed';
                    job.error = 'Cancelled by user';
                } else {
                    job.status = 'failed';
                    job.error = error instanceof Error ? error.message : 'Unknown error';
                }
                this.stateManager.removeDeployment(job.deploymentId);
                logger.forContext("error", error)
                    .forContext("jobId", job.jobId)
                    .forContext("projectId", job.projectId)
                    .error("Deployment job failed");
            } finally {
                this.abortControllers.delete(job.deploymentId);
            }

            // Move to completed jobs
            this.completedJobs.unshift(job);
            if (this.completedJobs.length > this.MAX_COMPLETED_JOBS) {
                this.completedJobs.pop();
            }

            this.currentJob = null;
        }
    } finally {
        this.isProcessing = false;
    }
}
```

**Step 5: Update `executeWithTimeout` to pass signal**

Replace the `executeWithTimeout` method (lines 157-166):

```typescript
private async executeWithTimeout(job: DeploymentJob, signal: AbortSignal): Promise<void> {
    const timeoutPromise = new Promise<never>((_, reject) => {
        setTimeout(() => reject(new Error(`Deployment timed out after ${this.JOB_TIMEOUT_MS / 60000} minutes`)), this.JOB_TIMEOUT_MS);
    });

    await Promise.race([
        this.executeDeployment(job, signal),
        timeoutPromise
    ]);
}
```

**Step 6: Commit**

```bash
cd /Users/milo/milodev/gits/hosting
git add manager/services/deployment-queue-service.ts
git commit -m "feat: refactor deployment into step pipeline with AbortController cancellation"
```

---

### Task 4: Hosting Manager — Add cancel route

**Files:**
- Modify: `/Users/milo/milodev/gits/hosting/manager/routes/projects.ts`

**Step 1: Add the cancel endpoint**

Add after the `/deploy` route (after line 75):

```typescript
router.post("/cancel", async (req, res) => {
    const { deploymentId } = req.body as { deploymentId: string };

    if (!deploymentId) {
        res.status(400).json({ error: "deploymentId is required" });
        return;
    }

    logger.forContext("deploymentId", deploymentId).information("Cancel deployment requested");

    const deploymentQueue = DeploymentQueueService.getInstance();
    const result = deploymentQueue.cancelDeployment(deploymentId);

    if (!result.success) {
        res.status(404).json({ error: "Deployment not found in queue or active jobs" });
        return;
    }

    if (result.wasQueued) {
        // Queued deployments need their status updated via callback
        await DeploymentApiClient.markDeploymentWithStatusAsync(deploymentId, "cancelled");
    }
    // Active deployments will mark themselves cancelled via the pipeline

    res.status(200).json({
        message: "Deployment cancellation initiated",
        wasQueued: result.wasQueued
    });
});
```

**Step 2: Commit**

```bash
cd /Users/milo/milodev/gits/hosting
git add manager/routes/projects.ts
git commit -m "feat: add POST /projects/cancel endpoint for deployment cancellation"
```

---

### Task 5: Web Backend — Add CancelDeployment command, handler, and route

**Files:**
- Create: `/Users/milo/milodev/gits/web/backend/Api/Endpoints/Projects/Deployments/CancelDeployment/CancelDeploymentCommand.cs`
- Create: `/Users/milo/milodev/gits/web/backend/Api/Endpoints/Projects/Deployments/CancelDeployment/CancelDeploymentResponse.cs`
- Create: `/Users/milo/milodev/gits/web/backend/Api/Endpoints/Projects/Deployments/CancelDeployment/CancelDeploymentRoute.cs`
- Create: `/Users/milo/milodev/gits/web/backend/Api/Endpoints/Projects/Deployments/CancelDeployment/CancelDeploymentCommandHandler.cs`
- Modify: `/Users/milo/milodev/gits/web/backend/Api/Endpoints/Projects/Deployments/DeploymentsGroup.cs`
- Modify: `/Users/milo/milodev/gits/web/backend/Api.Hosting/Services/IHostingApi.cs`
- Modify: `/Users/milo/milodev/gits/web/backend/Api.Hosting/Services/HostingApi.cs`

**Step 1: Create CancelDeploymentCommand**

```csharp
// CancelDeploymentCommand.cs
using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Projects.Deployments.CancelDeployment;

public sealed record CancelDeploymentCommand(
    ProjectId ProjectId,
    DeploymentId DeploymentId) : ICommand<CancelDeploymentResponse>;
```

**Step 2: Create CancelDeploymentResponse**

```csharp
// CancelDeploymentResponse.cs
namespace Api.Endpoints.Projects.Deployments.CancelDeployment;

public sealed record CancelDeploymentResponse(bool Success);
```

**Step 3: Create CancelDeploymentRoute**

Follow the pattern from `RedeployDeploymentRoute.cs`:

```csharp
// CancelDeploymentRoute.cs
using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Deployments.CancelDeployment;

public static class CancelDeploymentRoute
{
    public const string Route = "{deploymentId:guid}/cancel";

    public static RouteHandlerBuilder AddCancelDeploymentRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<CancelDeploymentResponse> ExecuteAsync(
        ProjectId projectId,
        DeploymentId deploymentId,
        IMediator mediator)
        => mediator.Send(new CancelDeploymentCommand(projectId, deploymentId));
}
```

**Step 4: Add CancelDeploymentAsync to IHostingApi**

Add to `IHostingApi.cs` (line 20, before `UpdateTcpProxyAsync`):

```csharp
Task CancelDeploymentAsync(DeploymentId deploymentId);
```

**Step 5: Implement CancelDeploymentAsync in HostingApi**

Add to `HostingApi.cs` (after the `DeployAsync` method, around line 185):

```csharp
public async Task CancelDeploymentAsync(DeploymentId deploymentId)
{
    var timeout = TimeSpan.FromSeconds(30);
    using var cts = new CancellationTokenSource(timeout);

    var response = await HttpClient.PostAsync("/projects/cancel", JsonContent.Create(new
    {
        deploymentId = deploymentId.Value.ToString()
    }), cts.Token);

    if (!response.IsSuccessStatusCode)
    {
        Log.ForContext("BaseDomain", HttpClient.BaseDomain)
           .Error("Failed to cancel deployment {deploymentId}: {response}",
               deploymentId, await response.Content.ReadAsStringAsync());
    }
}
```

**Step 6: Create CancelDeploymentCommandHandler**

```csharp
// CancelDeploymentCommandHandler.cs
using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Endpoints.Deployments.Logs.Add;
using Api.Hosting.Services;
using Api.Services.DeploymentTrigger;
using Core.MediatR;
using MediatR;

namespace Api.Endpoints.Projects.Deployments.CancelDeployment;

public sealed class CancelDeploymentCommandHandler(
    IDeploymentRepository deploymentRepository,
    IHostingApiFactory hostingApiFactory,
    IMediator mediator) : ICommandHandler<CancelDeploymentCommand, CancelDeploymentResponse>
{
    public async Task<CancelDeploymentResponse> Handle(
        CancelDeploymentCommand command,
        CancellationToken cancellationToken = default)
    {
        var deployment = await deploymentRepository.GetByIdAsync(command.DeploymentId, cancellationToken);

        if (deployment is null || deployment.ProjectId != command.ProjectId)
            return new CancelDeploymentResponse(false);

        if (deployment.Status is not (DeploymentStatus.Queued or DeploymentStatus.Deploying))
            return new CancelDeploymentResponse(false);

        if (deployment.Status == DeploymentStatus.Deploying)
        {
            try
            {
                var api = await hostingApiFactory.GetHostingApiAsync(
                    deployment.Project?.ServerId ?? throw new Exception("No server assigned"));
                await api.CancelDeploymentAsync(deployment.Id);
            }
            catch (Exception ex)
            {
                Log.ForContext("DeploymentId", deployment.Id.Value)
                   .Error(ex, "Failed to send cancel to hosting manager, marking as cancelled locally");
            }
        }

        deployment.Status = DeploymentStatus.Cancelled;
        deployment.BuildDuration = deployment.StartedAtUtc.HasValue
            ? DateTime.UtcNow - deployment.StartedAtUtc.Value
            : null;
        await deploymentRepository.UpdateAsync(deployment, cancellationToken);

        await mediator.Send(new AddLogCommand(
            deployment.Id,
            "Deployment cancelled by user",
            LogLevel.Warning), cancellationToken);

        await mediator.Send(
            new DeploymentTriggerCommand(deployment.ProjectId),
            cancellationToken);

        return new CancelDeploymentResponse(true);
    }
}
```

**Step 7: Register route in DeploymentsGroup**

Modify `/Users/milo/milodev/gits/web/backend/Api/Endpoints/Projects/Deployments/DeploymentsGroup.cs`:

Add import:
```csharp
using Api.Endpoints.Projects.Deployments.CancelDeployment;
```

Add route registration (after line 19, before `return group`):
```csharp
group.AddCancelDeploymentRoute();
```

**Step 8: Update ChangeDeploymentStatusCommandHandler for Cancelled case**

Modify `/Users/milo/milodev/gits/web/backend/Api.Hosting/Endpoints/Deployments/ChangeDeploymentStatus/ChangeDeploymentStatusCommandHandler.cs`.

Add a case for `Cancelled` in the switch (after the `Failed` case, around line 29):

```csharp
case DeploymentStatus.Cancelled:
    deployment.BuildDuration = deployment.GetBuildDuration();
    break;
```

**Step 9: Commit**

```bash
cd /Users/milo/milodev/gits/web
git add backend/Api/Endpoints/Projects/Deployments/CancelDeployment/
git add backend/Api/Endpoints/Projects/Deployments/DeploymentsGroup.cs
git add backend/Api.Hosting/Services/IHostingApi.cs
git add backend/Api.Hosting/Services/HostingApi.cs
git add backend/Api.Hosting/Endpoints/Deployments/ChangeDeploymentStatus/ChangeDeploymentStatusCommandHandler.cs
git commit -m "feat: add cancel deployment endpoint and command handler"
```

---

### Task 6: Frontend — Add cancel mutation and button to deployment detail page

**Files:**
- Modify: `/Users/milo/milodev/gits/web/frontend/src/routes/_auth/_dashboard/projects/$projectId/deployments/$deploymentId/index.tsx`

**Step 1: Regenerate API client**

The backend must be running on port 5000 for this.

```bash
cd /Users/milo/milodev/gits/web/frontend
npx openapi-qraft --plugin tanstack-query-react --plugin openapi-typescript http://localhost:5000/swagger/v1/swagger.json --clean -o src/generated --export-type --properties-required-by-default --default-non-nullable
```

**Step 2: Add cancel mutation and button**

After the redeploy mutation (line 132), add:

```typescript
const { mutateAsync: cancelDeploymentAsync, isPending: isCancelling } = api.v1.postProjectsProjectIdDeploymentsDeploymentIdCancel.useMutation();

const handleCancelAsync = async () => {
    await cancelDeploymentAsync({
        path: { projectId, deploymentId }
    });
    refetch();
};
```

Note: Need to destructure `refetch` from the deployment query. Update line 84:
```typescript
const { data: deployment, refetch } = api.v1.getProjectsProjectIdDeploymentsDeploymentId.useQuery(
```

Add the cancel button next to the Redeploy button (around line 180). Replace the single `LoadingButton` with a button group:

```tsx
<div className="flex items-center gap-2">
    {(isDeploying || isQueued) && (
        <LoadingButton
            isLoading={isCancelling}
            onClick={handleCancelAsync}
            variant="destructive"
            size="sm"
        >
            Cancel
        </LoadingButton>
    )}
    <LoadingButton isLoading={isRedeploying} onClick={handleRedeployAsync} variant="outline">
        Redeploy
    </LoadingButton>
</div>
```

Also add `XCircle` to the lucide-react import for the icon (optional, the text "Cancel" is clear enough).

**Step 3: Commit**

```bash
cd /Users/milo/milodev/gits/web
git add frontend/src/routes/_auth/_dashboard/projects/\$projectId/deployments/\$deploymentId/index.tsx
git commit -m "feat: add cancel button to deployment detail page"
```

---

### Task 7: Frontend — Add cancel button to deployment list cards

**Files:**
- Modify: `/Users/milo/milodev/gits/web/frontend/src/routes/_auth/_dashboard/projects/$projectId/deployments/index.tsx`

**Step 1: Add cancel button to DeploymentCard**

Update the `DeploymentCard` component props to accept a cancel handler. The approach: add a small cancel button on the right side of active deployment cards.

Add cancel mutation at the `RouteComponent` level (after line 128):

```typescript
const { mutateAsync: cancelDeploymentAsync } = api.v1.postProjectsProjectIdDeploymentsDeploymentIdCancel.useMutation();
```

Pass it to the card as a prop. Update the DeploymentCard call (around line 195-199):

```tsx
<DeploymentCard
    deployment={deployment}
    projectId={projectId}
    router={router}
    onCancel={async (deploymentId) => {
        await cancelDeploymentAsync({
            path: { projectId, deploymentId }
        });
        refetch();
    }}
/>
```

Update the `DeploymentCard` function signature to accept `onCancel`:

```typescript
function DeploymentCard({ deployment, projectId, router, onCancel }: {
    deployment: { /* ...existing type... */ };
    projectId: string;
    router: ReturnType<typeof useRouter>;
    onCancel: (deploymentId: string) => Promise<void>;
}) {
```

Add a cancel button inside the card. Place it after the time/duration/author section (around line 308, before the closing `</div>` of the flex row). Add it between the time section and the closing tag:

```tsx
{/* Cancel button for active deployments */}
{active && (
    <Button
        variant="ghost"
        size="sm"
        className="text-red-400 hover:text-red-300 hover:bg-red-500/10 ml-2 flex-shrink-0"
        onClick={(e) => {
            e.stopPropagation();
            onCancel(deployment.id!);
        }}
    >
        Cancel
    </Button>
)}
```

Import `XCircle` is optional — text button is cleaner in the compact card.

**Step 2: Commit**

```bash
cd /Users/milo/milodev/gits/web
git add frontend/src/routes/_auth/_dashboard/projects/\$projectId/deployments/index.tsx
git commit -m "feat: add cancel button to deployment list cards"
```

---

### Task 8: Verify and test end-to-end

**Step 1: Build and typecheck**

```bash
cd /Users/milo/milodev/gits/web
cd frontend && npm run typecheck
cd ../backend && dotnet build
```

**Step 2: Run backend tests**

```bash
cd /Users/milo/milodev/gits/web
dotnet test backend/Api.Tests/Api.Tests.csproj
```

**Step 3: Manual test checklist**

1. Start dev servers: `npm run dev`
2. Navigate to a project's deployments page
3. Trigger a deployment via "Deploy from Latest Commit"
4. Verify cancel button appears on the deploying card in the list
5. Click cancel — verify deployment stops and status shows "Cancelled"
6. Verify build logs show "Deployment cancelled by user"
7. Trigger another deployment and cancel from the detail page
8. Verify queued deployments can also be cancelled
9. Verify a new deployment can be started after cancellation

**Step 4: Final commit**

```bash
cd /Users/milo/milodev/gits/web
git add -A
git commit -m "feat: deployment cancellation - final adjustments and cleanup"
```
