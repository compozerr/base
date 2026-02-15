---
sidebar_position: 5
title: Create & Publish a Module
---

import CommandBlock from '@site/src/components/CommandBlock';
import Prerequisites from '@site/src/components/Prerequisites';
import ReadMore from '@site/src/components/ReadMore';
import WhatNext from '@site/src/components/WhatNext';

# Create & Publish a Module

Build a reusable full-stack module with backend services, endpoints, and frontend routes -- then share it across projects.

<Prerequisites items={["Compozerr CLI installed and authenticated", "An existing Compozerr project (run compozerr new project first)", ".NET SDK 9.0 and Node.js >= 18"]} />

## 1. Scaffold the module

From within your Compozerr project directory, run:

<CommandBlock command="compozerr new module my-module" output="Creating module my-module...
Scaffolding backend feature...
Scaffolding frontend package...
Done!" />

## 2. Explore the module structure

The generated module lives in `modules/my-module/`:

```
modules/my-module/
├── backend/
│   └── MyModule/
│       ├── MyModuleFeature.cs    # IFeature implementation
│       ├── Endpoints/            # Carter endpoint classes
│       ├── Commands/             # MediatR commands and handlers
│       └── MyModule.csproj       # Project file
├── frontend/
│   ├── src/
│   │   ├── routes/               # TanStack Router routes
│   │   └── components/           # Reusable React components
│   └── package.json              # @repo/my-module package
└── compozerr.json                # Module configuration
```

## 3. Implement the backend feature

Open `modules/my-module/backend/MyModule/MyModuleFeature.cs`. The feature class registers your module's services at startup:

```csharp
using Core.Feature;

public class MyModuleFeature : IFeature
{
    public bool IsEnabled => true;

    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IMyModuleService, MyModuleService>();
    }
}
```

The `IsEnabled` property controls whether the module is active. The `ConfigureServices` method is where you register your dependency injection services.

## 4. Add an endpoint

Create an endpoint in `modules/my-module/backend/MyModule/Endpoints/ItemsEndpoints.cs`:

```csharp
using Carter;
using MediatR;

public class ItemsEndpoints : CarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/my-module/items", async (IMediator mediator) =>
            await mediator.Send(new GetItemsCommand()));

        app.MapPost("/v1/my-module/items", async (CreateItemCommand cmd, IMediator mediator) =>
            await mediator.Send(cmd));
    }
}
```

Add the corresponding MediatR command in `Commands/GetItemsCommand.cs`:

```csharp
public sealed record GetItemsCommand : IRequest<List<ItemDto>>;

public sealed class GetItemsCommandHandler : IRequestHandler<GetItemsCommand, List<ItemDto>>
{
    public async Task<List<ItemDto>> Handle(GetItemsCommand command, CancellationToken ct)
    {
        // Your business logic here
        return new List<ItemDto>();
    }
}
```

## 5. Add a frontend route

Create a route file at `modules/my-module/frontend/src/routes/my-module/index.tsx`:

```tsx
import { createFileRoute } from "@tanstack/react-router";
import { api } from "@repo/frontend/api-client";

export const Route = createFileRoute("/my-module/")({
  component: MyModulePage,
});

function MyModulePage() {
  const { data, isLoading } = api.v1MyModule.getMyModuleItems.useQuery();

  if (isLoading) return <p>Loading...</p>;

  return (
    <div>
      <h1>My Module</h1>
      <ul>
        {data?.map((item) => <li key={item.id}>{item.name}</li>)}
      </ul>
    </div>
  );
}
```

Module routes are automatically discovered and merged into the main route tree.

<ReadMore to="/reference/architecture/module-system" title="Module System" description="Deep dive into how modules contribute backend features and frontend routes." />

## 6. Test locally

Start the dev servers to verify everything works:

<CommandBlock command="compozerr dev" />

Visit **http://localhost:3001/my-module** to see your module's frontend route. The backend endpoint is available at **http://localhost:5000/v1/my-module/items**.

## 7. Share with other projects

Modules are managed as git submodules. Other Compozerr projects can add your module:

<CommandBlock command="compozerr add my-module" />

This clones the module into the target project's `modules/` directory and registers it in the project configuration.

<ReadMore to="/reference/cli/new" title="compozerr new" description="Full reference for scaffolding projects and modules." />

<WhatNext items={[
  { title: "Deploy a Docker Compose Project", to: "/tutorials/deploy-docker-compose", description: "Deploy any project with a docker-compose.yml to Compozerr." },
  { title: "Set Up Custom Domains & SSL", to: "/tutorials/custom-domains-ssl", description: "Point your own domain to your deployed project with automatic SSL." }
]} />
