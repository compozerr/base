# API Client Integration Patterns

This document explains how the frontend integrates with backend APIs using OpenAPI Qraft and TanStack Query for type-safe API calls and efficient data management.

## Overview

The frontend uses a combination of:

- **OpenAPI Qraft**: Generates typed API client code from OpenAPI specifications
- **TanStack Query**: Manages query state, caching, and data fetching

This approach ensures type safety across the frontend-backend boundary and provides powerful data management capabilities.

## API Client Setup

### Configuration

The API client is configured in `api-client.ts`:

```typescript
// filepath: /frontend/src/api-client.ts
import { createAPIClient } from "./generated";
import { OperationSchema, requestFn, RequestFnInfo, RequestFnOptions } from "@openapi-qraft/react";
import { QueryClient } from "@tanstack/react-query";

// Configure QueryClient with default options
export const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            staleTime: 5 * 60 * 1000 // 5 minutes
        }
    }
});

export const apiBaseUrl = import.meta.env.VITE_BACKEND_URL;

// Create API client instance
export const api = createAPIClient({
    requestFn: (schema: OperationSchema, requestInfo: RequestFnInfo, options?: RequestFnOptions) => {
        return requestFn(
            schema,
            { ...requestInfo, credentials: "include" }, // Include credentials for authentication
            { ...options }
        );
    },
    queryClient,
});
```

### Generated API Client Structure

OpenAPI Qraft generates a strongly typed API client from the backend's OpenAPI specification:

```typescript
// Generated API client structure
api.v1.{endpointName}.{operation}({ parameters })
```

Where:
- `v1` represents the API version
- `endpointName` is the endpoint path (e.g., `getProjects`, `postProjectsProjectIdDomains`)
- `operation` is the operation type (`useQuery`, `useMutation`, `prefetchQuery`, etc.)

## Data Fetching Patterns

### Queries (GET operations)

For fetching data from the backend:

```tsx
// Basic query usage
const { data, isLoading, error } = api.v1.getProjectsProjectId.useQuery({
    path: { projectId }
});

// With query options
const { data: project } = api.v1.getProjectsProjectId.useQuery(
    { path: { projectId } },
    {
        // TanStack Query options
        refetchInterval: (project) => 
            project.state.data?.state === ProjectState.Starting ? 2000 : false,
        enabled: Boolean(projectId),
    }
);
```

### Mutations (POST, PUT, DELETE operations)

For modifying data on the backend:

```tsx
// Basic mutation setup
const { mutateAsync, isPending } = api.v1.postProjectsProjectIdDomains.useMutation({
    path: { projectId }
});

// Using the mutation
await mutateAsync({
    body: {
        domain: "example.com",
        serviceName: "web"
    }
});

// With onSuccess handler to update cache
const { mutateAsync } = api.v1.postProjectsProjectIdDomains.useMutation(
    { path: { projectId } },
    {
        onSuccess: () => {
            // Invalidate related queries to refetch data
            api.v1.getProjectsProjectIdDomains.invalidateQueries({
                parameters: { path: { projectId } }
            });
        }
    }
);
```

## Query Invalidation and Refetching

After mutations, related query data should be invalidated to trigger refetching:

```tsx
// Invalidate a specific query
api.v1.getProjectsProjectId.invalidateQueries({
    parameters: { path: { projectId } }
});

// Invalidate all queries for an endpoint
api.v1.getProjects.invalidateQueries();

// Invalidate all queries
queryClient.invalidateQueries();
```

## Prefetching Data

Data can be prefetched for anticipated user interactions:

```tsx
// In route loaders
loader: ({ params: { projectId } }) => {
    return api.v1.getProjectsProjectId.prefetchQuery({ 
        path: { projectId } 
    });
}

// Programmatically
await api.v1.getProjectsProjectId.prefetchQuery({ 
    path: { projectId: nextProjectId } 
});
```

## Error Handling

Error handling follows TanStack Query patterns:

```tsx
const { data, error, isError } = api.v1.getProjectsProjectId.useQuery({
    path: { projectId }
});

if (isError) {
    return <div>Error: {error.message}</div>;
}
```

## Typed Responses and Parameters

API client operations are fully typed based on the OpenAPI spec:

```tsx
// Example typed API call
const { data } = api.v1.getProjectsProjectId.useQuery({ 
    path: { projectId } 
});

// TypeScript knows the shape of data
const projectName = data.name; // Properly typed!

// Parameters are also typed
const { mutateAsync } = api.v1.putProjectsProjectIdChangeTier.useMutation({ 
    path: { projectId } 
});

// TypeScript enforces correct parameter types
await mutateAsync({
    body: {
        tierLevel: "professional" // Checked against allowed values
    }
});
```

This typing system helps catch errors at development time and provides rich autocomplete support.
