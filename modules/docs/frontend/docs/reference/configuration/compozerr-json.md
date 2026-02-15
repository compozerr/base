---
sidebar_position: 1
title: compozerr.json
---

# compozerr.json

Every compozerr project and module has a `compozerr.json` file at its root. This file tells the CLI and deployment system how to build, run, and configure your project.

There are two types: **project** configuration and **module** configuration.

## Project configuration

A project-level `compozerr.json` is created when you run `compozerr new project`. It defines how the project is deployed.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | `"project"` | Yes | Must be `"project"` |
| `name` | string | Yes | Project name. Also used as the GitHub repository name. |
| `id` | string (UUID) | Auto | Auto-generated project ID. Do not change this. |
| `start` | string | No | Custom start command. Overrides the default `npm run serve -- -d`. |
| `cwd` | string | No | Working directory for the start command, relative to the project root. |
| `templateType` | `"docker-compose"` \| `"dockerfile"` | No | Deployment template type. Auto-detected by the CLI if not set. |
| `templateFile` | string | No | Path to the Dockerfile or docker-compose.yml used for deployment. |

## Module configuration

A module-level `compozerr.json` defines how a module integrates with the project. Modules are self-contained features that can have backend, frontend, or both.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | `"module"` | Yes | Must be `"module"` |
| `name` | string | Yes | Module name |
| `dependencies` | object | No | Map of module dependencies |
| `start` | string | No | Command to start the module during development |
| `port` | string | No | Port the module listens on |
| `readyMessage` | string | No | String to match in stdout to detect when the module is ready |
| `startupTimeoutMs` | number | No | How long to wait for the ready message before timing out (milliseconds) |
| `frontend.routesDir` | string | No | Path to the directory containing frontend routes |
| `backend.defaultEnvProperties` | object | No | Default environment variables for the backend |

## Examples

### Minimal project

The simplest valid project configuration:

```json
{
  "type": "project",
  "name": "my-app",
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

### Docker Compose project

A project that deploys using Docker Compose:

```json
{
  "type": "project",
  "name": "my-app",
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "templateType": "docker-compose",
  "templateFile": "docker-compose.yml"
}
```

### Custom start command

A project with a custom start command and working directory:

```json
{
  "type": "project",
  "name": "my-app",
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "start": "docker compose up -d",
  "cwd": "dashboard"
}
```

### Module with frontend routes

A module that contributes routes to the frontend:

```json
{
  "type": "module",
  "name": "auth",
  "frontend": {
    "routesDir": "frontend/src/routes"
  }
}
```

### Module with full configuration

A module with startup detection, timeout, and a custom start command:

```json
{
  "type": "module",
  "name": "database",
  "start": "docker compose up -d",
  "port": "5432",
  "readyMessage": "database system is ready",
  "startupTimeoutMs": 30000
}
```

The `readyMessage` field is matched against the module's stdout. When the string appears, the CLI considers the module started and moves on. If the message does not appear within `startupTimeoutMs`, the CLI reports a timeout error.
