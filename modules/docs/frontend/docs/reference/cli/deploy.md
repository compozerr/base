---
sidebar_position: 3
title: deploy
---

import CommandBlock from '@site/src/components/CommandBlock';
import FlagTable from '@site/src/components/FlagTable';

# deploy

Deploy your project to production.

## Synopsis

```
compozerr deploy [--wait] [--poll-interval <ms>]
```

## Description

Build and deploy the current project. compozerr commits and pushes your code to GitHub, triggers a remote build, and deploys the result to your configured server. The command must be run from within a compozerr project directory. Requires authentication, a connected GitHub account, an active payment method, and a clean Git working tree.

By default, the command returns after the deployment is queued. Use `--wait` to block until the deployment completes or fails.

## Flags

<FlagTable flags={[
  { name: "--wait", type: "boolean", description: "Wait for the deployment to complete before returning. Streams status updates to the terminal." },
  { name: "--poll-interval", type: "number", default: "5000", description: "Polling interval in milliseconds when using --wait. Controls how frequently the CLI checks deployment status." }
]} />

:::tip Global flags
All commands support `--json`, `--quiet`, `--non-interactive`, `--yes`, and `--force-interactive`. See [global flags](/reference/cli/update-reset#global-flags) for details.
:::

## Examples

Deploy and return immediately:

<CommandBlock command="compozerr deploy" output="Deploying my-saas...
Deployment queued: dep_x7k9m2
View status: compozerr deployment status dep_x7k9m2" />

Deploy and wait for completion:

<CommandBlock command="compozerr deploy --wait" output="Deploying my-saas...
Building... (step 1/4)
Pushing image... (step 2/4)
Starting containers... (step 3/4)
Running health checks... (step 4/4)
Deployment complete!
Your app is live at https://my-saas.compozerr.app" />

Deploy with faster polling in CI/CD:

<CommandBlock command="compozerr deploy --wait --poll-interval 2000" />

Deploy non-interactively in a CI pipeline:

<CommandBlock command="compozerr deploy --wait --non-interactive" output="Deploying my-saas...
Building... (step 1/4)
Pushing image... (step 2/4)
Starting containers... (step 3/4)
Running health checks... (step 4/4)
Deployment complete!" />

## Related Commands

- [`deployment status`](/reference/cli/deployment) -- Check the status of a specific deployment
- [`deployment logs`](/reference/cli/deployment) -- View deployment logs
- [`deployment list`](/reference/cli/deployment) -- List all deployments
