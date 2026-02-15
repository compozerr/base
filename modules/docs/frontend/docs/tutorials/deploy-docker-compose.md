---
sidebar_position: 1
title: Deploy a Docker Compose Project
---

import CommandBlock from '@site/src/components/CommandBlock';
import Prerequisites from '@site/src/components/Prerequisites';
import ReadMore from '@site/src/components/ReadMore';
import WhatNext from '@site/src/components/WhatNext';

# Deploy a Docker Compose Project

Take any project with a `docker-compose.yml` and deploy it to Compozerr in minutes.

<Prerequisites items={["Compozerr CLI installed and authenticated", "An existing project with a docker-compose.yml file", "Payment method configured in the dashboard"]} />

## 1. Navigate to your project

Open a terminal and `cd` into the directory that contains your `docker-compose.yml`:

<CommandBlock command="cd ~/projects/my-app" />

## 2. Migrate to Compozerr

Run the `migrate` command. The CLI detects your `docker-compose.yml` and walks you through project setup:

<CommandBlock command="compozerr migrate" output="Detected docker-compose.yml
? Project name: my-app
? Region: eu-central
Creating compozerr.json..." />

This generates a `compozerr.json` in your project root.

<ReadMore to="/reference/cli/migrate" title="compozerr migrate" description="Full reference for the migrate command, including flags and non-interactive mode." />

## 3. Review compozerr.json

The generated configuration looks like this:

```json
{
  "type": "project",
  "name": "my-app",
  "templateType": "docker-compose",
  "templateFile": "docker-compose.yml",
  "region": "eu-central"
}
```

Key fields:

- **type** -- Always `"project"` for deployable applications.
- **templateType** -- Set to `"docker-compose"` since the CLI detected your Compose file.
- **templateFile** -- Points to your Compose file. Change this if you use a non-standard filename like `compose.yaml`.

<ReadMore to="/reference/configuration/compozerr-json" title="compozerr.json" description="Full reference for all configuration options in compozerr.json." />

## 4. Set environment variables

If your app requires environment variables, add them in the Compozerr dashboard:

1. Go to [compozerr.com/dashboard](https://compozerr.com/dashboard)
2. Select your project
3. Navigate to **Settings > Environment Variables**
4. Add each variable and save

These variables are injected at deploy time and available to all services in your Compose file.

## 5. Deploy

Push your project to production:

<CommandBlock command="compozerr deploy" output="Deploying my-app...
Uploading project files...
Building services...
Build complete.
Your app is live at https://my-app.compozerr.app" />

## 6. Verify the deployment

Check that everything is running correctly:

<CommandBlock command="compozerr deployment status" output="Status: Completed
URL: https://my-app.compozerr.app
Region: eu-central
Duration: 2m 34s" />

If the status shows `Failed`, check the logs with `compozerr deployment logs` to diagnose the issue.

<ReadMore to="/troubleshooting/deployment-failures" title="Troubleshooting Deployment Failures" description="Common deployment errors and how to fix them." />

<WhatNext items={[
  { title: "Set Up Custom Domains & SSL", to: "/tutorials/custom-domains-ssl", description: "Point your own domain to your deployed project with automatic SSL." },
  { title: "Monitor Deployments", to: "/tutorials/monitor-deployments", description: "Track deployment status, view logs, and debug failures." }
]} />
