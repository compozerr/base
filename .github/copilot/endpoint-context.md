# Project Structure for Endpoint Creation

This guide explains how to create new endpoints in the application. Follow this structure when generating code for new endpoints.

## Directory Structure

Endpoints are organized in a feature-based structure:

```
backend/
├── Api/
│   └── Endpoints/
│       └── [Feature]/
│           └── [Action]/
│               ├── [Action]Command.cs
│               ├── [Action]CommandHandler.cs
│               ├── [Action]CommandValidator.cs
│               ├── [Action]Response.cs
│               └── [Action]Route.cs
├── Api.Data/
│   ├── ApiDbContext.cs
│   ├── [Entity].cs
│   └── Repositories/
│       ├── [Entity]Repository.cs
│       └── I[Entity]Repository.cs
├── Api.Abstractions/
│   └── [Entity]Id.cs
```

## DbContext and Entity Structure

The main database context is `ApiDbContext` which inherits from `BaseDbContext<ApiDbContext>`. Here's how entities are defined:

```csharp
// ApiDbContext.cs
public class ApiDbContext : BaseDbContext<ApiDbContext>
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Deployment> Deployments => Set<Deployment>();
    public DbSet<Server> Servers => Set<Server>();
    // Other DbSets...
}
```

Entities use a standard pattern with strongly-typed IDs:

```csharp
// Entity example
public class Server : BaseEntityWithId<ServerId>
{
    public LocationId? LocationId { get; set; }
    public SecretId SecretId { get; set; } = null!;
    public ServerVisibility ServerVisibility { get; set; } = ServerVisibility.Unknown;
    public string MachineId { get; set; } = string.Empty;
    // Other properties...
}
```

## Repository Pattern

The application uses a repository pattern:

```csharp
// Repository interface
public interface IServerRepository : IGenericRepository<Server, ServerId, ApiDbContext>
{
    public Task<Secret> AddNewServer(string newSecret);
    public Task<Server> UpdateServer(/* parameters */);
    public Task<List<Server>> GetServersByLocationId(LocationId locationId);
    // Other specific methods...
}

// Repository implementation
public sealed class ServerRepository : GenericRepository<Server, ServerId, ApiDbContext>, IServerRepository
{
    // Implementation of specific methods...
}
```

## CQRS and MediatR

The application uses CQRS with MediatR. Commands and queries are organized as follows:

```csharp
// Command
public sealed record GetUsersCommand() : ICommand<GetUsersResponse>;

// Response
public sealed record GetUsersResponse(
    List<UserDto> Users
);

// Command Handler
public sealed class GetUsersCommandHandler : ICommandHandler<GetUsersCommand, GetUsersResponse>
{
    public async Task<GetUsersResponse> Handle(GetUsersCommand command, CancellationToken cancellationToken = default)
    {
        // Implementation...
    }
}
```

## Route Definition Pattern

Routes are defined using minimal API endpoints:

```csharp
// Route class
public static class GetUsersRoute
{
    public const string Route = ""; // Relative to group path

    public static RouteHandlerBuilder AddGetUsersRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<GetUsersResponse> ExecuteAsync(
        IMediator mediator)
        => mediator.Send(
            new GetUsersCommand());
}
```

## Group Organization

Endpoints are organized into groups using Carter modules:

```csharp
public class UsersGroup : CarterModule
{
    public UsersGroup() : base("/users")
    {
        WithTags("Users");
        RequireAuthorization(); // If authentication is required
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddGetUsersRoute();
        app.AddCreateUserRoute();
        app.AddUpdateUserRoute();
        // Other routes...
    }
}
```

## Request Validation

FluentValidation is used for command validation:

```csharp
public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");
            
        // Other validation rules...
    }
}
```

## Entity IDs

Strongly-typed IDs are used for entities:

```csharp
// ID definition in Api.Abstractions
public sealed record UserId : IdBase<UserId>, IId<UserId>
{
    public UserId(Guid value) : base(value) { }
    public static UserId Create(Guid value) => new(value);
    public static UserId CreateNew() => new(Guid.NewGuid());
    
    // Parameterless constructor for EF Core
    private UserId() : base(Guid.Empty) { }
}
```

## Available Modules and Features

The application includes the following main modules and features:

- **Api.Data**: Core data models and repositories
- **Auth**: User authentication and management
- **Projects**: Project management
- **Deployments**: Handling deployment operations
- **Servers**: Managing server resources
- **Hosting**: Hosting-related operations

## Example Endpoint Creation

When asked to create a new endpoint (e.g., "Create an endpoint that shows all users"), follow these steps:

1. Identify the appropriate DbContext and entity (e.g., `AuthDbContext` and `User`)
2. Create the appropriate files in the correct structure:
   - Command/Query
   - Response model
   - Command/Query handler
   - Route definition
   - Validator (if needed)
3. Register the route in the appropriate group

## Authentication and Authorization

Most endpoints require authorization:

```csharp
public class UsersGroup : CarterModule
{
    public UsersGroup() : base("/users")
    {
        WithTags("Users");
        RequireAuthorization();
    }
    
    // Route definitions...
}
```

Access the current user in handlers:

```csharp
var userId = CurrentUserAccessor.CurrentUserId!;
```

## Response Models

Response models should follow the pattern:

```csharp
public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string AvatarUrl
);
```

## Command Models

Command models should follow the pattern:

```csharp
public sealed record CreateUserCommand(
    string Name,
    string Email,
    string AvatarUrl
) : ICommand<CreateUserResponse>;
```

## Repository Methods

Common repository methods from GenericRepository:

```
- AddAsync
- UpdateAsync
- DeleteAsync
- GetByIdAsync
- GetFilteredAsync
- ExistsAsync
- CountAsync
```

Additional specific methods can be added to repository interfaces and implemented in the repository classes.
