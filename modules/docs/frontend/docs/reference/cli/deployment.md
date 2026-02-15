---
sidebar_position: 4
title: deployment
---

import CommandBlock from '@site/src/components/CommandBlock';
import FlagTable from '@site/src/components/FlagTable';

# deployment

Inspect and monitor your deployments.

## deployment status

### Synopsis

```
compozerr deployment status [deployment-id]
```

### Description

Check the current status of a deployment. If no deployment ID is provided, shows the status of the most recent deployment for the current project.

### Arguments

<FlagTable flags={[
  { name: "[deployment-id]", type: "string", description: "ID of a specific deployment. If omitted, uses the most recent deployment." }
]} />

### Examples

Check the latest deployment:

<CommandBlock command="compozerr deployment status" output="Deployment: dep_x7k9m2
Status: Completed
Started: 2026-02-15 10:32:00 UTC
Finished: 2026-02-15 10:34:12 UTC
URL: https://my-saas.compozerr.app" />

Check a specific deployment:

<CommandBlock command="compozerr deployment status dep_x7k9m2" output="Deployment: dep_x7k9m2
Status: Completed
Started: 2026-02-15 10:32:00 UTC
Finished: 2026-02-15 10:34:12 UTC
URL: https://my-saas.compozerr.app" />

Get status as JSON:

<CommandBlock command="compozerr deployment status --json" output='{"id":"dep_x7k9m2","status":"Completed","startedAt":"2026-02-15T10:32:00Z","finishedAt":"2026-02-15T10:34:12Z","url":"https://my-saas.compozerr.app"}' />

---

## deployment logs

### Synopsis

```
compozerr deployment logs [deployment-id]
```

### Description

View the build and deployment logs for a deployment. If no deployment ID is provided, shows logs for the most recent deployment. Useful for debugging failed deployments or reviewing build output.

### Arguments

<FlagTable flags={[
  { name: "[deployment-id]", type: "string", description: "ID of a specific deployment. If omitted, uses the most recent deployment." }
]} />

### Examples

View latest deployment logs:

<CommandBlock command="compozerr deployment logs" output="[10:32:01] Cloning repository...
[10:32:05] Building Docker image...
[10:32:45] Layer 1/8: FROM mcr.microsoft.com/dotnet/sdk:9.0
[10:32:46] Layer 2/8: COPY backend/ ./backend/
[10:33:20] Layer 8/8: ENTRYPOINT dotnet Api.dll
[10:33:25] Pushing image...
[10:33:50] Starting containers...
[10:34:10] Health check passed
[10:34:12] Deployment complete" />

View logs for a specific deployment:

<CommandBlock command="compozerr deployment logs dep_a3f8k1" />

---

## deployment list

### Synopsis

```
compozerr deployment list [--page <number>] [--status <status>]
```

### Description

List deployments for the current project. Results are paginated and can be filtered by status.

### Flags

<FlagTable flags={[
  { name: "--page", type: "number", description: "Page number for paginated results." },
  { name: "--status", type: "string", description: "Filter by deployment status. One of: Queued, Deploying, Completed, Failed, Cancelled." }
]} />

### Examples

List all deployments:

<CommandBlock command="compozerr deployment list" output="ID            Status     Started              Finished
dep_x7k9m2    Completed  2026-02-15 10:32:00  2026-02-15 10:34:12
dep_w5j3n8    Completed  2026-02-14 15:20:00  2026-02-14 15:22:30
dep_r2m6p4    Failed     2026-02-14 09:10:00  2026-02-14 09:11:45
dep_q1k8v7    Completed  2026-02-13 18:05:00  2026-02-13 18:07:20" />

List only failed deployments:

<CommandBlock command="compozerr deployment list --status Failed" output="ID            Status    Started              Finished
dep_r2m6p4    Failed    2026-02-14 09:10:00  2026-02-14 09:11:45" />

Get the second page of results:

<CommandBlock command="compozerr deployment list --page 2" />

List deployments as JSON:

<CommandBlock command="compozerr deployment list --json" />

---

## deployment latest

### Synopsis

```
compozerr deployment latest
```

### Description

Show the status of the latest deployment along with the last 20 log entries. This is a convenience command that combines `deployment status` and `deployment logs` for the most recent deployment.

### Examples

View the latest deployment:

<CommandBlock command="compozerr deployment latest" output="Deployment: dep_x7k9m2
Status: Completed
Started: 2026-02-15 10:32:00 UTC
Finished: 2026-02-15 10:34:12 UTC
URL: https://my-saas.compozerr.app

--- Last 20 log entries ---
[10:33:20] Layer 8/8: ENTRYPOINT dotnet Api.dll
[10:33:25] Pushing image...
[10:33:50] Starting containers...
[10:34:10] Health check passed
[10:34:12] Deployment complete" />

---

## Related Commands

- [`deploy`](/reference/cli/deploy) -- Trigger a new deployment
- [`ssh`](/reference/cli/ssh) -- Connect to the deployed project
