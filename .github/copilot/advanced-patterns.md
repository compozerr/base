# Advanced Query Patterns and Features

This document provides advanced patterns and features used in the codebase for GitHub Copilot to reference when generating code.

## Complex Queries with Filtering and Including Related Data

```csharp
// Example of complex query with filtering and includes
public async Task<Project> GetProjectByIdWithDomainsAsync(ProjectId projectId)
{
    return await Query()
        .Include(p => p.Domains)
        .FirstOrDefaultAsync(p => p.Id == projectId);
}

// Example with filtering by date range
public async Task<List<ProjectUsage>> GetWeek(ProjectId projectId)
{
    var utcNow = DateTime.UtcNow;
    var startDate = utcNow.AddDays(-7);

    return await Query()
        .Where(x => x.ProjectId == projectId && x.CreatedAtUtc >= startDate && x.CreatedAtUtc <= utcNow)
        .OrderBy(x => x.CreatedAtUtc)
        .ToListAsync();
}
```

## Domain Events

The system uses domain events for cross-cutting concerns. Here's how to use them:

```csharp
// Queueing a domain event on an entity
public async Task<CreateProjectResponse> Handle(CreateProjectCommand command, CancellationToken cancellationToken = default)
{
    var newProject = new Project
    {
        Name = command.RepoName,
        RepoUri = new Uri(command.RepoUrl),
        UserId = userId,
        LocationId = location.Id,
        ServerTierId = ServerTiers.GetById(new ServerTierId(command.Tier)).Id,
        State = ProjectState.Stopped
    };

    // Queue a domain event
    newProject.QueueDomainEvent<ProjectCreatedEvent>();

    var project = await ProjectRepository.AddAsync(newProject, cancellationToken);

    return new CreateProjectResponse(project.Id);
}

// Implementing a domain event handler
public sealed class AllocateDomains_ProjectCreatedEventHandler : IDomainEventHandler<ProjectCreatedEvent>
{
    public Task Handle(ProjectCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Implementation to handle the event
    }
}
```

## Pagination

Implementing pagination in queries:

```csharp
public async Task<PaginatedList<Item>> GetPaginatedItemsAsync(
    int pageNumber, 
    int pageSize,
    CancellationToken cancellationToken = default)
{
    var query = Query();
    
    var totalCount = await query.CountAsync(cancellationToken);
    
    var items = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);
    
    return new PaginatedList<Item>(items, totalCount, pageNumber, pageSize);
}

// PaginatedList class
public class PaginatedList<T>
{
    public List<T> Items { get; }
    public int PageNumber { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }
    
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    
    public PaginatedList(List<T> items, int count, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
        Items = items;
    }
}

// Usage in command handler
public async Task<GetItemsResponse> Handle(GetItemsCommand command, CancellationToken cancellationToken = default)
{
    var paginatedItems = await itemRepository.GetPaginatedItemsAsync(
        command.PageNumber, 
        command.PageSize, 
        cancellationToken);
        
    var itemDtos = paginatedItems.Items
        .Select(i => new ItemDto(/*...*/))
        .ToList();
        
    return new GetItemsResponse(
        itemDtos,
        paginatedItems.PageNumber,
        paginatedItems.TotalPages,
        paginatedItems.TotalCount,
        paginatedItems.HasPreviousPage,
        paginatedItems.HasNextPage);
}
```

## Soft Delete Pattern

The system uses a soft delete pattern for certain entities:

```csharp
// Entity with soft delete
public class Project : BaseEntityWithId<ProjectId>, ISoftDelete
{
    // Regular properties
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

// Query extension to filter out soft deleted entities
public static IQueryable<TEntity> NotDeleted<TEntity>(this IQueryable<TEntity> query)
    where TEntity : class, ISoftDelete
{
    return query.Where(e => !e.IsDeleted);
}

// Usage in repository
public Task<List<Project>> GetActiveProjectsAsync()
{
    return Query()
        .NotDeleted()
        .ToListAsync();
}

// Soft delete implementation
public async Task SoftDeleteAsync(Project project, CancellationToken cancellationToken = default)
{
    project.IsDeleted = true;
    project.DeletedAt = DateTime.UtcNow;
    
    await UpdateAsync(project, cancellationToken);
}
```

## Authorization and Access Control

The system uses a custom authorization approach:

```csharp
// Route with authorization
public class ProjectsGroup : CarterModule
{
    public ProjectsGroup() : base("/projects")
    {
        WithTags(nameof(Projects));
        RequireAuthorization();
    }
}

// Access control in handlers
public async Task<GetProjectResponse> ExecuteAsync(
    ProjectId projectId,
    ICurrentUserAccessor currentUserAccessor,
    IProjectRepository projectRepository)
{
    var project = await projectRepository.GetProjectByIdWithDomainsAsync(projectId)
        ?? throw new ArgumentException("Project not found");

    // Check if user owns the project
    if (project.UserId != currentUserAccessor.CurrentUserId)
    {
        throw new ArgumentException("Project not found");
    }
    
    // Continue with authorized access
}
```

## Working with Configuration Options

The system uses typed configuration options:

```csharp
// Options class
public class JwtOptions
{
    public string Key { get; init; } = null!;
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
}

// Service configuration
services.AddRequiredConfigurationOptions<JwtOptions>("Jwt");

// Using options in a service
public class JsonWebTokenService(IOptions<JwtOptions> options) : IJsonWebTokenService
{
    private readonly JwtOptions _options = options.Value;
    
    public string GenerateToken(/* params */)
    {
        // Use _options.Key, _options.Issuer, etc.
    }
}
```

## Error Handling and Exceptions

The system uses custom exceptions and global error handling:

```csharp
// Custom exception
public class ServerNotFoundException : Exception
{
    public ServerNotFoundException() : base("Server not found")
    {
    }

    public ServerNotFoundException(string message) : base(message)
    {
    }
}

// Using exceptions in handlers
public async Task<GetServerResponse> Handle(GetServerCommand command, CancellationToken cancellationToken = default)
{
    var server = await serverRepository.GetByIdAsync(command.ServerId, cancellationToken)
        ?? throw new ServerNotFoundException($"Server with ID {command.ServerId.Value} not found");
        
    // Continue processing
}
```

## Response Extensions

Common patterns for converting entities to response DTOs:

```csharp
// Extension method
public static class ProjectExtensions
{
    public static ProjectDto ToDto(this Project project)
    {
        return new ProjectDto(
            project.Id.Value.ToString(),
            project.Name,
            project.State.ToString(),
            project.CreatedAt
        );
    }
}

// Usage in handler
public async Task<GetProjectsResponse> Handle(GetProjectsCommand command, CancellationToken cancellationToken = default)
{
    var projects = await projectRepository.GetProjectsByUserIdAsync(
        currentUserAccessor.CurrentUserId!,
        cancellationToken);
        
    return new GetProjectsResponse(
        projects.Select(p => p.ToDto()).ToList()
    );
}
```

## Common Extension Methods

Frequently used extension methods:

```csharp
// Apply an action to each item in a collection
public static void Apply<T>(this IEnumerable<T> items, Action<T> action)
{
    foreach (var item in items)
    {
        action(item);
    }
}

// Usage
deployments.Apply(d => d.Status = DeploymentStatus.Cancelled);

// Throw if null extension
public static T ThrowIfNull<T>(this T? value, string message = "Value cannot be null")
{
    if (value is null)
    {
        throw new ArgumentNullException(message);
    }
    
    return value;
}

// Usage
var user = await userRepository.GetByIdAsync(userId)
    .ThrowIfNull("User not found");
```
