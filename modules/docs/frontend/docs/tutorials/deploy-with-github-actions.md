---
sidebar_position: 4
title: Deploy with GitHub Actions
---

import CommandBlock from '@site/src/components/CommandBlock';
import Prerequisites from '@site/src/components/Prerequisites';
import ReadMore from '@site/src/components/ReadMore';
import WhatNext from '@site/src/components/WhatNext';

# Deploy with GitHub Actions

Wire compozerr into your GitHub Actions workflows with the official [`compozerr/github-action`](https://github.com/compozerr/github-action). Deploy automatically on every push, run a staging -> production pipeline, or trigger deploys manually from the Actions UI.

<Prerequisites items={["A compozerr project already created (compozerr new or compozerr migrate)", "Your project pushed to a GitHub repository", "Permission to add repository secrets"]} />

## 1. Generate an API token {#1-generate-an-api-token}

The action needs an API token to authenticate as you.

1. Go to [compozerr.com](https://compozerr.com) and sign in.
2. Navigate to **Settings > API Tokens**.
3. Click **Create Token** and copy the value -- it starts with `cmpz_`.

:::caution
The token is shown only once. Copy it before closing the dialog.
:::

## 2. Store the token as a GitHub secret {#2-store-the-token-as-a-github-secret}

1. In your GitHub repository, open **Settings > Secrets and variables > Actions**.
2. Click **New repository secret**.
3. Name it `COMPOZERR_API_TOKEN` and paste the token you copied.

## 3. Add a deploy workflow {#3-add-a-deploy-workflow}

Create `.github/workflows/deploy.yml` in your repository:

```yaml
name: Deploy
on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: compozerr/github-action/deploy@v1
        with:
          token: ${{ secrets.COMPOZERR_API_TOKEN }}
```

Push the workflow to GitHub. The next push to `main` triggers a deploy.

## 4. Watch the deployment run {#4-watch-the-deployment-run}

Open the **Actions** tab in your GitHub repository, click the latest run, and expand the `deploy` job. The action prints a `status-url` annotation linking to the live deployment in the compozerr dashboard.

## Common workflow patterns {#common-workflow-patterns}

### Staging -> production pipeline {#staging---production-pipeline}

Deploy to staging first, then promote to production only if staging succeeds:

```yaml
name: Deploy Pipeline
on:
  push:
    branches: [main]

jobs:
  staging:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: compozerr/github-action/deploy@v1
        with:
          token: ${{ secrets.COMPOZERR_API_TOKEN }}
          environment: staging
          wait: 'true'

  production:
    runs-on: ubuntu-latest
    needs: staging
    steps:
      - uses: actions/checkout@v4
      - uses: compozerr/github-action/deploy@v1
        with:
          token: ${{ secrets.COMPOZERR_API_TOKEN }}
          environment: production
```

### Preview deploys on pull requests {#preview-deploys-on-pull-requests}

Deploy to `staging` when a pull request is opened, and to `production` when it merges to `main`:

```yaml
name: Deploy
on:
  pull_request:
    branches: [main]
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: compozerr/github-action/deploy@v1
        with:
          token: ${{ secrets.COMPOZERR_API_TOKEN }}
          environment: ${{ github.event_name == 'pull_request' && 'staging' || 'production' }}
```

### Manual deploy with an environment picker {#manual-deploy-with-an-environment-picker}

Trigger deploys by hand from the Actions UI and choose which environment to hit:

```yaml
name: Manual Deploy
on:
  workflow_dispatch:
    inputs:
      environment:
        description: 'Environment to deploy to'
        required: true
        default: 'production'
        type: choice
        options:
          - staging
          - production

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: compozerr/github-action/deploy@v1
        with:
          token: ${{ secrets.COMPOZERR_API_TOKEN }}
          environment: ${{ inputs.environment }}
```

### Production deploys from version tags {#production-deploys-from-version-tags}

Deploy immutable release tags to production:

```yaml
name: Deploy Production
on:
  push:
    tags:
      - 'v*'

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: compozerr/github-action/deploy@v1
        with:
          token: ${{ secrets.COMPOZERR_API_TOKEN }}
```

Do not set `ref: main` on `actions/checkout` in a tag-triggered workflow. The deploy action verifies the checkout against the triggering commit and corrects it when possible, so the deployment records and builds the pushed tag commit.

### Run custom CLI commands alongside deploy {#run-custom-cli-commands-alongside-deploy}

Use the `setup` action when you need more than just `deploy` -- for example, to run migrations, fetch logs, or inspect state in the same job:

```yaml
name: Deploy
on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: compozerr/github-action/setup@v1
        with:
          token: ${{ secrets.COMPOZERR_API_TOKEN }}
      - run: compozerr deploy --wait
      - run: compozerr deployment logs --since 5m
```

Once `setup` has authenticated the CLI, subsequent `deploy` action steps in the same job can omit the `token` input.

## Using action outputs {#using-action-outputs}

The `deploy` action exposes three outputs you can chain into later steps:

| Output | Description |
|--------|-------------|
| `deployment-id` | ID of the deployment that was created. |
| `status-url` | Dashboard URL to watch or share. |
| `status` | Final deployment status (only populated when `wait: 'true'`). |

Example -- post the dashboard URL as a PR comment:

```yaml
- uses: compozerr/github-action/deploy@v1
  id: deploy
  with:
    token: ${{ secrets.COMPOZERR_API_TOKEN }}
    environment: staging
    wait: 'true'

- uses: actions/github-script@v7
  if: github.event_name == 'pull_request'
  with:
    script: |
      github.rest.issues.createComment({
        issue_number: context.issue.number,
        owner: context.repo.owner,
        repo: context.repo.repo,
        body: `Staging deploy: ${{ steps.deploy.outputs.status-url }}`
      });
```

<ReadMore to="/reference/cli/deploy" title="compozerr deploy" description="Full flag reference for the underlying CLI command the action runs." />

<WhatNext items={[
  { title: "Monitor Deployments", to: "/tutorials/monitor-deployments", description: "Track status, view logs, and debug failures after the action hands off." },
  { title: "Set Up Custom Domains & SSL", to: "/tutorials/custom-domains-ssl", description: "Point your own domain at the project you just automated." }
]} />
