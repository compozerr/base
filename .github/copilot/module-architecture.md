# Module Architecture Guide

This document explains the module-based architecture used in this project, focusing on how modules integrate both backend and frontend functionality within the monorepo structure.

## Overview

The project uses a modular architecture where each module:

1. Contains its own backend and frontend code
2. Can be developed and tested separately
3. Exposes components and functionality for use in the main application
4. Can define its own routes that get integrated into the main application

This approach enables a clean separation of concerns while making feature development more maintainable by co-locating related code.

## Module Structure

Each module follows this general structure:

```
modules/
  module-name/
    backend/
      ModuleName/
        ModuleNameFeature.cs              # Module registration for backend
        Endpoints/                        # API endpoints specific to this module
          EntityGroup.cs                  # Route grouping
          Entity/
            Create/                       # CQRS command pattern folders
            Get/
            Update/
        Services/                         # Module-specific services
    frontend/
      package.json                        # Module's package definition and exports
      src/
        index.ts                          # Main entry point that exports components
        module-component.tsx              # Module-specific components
        routes/                           # Module-specific routes
          path/
            index.tsx                     # Route implementations
```

### Real Example: Stripe Module

Here's an actual example of the stripe module structure:

```
modules/
  stripe/
    backend/
      Stripe/
        StripeFeature.cs
        Endpoints/
          Subscriptions/
            CreateSubscription/
              CreateSubscriptionCommand.cs
              CreateSubscriptionCommandHandler.cs
              CreateSubscriptionResponse.cs
              CreateSubscriptionRoute.cs
        Services/
          StripeService.cs
          IStripeService.cs
    frontend/
      package.json                # Defines component exports
      src/
        stripe-component.tsx      # Exportable component
        routes/
          stripe/
            index.tsx             # Stripe route component
```

The package.json for the stripe module exposes components:

```json
{
    "name": "@repo/stripe",
    "version": "0.0.0",
    "private": true,
    "exports": {
        "./stripe-component": "./src/stripe-component.tsx"
    },
    "devDependencies": {
        "@repo/config": "*"
    }
}
```

And a simple component might look like:

```tsx
// modules/stripe/frontend/src/stripe-component.tsx
import React from "react";

interface Props {
   name: string;
}

const StripeComponent = (props: Props) => {
   return (
      <div>
         Hello {props.name}
      </div>
   );
}

export default StripeComponent;
```

## Backend Module Integration

Each module can define its own backend functionality through a Feature class:

```csharp
// Example: modules/stripe/backend/Stripe/StripeFeature.cs
public class StripeFeature : IFeature
{
    public void AddFeature(WebApplicationBuilder builder)
    {
        // Register module-specific services
        builder.Services.AddScoped<IStripeService, StripeService>();
        
        // Configure options
        builder.Services.Configure<StripeOptions>(
            builder.Configuration.GetSection("Stripe"));
    }
    
    public void UseFeature(WebApplication app)
    {
        // Any module-specific middleware
    }
}
```

The main application includes these modules by registering their Feature classes in `Program.cs`:

```csharp
// In backend/Api/Program.cs
builder.AddFeature(new StripeFeature());
```

## Frontend Module Integration

### Component Exports

Modules can export React components for use in the main application:

```json
// Example: modules/stripe/frontend/package.json
{
  "name": "@project/stripe",
  "version": "1.0.0",
  "main": "./src/index.ts",
  "exports": {
    ".": "./src/index.ts",
    "./components": "./src/components/index.ts",
    "./routes": "./src/routes/index.ts"
  }
}
```

These components are then exported from the module's entry point:

```typescript
// Example: modules/stripe/frontend/src/index.ts
export { default as StripeComponent } from './stripe-component';
export { default as SubscriptionsList } from './components/subscriptions-list';
export { default as PaymentMethodForm } from './components/payment-method-form';
```

### Route Integration

Modules can define their own routes that get integrated into the main application's routing system:

```tsx
// Actual example from modules/stripe/frontend/src/routes/stripe/index.tsx
import React from "react"
import { createFileRoute } from '@tanstack/react-router'
import StripeComponent from '../../stripe-component'

export const Route = createFileRoute('/stripe/')({
  component: RouteComponent,
})

function RouteComponent() {
  return (
    <div>
      <StripeComponent name="World!" />
    </div>
  )
}
```

These routes are automatically discovered and integrated through the virtual route configuration system defined in `frontend/virtual-route-config.ts`. When the application builds, it collects all route files from both the main frontend and all module frontends, creating a unified routing system.

## Using Module Components in the Main Application

To use components from a module in the main application:

```tsx
// Example: frontend/src/routes/using-module-component.tsx
import { createFileRoute } from '@tanstack/react-router'
import ExampleComponent from '@repo/template/example-component'

export const Route = createFileRoute('/using-module-component')({
  component: RouteComponent,
})

function RouteComponent() {
  return (
    <div>
      <h3>Using Module Component</h3>
      <ExampleComponent name="World!" />
    </div>
  )
}
```

The import pattern uses the package name as defined in the module's package.json, followed by the export name. This works because the monorepo setup properly resolves these imports to the actual module files.

## Example Module Usage Patterns

### Creating a New Component in a Module

When asked to "create a component in the stripe module to display current subscriptions", here are the steps to follow:

#### Step 1: Create the Component File

Create the component in the module's frontend source directory:

```tsx
// modules/stripe/frontend/src/subscriptions-list.tsx
import { api } from '@/api-client';

interface SubscriptionsListProps {
  projectId: string;
}

export default function SubscriptionsList({ projectId }: SubscriptionsListProps) {
  const { data, isLoading } = api.v1.getProjectsProjectIdSubscriptions.useQuery({
    path: { projectId }
  });
  
  if (isLoading) return <div>Loading subscriptions...</div>;
  
  return (
    <div className="space-y-4">
      <h2 className="text-xl font-bold">Current Subscriptions</h2>
      {data?.subscriptions?.length ? (
        data.subscriptions.map(sub => (
          <div key={sub.id} className="border p-4 rounded-md">
            <div className="font-medium">{sub.name}</div>
            <div className="text-sm text-muted-foreground">Status: {sub.status}</div>
            <div className="text-sm text-muted-foreground">
              Period: {new Date(sub.currentPeriodStart).toLocaleDateString()} to {new Date(sub.currentPeriodEnd).toLocaleDateString()}
            </div>
          </div>
        ))
      ) : (
        <div className="text-muted-foreground">No active subscriptions found.</div>
      )}
    </div>
  );
}
```

#### Step 2: Update the package.json to Export the Component

Add the new component to the module's exports:

```json
// modules/stripe/frontend/package.json
{
    "name": "@repo/stripe",
    "version": "0.0.0",
    "private": true,
    "exports": {
        "./stripe-component": "./src/stripe-component.tsx",
        "./subscriptions-list": "./src/subscriptions-list.tsx"
    },
    "devDependencies": {
        "@repo/config": "*"
    }
}
```

#### Step 3: Use the Component in the Main Application

Import and use the component in a route file of the main application:

```tsx
// frontend/src/routes/_auth/_dashboard/projects/$projectId/index.tsx
import { createFileRoute, getRouteApi } from '@tanstack/react-router'
import { api } from '@/api-client'
import SubscriptionsList from '@repo/stripe/subscriptions-list'

export const Route = createFileRoute('/_auth/_dashboard/projects/$projectId/')({
  component: ProjectDetailsComponent,
})

function ProjectDetailsComponent() {
  const { projectId } = getRouteApi("/_auth/_dashboard/projects/$projectId").useLoaderData();
  const { data: project } = api.v1.getProjectsProjectId.useQuery({ path: { projectId } });

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">{project?.name || 'Project Details'}</h1>
      
      {/* Project information panels */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="space-y-6">
          {/* Project status info */}
          {/* ... */}
        </div>
        
        <div className="space-y-6">
          {/* Using the module component */}
          <SubscriptionsList projectId={projectId} />
        </div>
      </div>
    </div>
  );
}
```

This workflow allows for proper separation of concerns while enabling reuse of module components throughout the application.

### Creating a New Route in a Module

When asked to "create a subscription management route in the stripe module":

1. Create the route in `modules/stripe/frontend/src/routes/stripe/manage/index.tsx`
2. The route will be automatically integrated into the main application routing

```tsx
// modules/stripe/frontend/src/routes/stripe/manage/index.tsx
import { createFileRoute } from '@tanstack/react-router';
import { api } from '@/api-client';

export const Route = createFileRoute('/stripe/manage/')({
  component: ManageSubscriptionsComponent
});

function ManageSubscriptionsComponent() {
  const { data } = api.v1.stripe.getSubscriptions.useQuery();
  
  return (
    <div>
      <h1>Manage Subscriptions</h1>
      {/* Subscription management UI */}
    </div>
  );
}
```

## Module Backend Integration

When creating or extending a module, you might need to implement backend functionality:

```csharp
// Create an endpoint in modules/stripe/backend/Stripe/Endpoints/Subscriptions/GetSubscriptions/
public class GetSubscriptionsQuery : IRequest<GetSubscriptionsResponse>
{
    public ProjectId ProjectId { get; set; } = null!;
}

public class GetSubscriptionsResponse
{
    public List<SubscriptionDto> Subscriptions { get; set; } = new();
}

public class GetSubscriptionsQueryHandler : IRequestHandler<GetSubscriptionsQuery, GetSubscriptionsResponse>
{
    private readonly IStripeService _stripeService;
    
    public GetSubscriptionsQueryHandler(IStripeService stripeService)
    {
        _stripeService = stripeService;
    }
    
    public async Task<GetSubscriptionsResponse> Handle(GetSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var subscriptions = await _stripeService.GetSubscriptionsForProject(request.ProjectId);
        
        return new GetSubscriptionsResponse
        {
            Subscriptions = subscriptions.Select(s => new SubscriptionDto
            {
                Id = s.Id,
                Name = s.Name,
                Status = s.Status
            }).ToList()
        };
    }
}

public class GetSubscriptionsRoute : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/projects/{projectId}/subscriptions", async (string projectId, ISender sender) =>
        {
            var result = await sender.Send(new GetSubscriptionsQuery
            {
                ProjectId = new ProjectId(projectId)
            });
            
            return Results.Ok(result);
        })
        .WithName("GetSubscriptions")
        .WithTags("Subscriptions")
        .RequireAuthorization();
    }
}
```

## Shared Dependencies

Modules can depend on:

1. Core backend libraries (through project references)
2. Core frontend packages (through package.json dependencies)
3. Other modules (through imports)

This allows for code reuse while maintaining modularity.

## Key Module Development Patterns

1. **Module as a Feature**: Each module represents a distinct feature or domain area
2. **Self-Contained**: Modules should contain all related code (both frontend and backend)
3. **Clear Interfaces**: Modules should expose clear interfaces for integration
4. **Consistent Structure**: Follow the same pattern for all modules to make development predictable

## Common Module-related Tasks

### Creating a New Module

1. Create the directory structure: `modules/new-module/backend` and `modules/new-module/frontend`
2. Add backend project files and feature class
3. Add frontend package.json and component exports
4. Register the module in the main application

### Extending an Existing Module

1. Add new components or routes to the module's frontend
2. Export them in the module's index.ts
3. Add new endpoints or services to the module's backend
4. Use the new functionality in the main application

### Testing a Module

1. Backend tests go in `modules/module-name/backend/ModuleName.Tests/`
2. Frontend tests go alongside the component files with `.test.tsx` extensions

## Module Development Workflow

1. Identify which module should contain a new feature
2. Implement backend endpoints in the module's backend folder
3. Implement frontend components in the module's frontend folder
4. Export the components and use them in the main application
5. Register any new backend services in the module's Feature class

By following this guide, you can effectively work with the modular architecture of this project, creating and extending modules while maintaining clean integration with the main application.
