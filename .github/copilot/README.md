# GitHub Copilot Context for Endpoint Creation

This directory contains structured documentation to help GitHub Copilot understand how to create new endpoints and implement features in this codebase.

## Available Documentation

1. [Endpoint Structure and Organization](./endpoint-context.md) - Core structure of endpoints, repositories, and entities
2. [Endpoint Implementation Examples](./endpoint-examples.md) - Complete examples of implementing different types of endpoints
3. [Advanced Patterns and Features](./advanced-patterns.md) - Advanced query patterns and architectural features

## Quick Reference

### Project Architecture

- **Backend Structure**: .NET 9 Minimal API with Carter modules
- **Database**: PostgreSQL with Entity Framework Core
- **Patterns**: CQRS with MediatR, Repository Pattern, Domain Events
- **Authentication**: JWT-based authentication

### Creating New Endpoints

When asked to create a new endpoint, GitHub Copilot should:

1. Identify the appropriate DbContext and entity being queried
2. Create files following the established pattern:
   - Command/Query
   - Response model
   - Command/Query handler
   - Route definition
   - Validator (if needed)
3. Register the route in the appropriate group

### Example Request

Request: "Create an endpoint to get all users"

GitHub Copilot should:
1. Identify that users are managed in `AuthDbContext` and `User` entity
2. Create the following files:
   - `Auth/Endpoints/Users/GetAll/GetAllUsersCommand.cs`
   - `Auth/Endpoints/Users/GetAll/GetAllUsersResponse.cs`
   - `Auth/Endpoints/Users/GetAll/GetAllUsersCommandHandler.cs`
   - `Auth/Endpoints/Users/GetAll/GetAllUsersRoute.cs`
3. Implement proper authorization checks in the handler
4. Return a list of user DTOs with appropriate properties
5. Register the route in `UsersGroup`

### Common Repositories and Entities

- **ApiDbContext**:
  - Deployment
  - Location
  - Module
  - Project
  - ProjectEnvironment
  - ProjectEnvironmentVariable
  - ProjectUsage
  - Server
  - Secret
  - Domain
  
- **AuthDbContext**:
  - User
  - UserLogin

- **GithubDbContext**:
  - GithubUserSettings
  - PushWebhookEvent

## Entity ID Pattern

The project uses strongly-typed IDs for entities:

```csharp
public sealed record UserId : IdBase<UserId>, IId<UserId>
{
    public UserId(Guid value) : base(value) { }
    public static UserId Create(Guid value) => new(value);
    public static UserId CreateNew() => new(Guid.NewGuid());
    
    // Parameterless constructor for EF Core
    private UserId() : base(Guid.Empty) { }
}
```

## Repository Pattern

All repositories implement the generic repository interface:

```csharp
public interface IGenericRepository<TEntity, TEntityId, TDbContext>
    where TEntity : BaseEntityWithId<TEntityId>
    where TEntityId : IdBase<TEntityId>, IId<TEntityId>
    where TDbContext : BaseDbContext<TDbContext>
{
    // Common methods...
}
```

With specific repositories adding domain-specific methods:

```csharp
public interface IUserRepository : IGenericRepository<User, UserId, AuthDbContext>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByAuthProviderUserIdAsync(string authProviderUserId);
    Task<bool> ExistsByAuthProviderUserIdAsync(string authProviderUserId);
}
```
