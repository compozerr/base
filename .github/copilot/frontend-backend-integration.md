# Frontend-Backend Integration Patterns

This document explains how the frontend integrates with the backend APIs, focusing on the connection between the two sides of the application.

## Overview

The application uses a clean separation between frontend and backend, with communication happening through RESTful API endpoints. The frontend uses generated TypeScript clients to ensure type safety when calling these endpoints.

## API Client Generation

The frontend uses OpenAPI Qraft to generate type-safe API clients from the backend's OpenAPI specification:

1. The backend generates an OpenAPI specification from the C# endpoints
2. The frontend uses this specification to generate TypeScript clients
3. This ensures full type safety between frontend and backend

### Generated API Structure

The generated API client mimics the backend's API structure:

```typescript
// Example API client usage
api.v1.getProjects.useQuery()                     // GET /api/v1/projects
api.v1.getProjectsProjectId.useQuery({            // GET /api/v1/projects/{projectId}
  path: { projectId: "123" }
})
api.v1.postProjectsProjectId.useMutation({        // POST /api/v1/projects/{projectId}
  path: { projectId: "123" }
})
```

## Integration Examples

### Backend CQRS Command to Frontend Mutation

#### Backend Command

```csharp
// Backend Command (C#)
public class CreateProjectCommand : IRequest<CreateProjectResponse>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ServerTierId ServerTierId { get; set; } = null!;
}
```

#### Frontend Mutation

```typescript
// Frontend Mutation (TypeScript)
const { mutateAsync, isPending } = api.v1.postProjects.useMutation();

// Use the mutation
await mutateAsync({
  body: {
    name: "New Project",
    description: "Project description",
    serverTierId: selectedTierId
  }
});
```

### Backend CQRS Query to Frontend Query

#### Backend Query

```csharp
// Backend Query (C#)
public class GetProjectsQuery : IRequest<GetProjectsResponse>
{
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? SearchTerm { get; set; }
    public ProjectStateFilter? StateFilter { get; set; }
}
```

#### Frontend Query

```typescript
// Frontend Query (TypeScript)
const { data, isLoading } = api.v1.getProjects.useQuery({
  query: {
    page: 1,
    pageSize: 10,
    searchTerm: searchValue,
    stateFilter: selectedFilter
  }
});
```

## Request-Response Flow

### Creating a New Resource

1. Backend defines a command and route:
   ```csharp
   // CreateProjectCommand.cs
   public class CreateProjectCommand : IRequest<CreateProjectResponse>
   {
       public string Name { get; set; } = string.Empty;
       // Other properties...
   }
   
   // CreateProjectResponse.cs
   public class CreateProjectResponse
   {
       public ProjectDto Project { get; set; } = null!;
   }
   
   // CreateProjectRoute.cs
   public class CreateProjectRoute : ICarterModule
   {
       public void AddRoutes(IEndpointRouteBuilder app)
       {
           app.MapPost("/api/v1/projects", async (CreateProjectCommand command, ISender sender) =>
           {
               var result = await sender.Send(command);
               return Results.Ok(result);
           })
           .RequireAuthorization()
           .WithName("CreateProject")
           .WithTags("Projects");
       }
   }
   ```

2. Frontend creates a new resource:
   ```typescript
   // Component using the API
   function CreateProjectForm() {
     const { mutateAsync, isPending } = api.v1.postProjects.useMutation({
       onSuccess: () => {
         // Invalidate queries to refetch data
         api.v1.getProjects.invalidateQueries();
       }
     });
     
     const handleSubmit = async (values) => {
       try {
         const result = await mutateAsync({
           body: values
         });
         
         // Access typed response data
         console.log("Created project ID:", result.project.id);
       } catch (error) {
         console.error("Failed to create project:", error);
       }
     };
     
     // Form implementation...
   }
   ```

### Fetching Resources

1. Backend defines a query and route:
   ```csharp
   // GetProjectByIdQuery.cs
   public class GetProjectByIdQuery : IRequest<GetProjectByIdResponse>
   {
       public ProjectId ProjectId { get; set; } = null!;
   }
   
   // GetProjectByIdResponse.cs
   public class GetProjectByIdResponse
   {
       public ProjectDto Project { get; set; } = null!;
   }
   
   // GetProjectByIdRoute.cs
   public class GetProjectByIdRoute : ICarterModule
   {
       public void AddRoutes(IEndpointRouteBuilder app)
       {
           app.MapGet("/api/v1/projects/{projectId}", async (string projectId, ISender sender) =>
           {
               var result = await sender.Send(new GetProjectByIdQuery
               {
                   ProjectId = new ProjectId(projectId)
               });
               return Results.Ok(result);
           })
           .RequireAuthorization()
           .WithName("GetProjectById")
           .WithTags("Projects");
       }
   }
   ```

2. Frontend fetches a resource:
   ```typescript
   // Component using the API
   function ProjectDetails() {
     const { projectId } = Route.useParams();
     
     // Query with path parameter
     const { data, isLoading, error } = api.v1.getProjectsProjectId.useQuery({
       path: { projectId }
     });
     
     if (isLoading) return <LoadingSpinner />;
     if (error) return <ErrorMessage error={error} />;
     
     // Access typed response data
     return (
       <div>
         <h1>{data.project.name}</h1>
         <p>{data.project.description}</p>
         {/* Other project details */}
       </div>
     );
   }
   ```

## Error Handling

The integration includes consistent error handling patterns:

### Backend Error Response

```csharp
// Backend error handling
app.MapPost("/api/v1/projects", async (CreateProjectCommand command, ISender sender) =>
{
    try
    {
        var result = await sender.Send(command);
        return Results.Ok(result);
    }
    catch (ValidationException ex)
    {
        // Return validation errors
        return Results.BadRequest(new
        {
            errors = ex.Errors.ToDictionary(
                err => err.PropertyName,
                err => err.ErrorMessage)
        });
    }
    catch (Exception ex)
    {
        // Log the error and return a generic message
        return Results.Problem(
            title: "Error creating project",
            detail: ex.Message,
            statusCode: 500
        );
    }
})
```

### Frontend Error Handling

```typescript
// Frontend error handling
const { mutateAsync } = api.v1.postProjects.useMutation();

try {
  await mutateAsync({
    body: formData
  });
  
  toast({
    title: "Success",
    description: "Project created successfully",
  });
} catch (error) {
  // Check for validation errors
  if (error.status === 400 && error.data?.errors) {
    // Handle validation errors
    const validationErrors = error.data.errors;
    Object.keys(validationErrors).forEach(key => {
      form.setError(key, {
        type: "server",
        message: validationErrors[key]
      });
    });
  } else {
    // Handle other errors
    toast({
      title: "Error",
      description: error.message || "Failed to create project",
      variant: "destructive"
    });
  }
}
```

## Authentication Integration

### Backend Authentication

```csharp
// Backend authentication
app.MapGet("/api/v1/projects", async (ClaimsPrincipal user, ISender sender) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    
    var result = await sender.Send(new GetProjectsQuery
    {
        UserId = new UserId(userId)
    });
    
    return Results.Ok(result);
})
.RequireAuthorization()
.WithName("GetProjects")
.WithTags("Projects");
```

### Frontend Authentication

```typescript
// Frontend authentication in main.tsx
const router = createRouter({
  routeTree,
  context: {
    auth: undefined!
  }
});

const AuthWrappedApp = () => {
  const { AuthProvider } = authComponents;
  
  const InnerApp = () => {
    const auth = authComponents.useDynamicAuth();
    return <RouterProvider router={router} context={{ auth }} />;
  };
  
  return (
    <AuthProvider>
      <QueryClientProvider client={queryClient}>
        <InnerApp />
      </QueryClientProvider>
    </AuthProvider>
  );
};
```

## Best Practices for Frontend-Backend Integration

1. **Use Strong Typing**: Always use the generated API client to ensure type safety between frontend and backend

2. **Consistent Error Handling**: Implement consistent error handling patterns across the application

3. **Route Pattern Matching**: When adding a new feature, ensure the frontend route structure logically matches the backend API structure

4. **Data Transformation**: Minimize data transformation between backend and frontend by designing DTOs that can be directly used by the UI

5. **Authentication Flow**: Ensure authentication state is properly propagated to API calls

6. **Cache Invalidation**: When mutating data, invalidate the relevant query cache to ensure data consistency

7. **Request Deduplication**: Use the API client's built-in request deduplication to avoid redundant API calls

8. **Interface Consistency**: Maintain consistent naming conventions between backend endpoints and frontend components

By following these integration patterns, the application maintains a clean, type-safe boundary between frontend and backend code, ensuring robust communication between the two layers.
