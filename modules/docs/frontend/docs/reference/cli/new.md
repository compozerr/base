---
sidebar_position: 2
title: new
---

import CommandBlock from '@site/src/components/CommandBlock';
import FlagTable from '@site/src/components/FlagTable';

# new

Scaffold new compozerr projects and modules.

## new project

### Synopsis

```
compozerr new project <name> [--location <iso>] [--tier <tier>] [--editor <name>] [--no-editor]
```

### Description

Create a new compozerr project. This scaffolds a full-stack application with a .NET backend, React frontend, Docker configuration, and database setup. The project is initialized as a Git repository and linked to a new GitHub repo. Requires authentication, a connected GitHub account, and an active payment method.

### Arguments

<FlagTable flags={[
  { name: "<name>", type: "string", description: "Name for the new project. Used as the directory name and GitHub repository name." }
]} />

### Flags

<FlagTable flags={[
  { name: "--location", type: "string", description: "Server location as an ISO country code (e.g., us, de, sg). Determines where your project will be deployed." },
  { name: "--tier", type: "string", description: "Server tier for hosting. Run compozerr tiers to see available options and pricing." },
  { name: "--editor", type: "string", description: "Open the project in a specific editor after creation (e.g., code, cursor, webstorm)." },
  { name: "--no-editor", type: "boolean", description: "Skip opening an editor after project creation." }
]} />

### Examples

Create a project with defaults (interactive prompts for location and tier):

<CommandBlock command="compozerr new project my-saas" output="Creating project my-saas...
Scaffolding project structure...
Initializing Git repository...
Creating GitHub repository...
Project created successfully!

  cd my-saas
  compozerr dev" />

Create a project with a specific location and tier:

<CommandBlock command="compozerr new project my-api --location de --tier starter" output="Creating project my-api in de (starter tier)...
Scaffolding project structure...
Initializing Git repository...
Creating GitHub repository...
Project created successfully!" />

Create a project and open in VS Code:

<CommandBlock command="compozerr new project my-app --editor code" />

Create a project without opening any editor:

<CommandBlock command="compozerr new project my-app --no-editor" />

---

## new module

### Synopsis

```
compozerr new module <name>
```

### Description

Create a new module within the current project. Modules extend your project with additional backend and frontend functionality. The module is scaffolded with the standard directory structure including a backend feature, frontend package, and `compozerr.json` configuration. Requires authentication, a connected GitHub account, and a clean Git working tree.

### Arguments

<FlagTable flags={[
  { name: "<name>", type: "string", description: "Name for the new module. Used as the directory name under modules/." }
]} />

### Examples

Create a new module:

<CommandBlock command="compozerr new module billing" output="Creating module billing...
Scaffolding module structure at modules/billing/...
Module created successfully!

  modules/billing/backend/Billing/BillingFeature.cs
  modules/billing/frontend/package.json
  modules/billing/compozerr.json" />

---

## Related Commands

- [`add`](/reference/cli/add-remove) -- Add an existing module to your project
- [`deploy`](/reference/cli/deploy) -- Deploy after creating a project
- [`dev`](/reference/cli/dev) -- Start the development environment
- [`tiers`](/reference/cli/tiers) -- View available server tiers and pricing
