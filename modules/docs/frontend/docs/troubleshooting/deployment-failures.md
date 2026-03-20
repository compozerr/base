---
sidebar_position: 2
title: Deployment Failures
---

import CommandBlock from '@site/src/components/CommandBlock';

# Deployment Failures

When a deployment fails, start by checking the logs, then work through the common causes below.

## Check deployment logs

The first step for any deployment failure:

<CommandBlock command="compozerr deployment logs" />

Look for error-level entries. The logs show each step of the deployment pipeline, so the error message will indicate which stage failed.

## Build failures

Build errors are the most common cause of deployment failures.

**Missing dependencies** -- If a package is used in code but not in your `package.json` or `.csproj`, the build will fail. This often happens when a dependency works locally because it is installed globally.

**Wrong base image** -- If you customized your Dockerfile, ensure the base image includes the tools your build needs (e.g., Node.js, .NET SDK).

**Build arg not set** -- If your Dockerfile expects build arguments (like `NEXT_PUBLIC_*` variables), make sure they are defined in your `docker-compose.yml` and set in the dashboard. See [Environment Variables](/reference/configuration/environment-variables) for details.

## Start command failures

If the build succeeds but the application does not start:

**Wrong command** -- Check the `start` field in `compozerr.json`. The command must be valid on the deployment VM, not just your local machine.

**Missing scripts** -- If your start command references an npm script (like `npm run serve`), verify the script exists in `package.json`.

**Permission issues** -- Ensure shell scripts are executable: `chmod +x script.sh`.

## Missing environment variables

Your application may depend on environment variables that are not set on the server.

Set them in the [compozerr dashboard](https://compozerr.com/projects) under **Settings > Environment** before deploying. Variables set after deployment require a redeploy to take effect.

## Port conflicts in Docker

If your `docker-compose.yml` maps host ports that are already in use on the VM, the container will fail to start. Check for conflicting port mappings in your Docker Compose configuration.

## Timeout

Deployments time out after **15 minutes**. If your build takes longer, optimize your Dockerfile:

- Use **multi-stage builds** to separate build and runtime stages
- Order Dockerfile instructions so that dependency installation comes before code copying -- this enables Docker layer caching
- Add a `.dockerignore` file to exclude `node_modules/`, `.git/`, and other large directories from the build context

## Bun runtime not supported

**Compozerr does not support Bun as a runtime.** If your Dockerfile uses `FROM oven/bun` or your start command uses `bun run`, the container will fail to start on the deployment VM.

The deployment VMs run inside KVM virtualization (Proxmox), and the Bun binary has compatibility issues in this environment. The platform's dependency pipeline also only supports npm.

**Fix:** Replace `oven/bun` with a Node.js base image (e.g., `node:20-alpine`) and rewrite any Bun-specific APIs (`Bun.serve`, `Bun.file`) to use standard Node.js modules.

## Git authentication

If the deployment fails to pull your code, your GitHub connection may have expired. Reconnect your GitHub account in the dashboard under **Settings > GitHub**.
