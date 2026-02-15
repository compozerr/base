---
sidebar_position: 2
title: Environment Variables
---

# Environment Variables

Environment variables configure your application's runtime behavior -- API keys, database URLs, feature flags, and other settings that change between environments.

## How environment variables work

1. You set environment variables in the [Compozerr dashboard](https://compozerr.com/dashboard) under your project's **Settings > Environment**.
2. When you deploy, the values are pushed to your project's VM.
3. The deployment system writes them to `.env` files in your project directory.
4. Your application reads them at startup.

You never need to manually create or edit `.env` files on the server.

## Where .env files are written

During deployment, the system writes environment variables to the appropriate location based on your project structure:

- `backend/.env` -- Variables consumed by the .NET backend
- `frontend/.env` -- Variables consumed by the Vite frontend
- Project root `.env` -- For projects that use a single `.env` file (e.g., Docker Compose projects)

The system determines the correct location automatically. For the Compozerr full-stack template, backend and frontend variables are written to their respective directories.

## Frontend variables

Frontend variables are embedded into the JavaScript bundle at **build time**. They are not secret -- anyone can view them in the browser.

For **Vite** frontends (the Compozerr template default), prefix variables with `VITE_`:

```
VITE_API_URL=https://api.example.com
VITE_APP_NAME=My App
```

Access them in code:

```typescript
const apiUrl = import.meta.env.VITE_API_URL;
```

For **Next.js** projects, use the `NEXT_PUBLIC_` prefix:

```
NEXT_PUBLIC_API_URL=https://api.example.com
```

## Build-time vs runtime variables

**Runtime variables** are available via `process.env` in your backend code. They are injected into the container at startup and can change between deployments without rebuilding.

**Build-time variables** are baked into the output during the build step. Frontend variables (`VITE_*`, `NEXT_PUBLIC_*`) are always build-time. If you change them, you must redeploy.

For Docker Compose projects, build-time variables must be passed as build args in your `docker-compose.yml`:

```yaml
services:
  frontend:
    build:
      context: .
      dockerfile: Dockerfile.frontend
      args:
        - NEXT_PUBLIC_API_URL=${NEXT_PUBLIC_API_URL}
```

And consumed in the Dockerfile:

```dockerfile
ARG NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
RUN npm run build
```

## Module defaults

Modules can define default environment variables in their `compozerr.json`. These values are used when no override is set in the dashboard:

```json
{
  "type": "module",
  "name": "database",
  "backend": {
    "defaultEnvProperties": {
      "DATABASE_HOST": "localhost",
      "DATABASE_PORT": "5432"
    }
  }
}
```

Dashboard values always take precedence over module defaults.

## Per-branch variables

You can set different environment variables for each branch. This lets you maintain separate configurations for staging and production:

- **main** branch: production database URL, live API keys
- **staging** branch: staging database URL, test API keys

Configure per-branch variables in the dashboard under **Settings > Environment**. Select the branch from the dropdown before adding or editing variables.
