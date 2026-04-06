# Deployment Cancellation Design

**Date:** 2026-02-27
**Status:** Approved
**Scope:** Hosting manager, web backend, web frontend

## Problem

Deployments cannot be cancelled once started. The hosting manager runs a monolithic `executeDeployment` function with no cancellation checkpoints. Users must wait for the deployment to complete or timeout (15 minutes).

## Design Decisions

- **Cancel scope:** Leave VM as-is when cancelled. No rollback, no cleanup. Safe and simple.
- **Cancel points:** Both queued and actively running deployments can be cancelled.
- **Mechanism:** AbortController-based step pipeline in the hosting manager.
- **UI:** Cancel button on both deployment list cards and detail page. No confirmation dialog (cancellation is safe and recoverable).

## Hosting Manager: Step Pipeline

Refactor `DeploymentQueueService.executeDeployment()` into discrete steps. Each step is a named function receiving an `AbortSignal`. Between each step, check `signal.aborted`.

### Steps

| # | Step | Description | Can interrupt mid-step? |
|---|------|-------------|------------------------|
| 1 | markDeploying | Update status to "deploying" via API | No (fast) |
| 2 | provisionVm | Get or create VM | Between sub-operations |
| 3 | verifyVm | Check VM is running | No (fast) |
| 4 | configureDomains | Write Traefik config | No (fast) |
| 5 | waitForVm | Poll until QEMU agent ready | Yes (abort polling loop) |
| 6 | cloneRepo | Git clone/fetch inside VM | Between git commands |
| 7 | installDeps | npm install / deno install | Between commands |
| 8 | setupEnv | Write .env files | No (fast) |
| 9 | startServices | docker-compose up | No (must complete) |
| 10 | verifyServices | Check containers running | No (fast) |
| 11 | markCompleted | Update status to "completed" | No (fast) |

### Cancellation Flow

```
signal.aborted === true
    ↓
Log "Deployment cancelled by user at step: {stepName}"
    ↓
Mark deployment status as "Cancelled" via API callback
    ↓
Stop pipeline, do not run remaining steps
    ↓
Leave VM in current state
```

### New Endpoint

`POST /projects/cancel`

Request body:
```json
{ "deploymentId": "string" }
```

Behavior:
- Queued deployment: remove from queue, callback "Cancelled"
- Active deployment: call `abortController.abort()`, pipeline stops at next step boundary
- Not found: return 404

### AbortController Tracking

```typescript
// In DeploymentQueueService
private abortControllers: Map<string, AbortController> = new Map();

// When job starts:
const controller = new AbortController();
this.abortControllers.set(job.deploymentId, controller);

// When cancel requested:
const controller = this.abortControllers.get(deploymentId);
controller?.abort();

// When job finishes (any reason):
this.abortControllers.delete(job.deploymentId);
```

### Step Runner Pattern

```typescript
interface DeploymentStep {
  name: string;
  execute: (context: DeploymentContext, signal: AbortSignal) => Promise<void>;
}

async function runPipeline(steps: DeploymentStep[], context: DeploymentContext, signal: AbortSignal) {
  for (const step of steps) {
    if (signal.aborted) {
      await logCancellation(context, step.name);
      await markCancelled(context);
      return;
    }
    await step.execute(context, signal);
  }
}
```

## Web Backend

### New Endpoint

`POST /v1/projects/{projectId}/deployments/{deploymentId}/cancel`

### CancelDeploymentCommand

Handler logic:
1. Load deployment, verify it belongs to project
2. Verify status is `Queued` or `Deploying` (reject otherwise)
3. If `Queued`: update status to `Cancelled` in DB directly
4. If `Deploying`: call `HostingApi.CancelDeploymentAsync(deploymentId)`
5. Log "Deployment cancelled by user"
6. Trigger `DeploymentTriggerCommand` to process next queued deployment

### New HostingApi Method

```csharp
Task CancelDeploymentAsync(DeploymentId deploymentId);
// POST /projects/cancel { deploymentId }
// 30-second timeout
```

## Frontend

### Deployment List Page

- Add "Cancel" button to each deployment card with status `Queued` or `Deploying`
- Button style: destructive/red, small size to fit in card layout
- On click: call cancel endpoint, refetch deployment list

### Deployment Detail Page

- Add "Cancel" button next to "Redeploy" button
- Only visible when status is `Queued` or `Deploying`
- After cancel: logs update with cancellation message, status badge shows `Cancelled`

### Status Styling

`Cancelled` status already has zinc-500 color styling in the frontend. No changes needed.

## Build Logs on Cancellation

When cancelled, the hosting manager appends:
```
[WARNING] Deployment cancelled by user
[INFO] Deployment stopped at step: {stepName}
```

## State Management

- `deployment-state.json` schema unchanged. Cancelled deployments removed from active/pending.
- Recovery on restart: orphaned deployments marked as failed (unchanged behavior).

## Edge Cases

- **Cancel during docker-compose up:** The step completes (non-interruptible), then pipeline stops before `verifyServices`. Services may be partially started — user can redeploy.
- **Cancel while queued on hosting manager:** Job removed from queue. If web backend had already sent to hosting, the cancel endpoint handles it.
- **Double cancel:** Second cancel is a no-op (deployment already cancelled or gone).
- **Cancel then immediate redeploy:** Works normally — new deployment enters queue.
- **Service crash during cancel:** Recovery service marks deployment as failed on restart. Acceptable.
