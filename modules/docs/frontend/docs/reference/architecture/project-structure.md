---
sidebar_position: 4
title: Project Structure
---

# Project Structure

A Compozerr template project has the following structure:

```
my-app/
├── backend/                    # .NET 9.0 backend
│   ├── Api/                    # Main API project
│   │   └── Program.cs          # Application entry point
│   ├── Core/                   # Shared infrastructure
│   │   └── Feature/            # IFeature interface and auto-discovery
│   │       ├── IFeature.cs     # Feature interface definition
│   │       └── Features.cs     # Feature registration and lifecycle
│   └── Backend.sln             # .NET solution file
│
├── frontend/                   # React + Vite frontend
│   ├── src/
│   │   ├── routes/             # File-based routes (TanStack Router)
│   │   ├── generated/          # Auto-generated API types (don't edit)
│   │   ├── api-client.ts       # API client configuration
│   │   └── main.tsx            # App entry point
│   ├── virtual-route-config.ts # Module route discovery
│   ├── vite.config.ts          # Vite configuration
│   └── package.json
│
├── modules/                    # Feature modules
│   ├── auth/                   # Authentication module
│   ├── database/               # Database module
│   └── ...                     # Other modules
│
├── scripts/                    # Development scripts (Deno)
│   ├── dev.ts                  # Dev server orchestration
│   └── serve.ts                # Production serve script
│
├── compozerr.json              # Project configuration
├── docker-compose.yml          # Container orchestration for deployment
├── Dockerfile.backend          # Backend Docker build (multi-stage)
├── Dockerfile.frontend         # Frontend Docker build (multi-stage)
├── turbo.json                  # Build task configuration (Turborepo)
├── global.json                 # .NET SDK version constraint
└── package.json                # Root workspace configuration
```

## Key files

### `compozerr.json`

Project configuration file. Defines the project name, type, start command, and deployment settings. See [compozerr.json reference](/reference/configuration/compozerr-json).

### `backend/Api/Program.cs`

The .NET application entry point. This is where the web host is configured and features are registered. Features are auto-discovered via reflection -- you rarely need to modify this file.

### `backend/Core/Feature/`

The feature system that powers the module architecture. `IFeature.cs` defines the interface all modules implement. `Features.cs` handles auto-discovery and lifecycle management. You should not need to edit these files.

### `frontend/src/routes/`

File-based routing directory. Each file becomes a route. See [Frontend Architecture](/reference/architecture/frontend-architecture) for naming conventions.

### `frontend/src/generated/`

Auto-generated TypeScript types and API client code from the backend's OpenAPI spec. Do not edit these files manually -- regenerate them when the backend API changes.

### `frontend/src/api-client.ts`

Configures the OpenAPI Qraft client with base URL and authentication. Edit this file to customize API client behavior (e.g., adding auth headers).

### `frontend/virtual-route-config.ts`

Discovers and merges module routes into the main route tree. Rarely needs editing.

### `docker-compose.yml`

Defines the services that run in production -- typically the backend, frontend, and a PostgreSQL database. This file is used during deployment.

### `Dockerfile.backend` and `Dockerfile.frontend`

Multi-stage Docker build files for the backend and frontend. They handle dependency installation, building, and producing minimal production images.

### `turbo.json`

Configures Turborepo task orchestration for the monorepo. Defines build, dev, and other task pipelines with proper dependency ordering.

### `global.json`

Pins the .NET SDK version to ensure consistent builds across machines. The template uses .NET 9.0.

### `scripts/dev.ts`

Deno script that orchestrates the development environment. Starts the backend, frontend, and module services in parallel with proper startup ordering.
