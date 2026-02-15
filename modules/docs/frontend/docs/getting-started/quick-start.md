---
sidebar_position: 2
title: Quick Start
---

import CommandBlock from '@site/src/components/CommandBlock';
import Prerequisites from '@site/src/components/Prerequisites';
import ReadMore from '@site/src/components/ReadMore';
import WhatNext from '@site/src/components/WhatNext';

# Quick Start

Go from zero to a deployed app in five minutes.

<Prerequisites items={["Compozerr CLI installed", "GitHub account connected", "Payment method set up"]} />

## 1. Install

If you haven't already, install the CLI:

<CommandBlock command='/bin/bash -c "$(curl -fsSL https://compozerr.com/install.sh)"' />

See [Installation](/getting-started/installation) for details.

## 2. Sign in

Authenticate with your GitHub account:

<CommandBlock command="compozerr login" />

## 3. Create a project

Scaffold a new full-stack project:

<CommandBlock command="compozerr new project my-app" />

This generates a .NET + React project with Docker configuration, database setup, and a ready-to-deploy structure.

## 4. Start development

Launch the dev servers:

<CommandBlock command="compozerr dev" />

The frontend runs at **http://localhost:3001** and the backend at **http://localhost:5000**. Changes hot-reload automatically.

<ReadMore to="/troubleshooting/project-wont-start" title="Troubleshooting: Project Won't Start" description="Fix common issues when the dev environment fails to start." />

## 5. Deploy

Push your app to production:

<CommandBlock command="compozerr deploy" output="Deploying my-app...
Build complete.
Your app is live at https://my-app.compozerr.app" />

Your app is now live with automatic SSL. Custom domains can be configured from the dashboard.

<WhatNext items={[
  { title: "Deploy a Docker Compose Project", to: "/tutorials/deploy-docker-compose", description: "Bring your own Docker Compose file and deploy it to Compozerr." },
  { title: "Build a Full-Stack App", to: "/tutorials/build-fullstack-app", description: "Walk through building and deploying a complete application with the Compozerr template." }
]} />
