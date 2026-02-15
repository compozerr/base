---
sidebar_position: 4
title: Build a Full-Stack App
---

import CommandBlock from '@site/src/components/CommandBlock';
import Prerequisites from '@site/src/components/Prerequisites';
import ReadMore from '@site/src/components/ReadMore';
import WhatNext from '@site/src/components/WhatNext';

# Build a Full-Stack App

Start from scratch with the Compozerr template -- a .NET 9 backend, React frontend, and module system -- then deploy to production.

<Prerequisites items={["Compozerr CLI installed and authenticated", ".NET SDK 9.0", "Node.js >= 18", "Payment method configured in the dashboard"]} />

## 1. Create a new project

Scaffold a full-stack project:

<CommandBlock command="compozerr new project my-app" output="Creating project my-app...
Scaffolding backend...
Scaffolding frontend...
Installing dependencies...
Done! cd my-app to get started." />

## 2. Explore the project structure

The generated project has this layout:

```
my-app/
├── backend/
│   ├── Api/                  # Entry point (Program.cs)
│   ├── Core/                 # Feature system, shared abstractions
│   └── Base.sln              # Solution file
├── frontend/
│   ├── src/
│   │   ├── routes/           # File-based routing (TanStack Router)
│   │   └── generated/        # Auto-generated API client types
│   └── package.json
├── modules/                  # Feature modules (auth, database, etc.)
├── compozerr.json            # Project configuration
└── docker-compose.yml        # Production container setup
```

- **backend/** -- .NET 9 API using Carter for endpoints and MediatR for business logic.
- **frontend/** -- React + Vite with TanStack Router for file-based routing.
- **modules/** -- Self-contained feature modules that contribute backend services and frontend routes.
- **compozerr.json** -- Project metadata, region, and module configuration.

<ReadMore to="/reference/architecture/project-structure" title="Project Structure" description="Detailed breakdown of every directory and file in a Compozerr project." />

## 3. Start development

Launch the dev servers:

<CommandBlock command="compozerr dev" output="Starting backend on http://localhost:5000
Starting frontend on http://localhost:3001
Watching for changes..." />

The frontend runs at **http://localhost:3001** and the backend at **http://localhost:5000**. Both hot-reload on file changes.

<ReadMore to="/reference/cli/dev" title="compozerr dev" description="Full reference for the dev command, including TUI mode and non-interactive options." />

## 4. Add a backend endpoint

Create a new endpoint using Carter. Add a file at `backend/Api/Endpoints/HelloEndpoints.cs`:

```csharp
using Carter;

public class HelloEndpoints : CarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/hello", () => new { message = "Hello from Compozerr!" });
    }
}
```

Save the file and the backend will hot-reload. Test it:

<CommandBlock command="curl http://localhost:5000/v1/hello" output='{"message":"Hello from Compozerr!"}' />

## 5. Regenerate the API client

After adding or changing backend endpoints, regenerate the TypeScript types so the frontend stays in sync:

<CommandBlock command='cd frontend && npx openapi-qraft --plugin tanstack-query-react --plugin openapi-typescript http://localhost:5000/swagger/v1/swagger.json --clean -o src/generated --export-type --properties-required-by-default --default-non-nullable' />

This reads the OpenAPI spec from your running backend and generates typed React Query hooks in `frontend/src/generated/`.

## 6. Use the endpoint in the frontend

Now consume the endpoint from a React component using the generated API client:

```tsx
import { api } from "../api-client";

export function HelloMessage() {
  const { data, isLoading } = api.v1.getHello.useQuery();

  if (isLoading) return <p>Loading...</p>;

  return <p>{data?.message}</p>;
}
```

The API client provides full type safety -- autocomplete, type checking, and error handling all come from the generated types.

## 7. Deploy

When you're ready, push to production:

<CommandBlock command="compozerr deploy" output="Deploying my-app...
Building services...
Build complete.
Your app is live at https://my-app.compozerr.app" />

<ReadMore to="/reference/architecture/module-system" title="Module System" description="Learn how modules add backend features and frontend routes to your project." />

<ReadMore to="/reference/architecture/frontend-architecture" title="Frontend Architecture" description="Deep dive into TanStack Router, the generated API client, and module route integration." />

<WhatNext items={[
  { title: "Create & Publish a Module", to: "/tutorials/create-publish-module", description: "Build a reusable full-stack module and share it across projects." },
  { title: "Set Up Custom Domains & SSL", to: "/tutorials/custom-domains-ssl", description: "Point your own domain to your deployed project with automatic SSL." }
]} />
