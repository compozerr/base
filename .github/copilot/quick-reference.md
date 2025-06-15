# Quick Reference Guide

This document provides quick reference information for common tasks in both frontend and backend development.

## Frontend Quick Reference

### Adding a New Route

1. Create a file at `src/routes/[path]/index.tsx` (for root routes) or `src/routes/[path]/[subpath].tsx` (for subpages)
2. Define the route using `createFileRoute` and export it:

```tsx
import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/path/')({
  component: PageComponent,
})

function PageComponent() {
  return <div>Content</div>
}
```

### Creating a New API Call

1. Using an existing API endpoint with the generated API client:

```tsx
// For queries (GET)
const { data, isLoading } = api.v1.getEndpoint.useQuery({
  path: { pathParam: value },
  query: { queryParam: value }
});

// For mutations (POST, PUT, DELETE)
const { mutateAsync, isPending } = api.v1.postEndpoint.useMutation();
await mutateAsync({ 
  path: { pathParam: value },
  body: { /* request body */ } 
});
```

### Adding a New Form

1. Define a validation schema using Zod:

```tsx
const formSchema = z.object({
  name: z.string().min(2).max(50),
  email: z.string().email(),
  // More fields...
});
```

2. Use the `useAppForm` hook:

```tsx
const form = useAppForm({
  schema: formSchema,
  defaultValues: { name: '', email: '' },
  onSubmit: async (values) => {
    await api.v1.postEndpoint.mutateAsync({
      body: values
    });
  },
});
```

3. Render the form with form components:

```tsx
<FormProvider {...form}>
  <form onSubmit={form.handleSubmit}>
    <FormField
      control={form.control}
      name="name"
      render={({ field }) => (
        <FormItem>
          <FormLabel>Name</FormLabel>
          <FormControl>
            <Input {...field} />
          </FormControl>
          <FormMessage />
        </FormItem>
      )}
    />
    {/* More fields */}
    <Button type="submit">Submit</Button>
  </form>
</FormProvider>
```

### Adding a New Modal

```tsx
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';

function MyModal() {
  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button>Open Modal</Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Modal Title</DialogTitle>
        </DialogHeader>
        {/* Modal content */}
      </DialogContent>
    </Dialog>
  );
}
```

### Table with Pagination

```tsx
function PaginatedTable() {
  const [page, setPage] = useState(1);
  const { data } = api.v1.getEndpoint.useQuery({
    query: { page, pageSize: 10 }
  });
  
  return (
    <>
      <Table>
        <TableHeader>{/* ... */}</TableHeader>
        <TableBody>
          {data?.items.map(item => (
            <TableRow key={item.id}>
              {/* Row cells */}
            </TableRow>
          ))}
        </TableBody>
      </Table>
      
      <div className="flex justify-between">
        <Button 
          onClick={() => setPage(p => Math.max(1, p-1))}
          disabled={page === 1}
        >
          Previous
        </Button>
        <Button 
          onClick={() => setPage(p => p+1)}
          disabled={page >= (data?.totalPages || 1)}
        >
          Next
        </Button>
      </div>
    </>
  );
}
```

## Backend Quick Reference

### Creating a New Endpoint

1. Create the necessary files:

```
ModuleName/Endpoints/EntityName/Action/
  - ActionCommand.cs (or ActionQuery.cs)
  - ActionResponse.cs
  - ActionCommandHandler.cs (or ActionQueryHandler.cs)
  - ActionRoute.cs
  - [Optional] ActionCommandValidator.cs
```

2. Define the command/query:

```csharp
public class GetEntityQuery : IRequest<GetEntityResponse>
{
    public string Id { get; set; } = string.Empty;
}
```

3. Define the response:

```csharp
public class GetEntityResponse
{
    public EntityDto Entity { get; set; } = null!;
}
```

4. Create the handler:

```csharp
public class GetEntityQueryHandler : IRequestHandler<GetEntityQuery, GetEntityResponse>
{
    private readonly MyDbContext _dbContext;

    public GetEntityQueryHandler(MyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetEntityResponse> Handle(GetEntityQuery request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Entities
            .FirstOrDefaultAsync(e => e.Id == request.Id);

        if (entity == null)
        {
            throw new NotFoundException($"Entity with ID {request.Id} not found");
        }

        return new GetEntityResponse
        {
            Entity = new EntityDto
            {
                Id = entity.Id,
                Name = entity.Name,
                // Map other properties
            }
        };
    }
}
```

5. Define the route:

```csharp
public class GetEntityRoute : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/entities/{id}", async (string id, ISender sender) =>
        {
            var result = await sender.Send(new GetEntityQuery { Id = id });
            return Results.Ok(result);
        })
        .WithName("GetEntity")
        .WithTags("Entities")
        .RequireAuthorization();
    }
}
```

### Adding Validation

1. Create a validator:

```csharp
public class CreateEntityCommandValidator : AbstractValidator<CreateEntityCommand>
{
    public CreateEntityCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
        
        RuleFor(x => x.Description)
            .MaximumLength(500);
        
        // Add more validation rules
    }
}
```

### Adding a New Entity

1. Define the entity class:

```csharp
public class NewEntity
{
    public NewEntityId Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    
    private NewEntity() { }
    
    public NewEntity(NewEntityId id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }
    
    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
```

2. Add an ID value object:

```csharp
public record NewEntityId(string Value)
{
    public static NewEntityId Create() => new(Guid.NewGuid().ToString());
}
```

3. Update the DbContext:

```csharp
public class MyDbContext : DbContext
{
    public DbSet<NewEntity> NewEntities => Set<NewEntity>();
    
    // ... other code
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<NewEntity>(entity =>
        {
            entity.ToTable("NewEntities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasConversion(id => id.Value, value => new NewEntityId(value));
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.Description)
                .HasMaxLength(500);
        });
    }
}
```

4. Add a migration:

```shell
dotnet ef migrations add AddNewEntity --project Api.Data
```

### Adding a New Module

1. Create a module folder structure:

```
modules/
  newmodule/
    backend/
      NewModule/
        NewModuleFeature.cs
        Endpoints/
          EntityGroup.cs
          Entity/
            Create/
            Get/
            Update/
            Delete/
        Services/
    frontend/
      src/
        routes/
        components/
```

2. Define the module Feature class:

```csharp
public class NewModuleFeature : IFeature
{
    public void AddFeature(WebApplicationBuilder builder)
    {
        // Register services
        builder.Services.AddScoped<INewService, NewService>();
    }
    
    public void UseFeature(WebApplication app)
    {
        // Any feature-specific middleware
    }
}
```

3. Register the module in `Program.cs`:

```csharp
builder.AddFeature(new NewModuleFeature());
```

## Common Patterns

### Backend Repository Pattern

```csharp
public interface IEntityRepository
{
    Task<Entity?> GetByIdAsync(EntityId id);
    Task<IEnumerable<Entity>> GetAllAsync();
    Task AddAsync(Entity entity);
    Task UpdateAsync(Entity entity);
    Task DeleteAsync(EntityId id);
}

public class EntityRepository : IEntityRepository
{
    private readonly MyDbContext _dbContext;
    
    public EntityRepository(MyDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<Entity?> GetByIdAsync(EntityId id)
    {
        return await _dbContext.Entities.FirstOrDefaultAsync(e => e.Id == id);
    }
    
    // Implement other methods
}
```

### Frontend Data Fetching Pattern

```tsx
// Basic pattern for queries
function useEntityData(id: string) {
  const { data, isLoading, error } = api.v1.getEntitiesId.useQuery({
    path: { id },
    query: { /* query params */ }
  }, {
    enabled: Boolean(id),
    staleTime: 30000,
    refetchOnWindowFocus: true
  });
  
  return {
    entity: data?.entity,
    isLoading,
    error
  };
}

// Basic pattern for mutations
function useEntityMutation() {
  const { mutateAsync, isPending } = api.v1.postEntities.useMutation({
    onSuccess: () => {
      // Invalidate relevant queries
      api.v1.getEntities.invalidateQueries();
    }
  });
  
  return {
    createEntity: async (data) => {
      return await mutateAsync({
        body: data
      });
    },
    isPending
  };
}
```

Use this quick reference guide as a cheat sheet when implementing common patterns in the application.
