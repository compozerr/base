---
sidebar_position: 6
title: migrate
---

import CommandBlock from '@site/src/components/CommandBlock';
import FlagTable from '@site/src/components/FlagTable';

# migrate

Migrate an existing project to Compozerr.

## Synopsis

```
compozerr migrate [name] [--location <iso>] [--tier <tier>] [--skip-docker-setup] [--redetect]
```

## Description

Convert an existing project into a Compozerr-managed project. The CLI analyzes your project structure, detects the technology stack, generates the necessary Docker and Compozerr configuration, and sets up deployment infrastructure. Requires authentication, a connected GitHub account, an active payment method, and a clean Git working tree.

The migration process:

1. Detects your project template (e.g., Node.js, .NET, static site, Docker Compose)
2. Generates a `Dockerfile` and `docker-compose.yml` if needed
3. Creates a `compozerr.json` configuration file
4. Links the project to Compozerr for deployment

## Arguments

<FlagTable flags={[
  { name: "[name]", type: "string", description: "Name for the migrated project. If omitted, the current directory name is used." }
]} />

## Flags

<FlagTable flags={[
  { name: "--location", type: "string", description: "Server location as an ISO country code (e.g., us, de, sg)." },
  { name: "--tier", type: "string", description: "Server tier for hosting. Run compozerr tiers to see available options." },
  { name: "--skip-docker-setup", type: "boolean", description: "Skip Docker configuration generation. Use when your project already has a working Dockerfile and docker-compose.yml." },
  { name: "--redetect", type: "boolean", description: "Force re-detection of the project template, overwriting any previously detected configuration." }
]} />

## Examples

Migrate the current project with interactive prompts:

<CommandBlock command="compozerr migrate" output="Detecting project template...
Detected: Node.js (Next.js)
Generating Docker configuration...
Creating compozerr.json...
Migration complete!

  compozerr deploy" />

Migrate with a specific name and location:

<CommandBlock command="compozerr migrate my-legacy-app --location us --tier starter" output="Detecting project template...
Detected: Node.js (Express)
Generating Docker configuration...
Creating compozerr.json...
Migration complete!" />

Migrate a project that already has Docker setup:

<CommandBlock command="compozerr migrate --skip-docker-setup" output="Skipping Docker configuration generation...
Creating compozerr.json...
Migration complete!" />

Re-run template detection on a previously migrated project:

<CommandBlock command="compozerr migrate --redetect" output="Re-detecting project template...
Detected: Docker Compose
Updating compozerr.json...
Migration updated!" />

## Related Commands

- [`deploy`](/reference/cli/deploy) -- Deploy after migration
- [`dev`](/reference/cli/dev) -- Start developing the migrated project
- [`tiers`](/reference/cli/tiers) -- View available server tiers
