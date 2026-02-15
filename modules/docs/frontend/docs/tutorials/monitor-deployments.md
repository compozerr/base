---
sidebar_position: 8
title: Monitor Deployments
---

import CommandBlock from '@site/src/components/CommandBlock';
import Prerequisites from '@site/src/components/Prerequisites';
import ReadMore from '@site/src/components/ReadMore';
import WhatNext from '@site/src/components/WhatNext';

# Monitor Deployments

Track deployment status, view logs, and diagnose failures from the command line.

<Prerequisites items={["Compozerr CLI installed and authenticated", "A Compozerr project with at least one deployment"]} />

## 1. Check the latest deployment

Get a quick overview of your most recent deployment:

<CommandBlock command="compozerr deployment latest" output="Deployment: dep_abc123
Status: Completed
Branch: main
Commit: feat: add user dashboard
Region: eu-central
Duration: 2m 34s
URL: https://my-app.compozerr.app

Recent logs:
[10:00:01] INFO  Uploading project files...
[10:00:15] INFO  Building services...
[10:01:42] INFO  Starting containers...
[10:02:34] SUCCESS  Deployment complete" />

This shows the deployment status and the last 20 log entries.

## 2. Get details for a specific deployment

If you need full details for a particular deployment, pass its ID:

<CommandBlock command="compozerr deployment status dep_abc123" output="Deployment: dep_abc123
Status: Completed
Branch: main
Commit: a1b2c3d (feat: add user dashboard)
Region: eu-central
Started: 2026-02-15 10:00:01 UTC
Finished: 2026-02-15 10:02:34 UTC
Duration: 2m 34s
URL: https://my-app.compozerr.app" />

## 3. View full deployment logs

For the complete timestamped log output:

<CommandBlock command="compozerr deployment logs dep_abc123" output="[10:00:01] INFO     Uploading project files...
[10:00:15] INFO     Building services...
[10:00:16] INFO     Step 1/8: FROM node:20-alpine AS deps
[10:00:45] INFO     Step 5/8: RUN npm ci
[10:01:20] INFO     Step 8/8: CMD [&quot;node&quot;, &quot;server.js&quot;]
[10:01:42] INFO     Starting containers...
[10:02:30] INFO     Health check passed
[10:02:34] SUCCESS  Deployment complete" />

Log entries include severity levels: **Info**, **Warning**, **Error**, and **Success**. Look for **Error** entries to diagnose failures.

## 4. List all deployments

View a paginated list of all deployments for your project:

<CommandBlock command="compozerr deployment list" output="ID            STATUS      BRANCH  COMMIT          CREATED
dep_abc123    Completed   main    feat: add ...   2 hours ago
dep_xyz789    Completed   main    fix: update..   1 day ago
dep_def456    Failed      main    chore: bump..   2 days ago" />

Filter by status to find specific deployments:

<CommandBlock command="compozerr deployment list --status Failed" output="ID            STATUS    BRANCH  COMMIT          CREATED
dep_def456    Failed    main    chore: bump..   2 days ago" />

## 5. Machine-readable output for CI/CD

Use the `--json` flag for machine-readable output, useful in CI/CD pipelines or scripts:

<CommandBlock command="compozerr deployment latest --json" output='{
  "id": "dep_abc123",
  "status": "Completed",
  "branch": "main",
  "region": "eu-central",
  "url": "https://my-app.compozerr.app"
}' />

You can combine this with tools like `jq` to extract specific fields:

<CommandBlock command="compozerr deployment latest --json | jq -r '.status'" output="Completed" />

<ReadMore to="/reference/cli/deployment" title="compozerr deployment" description="Full reference for all deployment commands and flags." />

<WhatNext items={[
  { title: "SSH Into Your Project", to: "/tutorials/ssh-into-project", description: "Open a terminal into your deployed project for debugging and inspection." },
  { title: "Set Up Custom Domains & SSL", to: "/tutorials/custom-domains-ssl", description: "Point your own domain to your deployed project with automatic SSL." }
]} />
