---
sidebar_position: 1
title: GitHub Action
---

import FlagTable from '@site/src/components/FlagTable';
import ReadMore from '@site/src/components/ReadMore';

# GitHub Action

The [`compozerr/github-action`](https://github.com/compozerr/github-action) repository publishes two composite actions for GitHub Actions workflows: `setup` and `deploy`. Both pin to `@v1` for stable major-version updates.

<ReadMore to="/tutorials/deploy-with-github-actions" title="Deploy with GitHub Actions" description="Step-by-step tutorial covering token setup, common workflow patterns, and preview deploys." />

## `compozerr/github-action/deploy` {#compozerrgithub-actiondeploy}

Deploys the project in the working directory. Installs and authenticates the CLI automatically when a `token` input is provided.

```yaml
- uses: compozerr/github-action/deploy@v1
  with:
    token: ${{ secrets.COMPOZERR_API_TOKEN }}
```

In GitHub Actions, the deploy action verifies that the local checkout points at the workflow's triggering `GITHUB_SHA`. If a tag-triggered workflow accidentally checks out `main`, the action corrects the checkout when possible before running `compozerr deploy`.

### Inputs {#inputs}

<FlagTable flags={[
  { name: "token", type: "string", description: "Compozerr API token (cmpz_ prefix). Not required if the setup action was already run in an earlier step of the same job." },
  { name: "environment", type: "string", description: "Environment name to deploy to. Defaults to the project's production environment when omitted." },
  { name: "wait", type: "boolean", default: "false", description: "Wait for the deployment to complete before finishing the step. When false, the step returns as soon as the deployment is queued." },
  { name: "wait-timeout", type: "number", default: "600", description: "Timeout in seconds when wait is true. A timeout is reported as a warning, not a failure -- the deployment may still complete." },
  { name: "working-directory", type: "string", default: ".", description: "Directory containing compozerr.json. Use this when the project lives in a monorepo subdirectory." }
]} />

### Outputs {#outputs}

| Output | Description |
|--------|-------------|
| `deployment-id` | ID of the deployment that was created. |
| `status-url` | URL to track deployment status in the dashboard. |
| `status` | Final deployment status. Only populated when `wait` is `true`. |

## `compozerr/github-action/setup` {#compozerrgithub-actionsetup}

Installs the compozerr CLI on the runner and authenticates it. Use this when you need to run CLI commands beyond `deploy` in the same job.

```yaml
- uses: compozerr/github-action/setup@v1
  with:
    token: ${{ secrets.COMPOZERR_API_TOKEN }}
- run: compozerr deploy --wait
- run: compozerr deployment logs --since 5m
```

### Inputs {#inputs-1}

<FlagTable flags={[
  { name: "token", type: "string", description: "Compozerr API token (cmpz_ prefix). Required." }
]} />

After `setup` runs, the `compozerr` binary is on `PATH` and every subsequent command in the job is authenticated. The `deploy` action in the same job can then omit its own `token` input.

## Versioning {#versioning}

Both actions follow semantic versioning and publish a floating `v1` tag that tracks the latest `1.x` release. Pin to the major version for automatic non-breaking updates:

```yaml
- uses: compozerr/github-action/deploy@v1
```

Pin to a specific SHA if you need byte-for-byte reproducibility:

```yaml
- uses: compozerr/github-action/deploy@<commit-sha>
```

## Source {#source}

The action is open source under MIT. See the [repository](https://github.com/compozerr/github-action) for the action definitions (`deploy/action.yml`, `setup/action.yml`) and to report issues.
