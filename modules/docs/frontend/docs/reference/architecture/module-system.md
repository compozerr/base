---
sidebar_position: 2
title: Module System
---

# Module System

Modules are self-contained, full-stack features that plug into a Compozerr template project. Each module can contribute backend logic, frontend routes, or both.

## What is a module

A module is a reusable feature package -- authentication, payments, a database layer, or anything else your app needs. Modules are added to your project as Git submodules and live in the `modules/` directory.

Each module has its own `compozerr.json` that declares how it integrates with the project. The backend discovers modules automatically at startup via reflection.

## Module directory structure

```
modules/
  └── auth/
      ├── backend/
      │   ├── Auth/                  # Feature implementation
      │   │   ├── AuthFeature.cs     # IFeature entry point
      │   │   ├── Endpoints/         # Carter endpoint modules
      │   │   ├── Commands/          # MediatR commands and handlers
      │   │   └── Auth.csproj        # Project file
      │   └── Auth.Abstractions/     # Shared interfaces and models
      ├── frontend/                  # @repo/auth package
      │   ├── src/
      │   │   ├── routes/            # TanStack Router routes
      │   │   └── components/        # Reusable React components
      │   └── package.json
      └── compozerr.json             # Module configuration
```

## The IFeature interface

Every backend module implements `IFeature`. This interface has three lifecycle methods that the framework calls during application startup:

```csharp
public class AuthFeature : IFeature
{
    public bool IsEnabled => true;

    void IFeature.ConfigureBuilder(WebApplicationBuilder builder)
    {
        // Configure builder-level settings (rare)
    }

    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // Register services, DbContexts, etc.
        services.AddDbContext<AuthDbContext>();
        services.AddScoped<IAuthService, AuthService>();
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        // Run migrations, seed data, configure middleware
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        context.Database.Migrate();
    }
}
```

Set `IsEnabled` to `false` to disable a module without removing it.

## Backend patterns

### MediatR commands and queries

All business logic is implemented as MediatR commands. This keeps endpoint definitions thin and business logic testable.

Define a command and its handler:

```csharp
// Command
public sealed record CreateUserCommand(string Email, string Password)
    : ICommand<CreateUserResponse>;

// Handler
public sealed class CreateUserCommandHandler
    : ICommandHandler<CreateUserCommand, CreateUserResponse>
{
    public async Task<CreateUserResponse> Handle(
        CreateUserCommand command, CancellationToken ct)
    {
        // Business logic here
    }
}
```

Access the current authenticated user in any handler:

```csharp
var userId = CurrentUserAccessor.CurrentUserId!;
```

### Carter endpoints

Endpoints are defined using Carter modules. Each module maps HTTP routes to MediatR commands:

```csharp
public class AuthEndpoints : CarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/auth/register", async (
            CreateUserCommand cmd, IMediator mediator) =>
                await mediator.Send(cmd));

        app.MapPost("/v1/auth/login", async (
            LoginCommand cmd, IMediator mediator) =>
                await mediator.Send(cmd));
    }
}
```

### FluentValidation

Request validation uses FluentValidation. Validators are automatically discovered and registered:

```csharp
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8);
    }
}
```

## Frontend integration

Modules contribute frontend routes by declaring a `routesDir` in their `compozerr.json`:

```json
{
  "type": "module",
  "name": "auth",
  "frontend": {
    "routesDir": "frontend/src/routes"
  }
}
```

Routes placed in this directory are automatically merged into the main application's route tree via `virtual-route-config.ts`.

Modules can also export reusable components through their `package.json`:

```json
{
  "name": "@repo/auth",
  "exports": {
    "./login-form": "./src/components/login-form.tsx",
    "./auth-provider": "./src/components/auth-provider.tsx"
  }
}
```

Import them in the main app or other modules:

```typescript
import { LoginForm } from "@repo/auth/login-form";
```

## Adding and removing modules

Add a module to your project:

```
compozerr add auth
```

This clones the module as a Git submodule into `modules/auth/` and registers it with the project.

Remove a module:

```
compozerr remove auth
```

This removes the submodule and cleans up references. See the [add/remove CLI reference](/reference/cli/add-remove) for details.
