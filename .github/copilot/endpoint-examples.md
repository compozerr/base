# Endpoint Implementation Examples

This document provides concrete examples of implementing endpoints in the system, following the established patterns.

## Example: Get All Items Endpoint

Here's a complete example of implementing a "Get All" endpoint:

### 1. Command Definition

```csharp
// filepath: /backend/Api/Endpoints/Items/GetAll/GetAllItemsCommand.cs
using Core.MediatR;

namespace Api.Endpoints.Items.GetAll;

public sealed record GetAllItemsCommand() : ICommand<GetAllItemsResponse>;
```

### 2. Response Definition

```csharp
// filepath: /backend/Api/Endpoints/Items/GetAll/GetAllItemsResponse.cs
namespace Api.Endpoints.Items.GetAll;

public sealed record ItemDto(
    string Id,
    string Name,
    string Description,
    DateTime CreatedAt
);

public sealed record GetAllItemsResponse(
    List<ItemDto> Items
);
```

### 3. Command Handler

```csharp
// filepath: /backend/Api/Endpoints/Items/GetAll/GetAllItemsCommandHandler.cs
using Api.Data.Repositories;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Items.GetAll;

public sealed class GetAllItemsCommandHandler(
    IItemRepository itemRepository) : ICommandHandler<GetAllItemsCommand, GetAllItemsResponse>
{
    public async Task<GetAllItemsResponse> Handle(GetAllItemsCommand command, CancellationToken cancellationToken = default)
    {
        var items = await itemRepository.Query()
            .Select(i => new ItemDto(
                i.Id.Value.ToString(),
                i.Name,
                i.Description,
                i.CreatedAt))
            .ToListAsync(cancellationToken);
            
        return new GetAllItemsResponse(items);
    }
}
```

### 4. Route Definition

```csharp
// filepath: /backend/Api/Endpoints/Items/GetAll/GetAllItemsRoute.cs
using MediatR;

namespace Api.Endpoints.Items.GetAll;

public static class GetAllItemsRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddGetAllItemsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<GetAllItemsResponse> ExecuteAsync(
        IMediator mediator)
        => mediator.Send(new GetAllItemsCommand());
}
```

### 5. Group Registration

```csharp
// filepath: /backend/Api/Endpoints/Items/ItemsGroup.cs
using Api.Endpoints.Items.GetAll;
using Api.Endpoints.Items.GetById;
using Api.Endpoints.Items.Create;

namespace Api.Endpoints.Items;

public class ItemsGroup : CarterModule
{
    public ItemsGroup() : base("/items")
    {
        WithTags("Items");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddGetAllItemsRoute();
        app.AddGetItemByIdRoute();
        app.AddCreateItemRoute();
        // Other item routes...
    }
}
```

## Example: Create Item Endpoint

### 1. Command Definition

```csharp
// filepath: /backend/Api/Endpoints/Items/Create/CreateItemCommand.cs
using Core.MediatR;

namespace Api.Endpoints.Items.Create;

public sealed record CreateItemCommand(
    string Name,
    string Description
) : ICommand<CreateItemResponse>;
```

### 2. Response Definition

```csharp
// filepath: /backend/Api/Endpoints/Items/Create/CreateItemResponse.cs
namespace Api.Endpoints.Items.Create;

public sealed record CreateItemResponse(
    string Id,
    string Name,
    string Description
);
```

### 3. Command Handler

```csharp
// filepath: /backend/Api/Endpoints/Items/Create/CreateItemCommandHandler.cs
using Api.Data;
using Api.Data.Repositories;
using Auth.Services;
using Core.MediatR;

namespace Api.Endpoints.Items.Create;

public sealed class CreateItemCommandHandler(
    IItemRepository itemRepository,
    ICurrentUserAccessor currentUserAccessor) : ICommandHandler<CreateItemCommand, CreateItemResponse>
{
    public async Task<CreateItemResponse> Handle(CreateItemCommand command, CancellationToken cancellationToken = default)
    {
        var userId = currentUserAccessor.CurrentUserId!;
        
        var item = new Item
        {
            Name = command.Name,
            Description = command.Description,
            UserId = userId
        };
        
        var createdItem = await itemRepository.AddAsync(item, cancellationToken);
        
        return new CreateItemResponse(
            createdItem.Id.Value.ToString(),
            createdItem.Name,
            createdItem.Description
        );
    }
}
```

### 4. Route Definition

```csharp
// filepath: /backend/Api/Endpoints/Items/Create/CreateItemRoute.cs
using MediatR;

namespace Api.Endpoints.Items.Create;

public sealed record CreateItemRequest(
    string Name,
    string Description
);

public static class CreateItemRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddCreateItemRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<CreateItemResponse> ExecuteAsync(
        CreateItemRequest request,
        IMediator mediator)
        => mediator.Send(
            new CreateItemCommand(
                request.Name,
                request.Description));
}
```

### 5. Validator

```csharp
// filepath: /backend/Api/Endpoints/Items/Create/CreateItemCommandValidator.cs
using FluentValidation;

namespace Api.Endpoints.Items.Create;

public sealed class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
{
    public CreateItemCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");
            
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
    }
}
```

## Example: Get By ID Endpoint

### 1. Command Definition

```csharp
// filepath: /backend/Api/Endpoints/Items/GetById/GetItemByIdCommand.cs
using Api.Abstractions;
using Core.MediatR;

namespace Api.Endpoints.Items.GetById;

public sealed record GetItemByIdCommand(
    ItemId ItemId
) : ICommand<GetItemByIdResponse>;
```

### 2. Response Definition

```csharp
// filepath: /backend/Api/Endpoints/Items/GetById/GetItemByIdResponse.cs
namespace Api.Endpoints.Items.GetById;

public sealed record GetItemByIdResponse(
    string Id,
    string Name,
    string Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
```

### 3. Command Handler

```csharp
// filepath: /backend/Api/Endpoints/Items/GetById/GetItemByIdCommandHandler.cs
using Api.Data.Repositories;
using Core.MediatR;
using Auth.Services;

namespace Api.Endpoints.Items.GetById;

public sealed class GetItemByIdCommandHandler(
    IItemRepository itemRepository,
    ICurrentUserAccessor currentUserAccessor) : ICommandHandler<GetItemByIdCommand, GetItemByIdResponse>
{
    public async Task<GetItemByIdResponse> Handle(GetItemByIdCommand command, CancellationToken cancellationToken = default)
    {
        var userId = currentUserAccessor.CurrentUserId!;
        
        var item = await itemRepository.GetSingleAsync(
            i => i.Id == command.ItemId && i.UserId == userId,
            cancellationToken) ?? throw new ArgumentException($"Item with ID {command.ItemId.Value} not found.");
            
        return new GetItemByIdResponse(
            item.Id.Value.ToString(),
            item.Name,
            item.Description,
            item.CreatedAt,
            item.UpdatedAt
        );
    }
}
```

### 4. Route Definition

```csharp
// filepath: /backend/Api/Endpoints/Items/GetById/GetItemByIdRoute.cs
using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Items.GetById;

public static class GetItemByIdRoute
{
    public const string Route = "{itemId:guid}";

    public static RouteHandlerBuilder AddGetItemByIdRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<GetItemByIdResponse> ExecuteAsync(
        ItemId itemId,
        IMediator mediator)
        => mediator.Send(new GetItemByIdCommand(itemId));
}
```

## Entity and Repository Definitions

### Entity Definition

```csharp
// filepath: /backend/Api.Data/Item.cs
namespace Api.Data;

public class Item : BaseEntityWithId<ItemId>
{
    public UserId UserId { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public virtual User? User { get; set; }
}
```

### Entity ID Definition

```csharp
// filepath: /backend/Api.Abstractions/ItemId.cs
namespace Api.Abstractions;

public sealed record ItemId : IdBase<ItemId>, IId<ItemId>
{
    public ItemId(Guid value) : base(value) { }
    public static ItemId Create(Guid value) => new(value);
    public static ItemId CreateNew() => new(Guid.NewGuid());
    
    // Parameterless constructor for EF Core
    private ItemId() : base(Guid.Empty) { }
}
```

### Repository Interface

```csharp
// filepath: /backend/Api.Data/Repositories/IItemRepository.cs
using Database.Repositories;

namespace Api.Data.Repositories;

public interface IItemRepository : IGenericRepository<Item, ItemId, ApiDbContext>
{
    Task<List<Item>> GetItemsByUserId(UserId userId);
    Task<bool> ItemExistsForUser(ItemId itemId, UserId userId);
}
```

### Repository Implementation

```csharp
// filepath: /backend/Api.Data/Repositories/ItemRepository.cs
using Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Repositories;

public sealed class ItemRepository(
    ApiDbContext context) : GenericRepository<Item, ItemId, ApiDbContext>(context), IItemRepository
{
    public Task<List<Item>> GetItemsByUserId(UserId userId)
        => Query().Where(i => i.UserId == userId).ToListAsync();
        
    public Task<bool> ItemExistsForUser(ItemId itemId, UserId userId)
        => Query().AnyAsync(i => i.Id == itemId && i.UserId == userId);
}
```

### DbContext Registration

```csharp
// filepath: /backend/Api.Data/ApiDbContext.cs
public class ApiDbContext : BaseDbContext<ApiDbContext>
{
    public DbSet<Item> Items => Set<Item>();
    // Other DbSets...
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Item configuration
        modelBuilder.Entity<Item>()
            .HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Repository Registration

```csharp
// filepath: /backend/Api.Data/ApiDataFeature.cs
public class ApiDataFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Database setup...
        
        services.AddScoped<IItemRepository, ItemRepository>();
        // Other repositories...
    }
}
```
