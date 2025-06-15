# Frontend Routing with TanStack Router

This document explains the routing system used in the frontend application, focusing on how TanStack Router is implemented and how it integrates with the backend API.

## Overview

The application uses TanStack Router, a file-based routing system for React applications. Routes are defined using special files that export route configurations, and the router is set up to handle authentication and other app-wide concerns.

## Route Structure

Routes are organized in a file-based structure where:

- File names define the route paths
- Special syntax like `$paramName` indicates dynamic path parameters
- Files with underscore prefixes (`_auth`) indicate layout or parent routes
- Each route file exports a `Route` object created with `createFileRoute`

### Example Route Structure

```
/routes
  ├── __root.tsx                 # Root layout
  ├── index.tsx                  # Home page
  ├── /_auth                     # Authentication layout
  │   ├── _dashboard.tsx         # Dashboard layout
  │   ├── logout.tsx             # Logout route
  │   └── /_dashboard            # Dashboard routes
  │       ├── dashboard.tsx      # Dashboard home
  │       ├── settings.tsx       # User settings
  │       └── /projects          # Projects routes
  │           ├── index.tsx      # Projects list
  │           └── /$projectId    # Project details with dynamic parameter
  │               ├── index.tsx  # Project overview
  │               └── /settings  # Project settings
  │                   └── domains.tsx  # Domain settings for project
```

## Route Definition Pattern

Each route file follows this pattern:

```tsx
// filepath: routes/example/$paramName/index.tsx
import { createFileRoute, useParams } from '@tanstack/react-router'

export const Route = createFileRoute('/example/$paramName/')({
  // Route configuration
  component: ExampleComponent,
  
  // Optional loader for data prefetching
  loader: async ({ params }) => {
    return api.v1.getSomeData.prefetchQuery({ 
      path: { paramName: params.paramName } 
    });
  },
})

function ExampleComponent() {
  // Component implementation
  const { paramName } = Route.useParams();
  
  // Use the parameters to fetch or display data
  const { data } = api.v1.getSomeData.useQuery({
    path: { paramName }
  });
  
  return <div>{/* Component JSX */}</div>;
}
```

## Route Configuration

The main router configuration is in `main.tsx`:

```tsx
import { RouterProvider, createRouter } from '@tanstack/react-router'
import { routeTree } from './routeTree.gen'

const router = createRouter({
  routeTree,
  defaultPreload: 'intent',
  context: {
    auth: undefined! // Will be provided later
  }
})

// Type declaration for TypeScript
declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}
```

## Route Parameters and Data Loading

### Accessing Route Parameters

```tsx
const { projectId } = Route.useParams();
// or
const { projectId } = useParams({ from: '/_auth/_dashboard/projects/$projectId/' });
// or
const { projectId } = getRouteApi("/_auth/_dashboard/projects/$projectId").useLoaderData();
```

### Data Prefetching Using Loaders

Loaders prefetch data before navigating to a route:

```tsx
loader: ({ params: { projectId } }) => {
  return api.v1.getProjectsProjectId.prefetchQuery({ 
    path: { projectId } 
  });
}
```

This data can then be accessed in the component using the corresponding query hook:

```tsx
const { data: project } = api.v1.getProjectsProjectId.useQuery({ 
  path: { projectId } 
});
```

## Authentication and Protected Routes

Routes under the `_auth` path are protected and require authentication. The authentication state is provided through the router context:

```tsx
// In main.tsx
const InnerApp = () => {
  const auth = authComponents.useDynamicAuth();
  return <RouterProvider router={router} context={{ auth }} />;
};
```

In protected routes, the auth context is available and can be used to verify authentication status or access user information.

## Nested Layouts

The application uses nested layouts to maintain UI consistency across related routes:

1. `__root.tsx` - The application's root layout with main navigation
2. `_auth/_dashboard.tsx` - Dashboard layout for authenticated users
3. `_auth/_dashboard/projects/$projectId/settings/route.tsx` - Settings tab layout for project settings

This allows for reusing UI components like navigation bars, sidebars, and tabs across related routes.

## Route Navigation

Navigation between routes is handled using the `Link` component or the `navigate` function:

```tsx
// Using Link
import { Link } from '@tanstack/react-router'

<Link to="/projects/$projectId" params={{ projectId: "123" }}>
  Project Details
</Link>

// Using navigate
import { useNavigate } from '@tanstack/react-router'

const navigate = useNavigate()
navigate({ to: '/projects/$projectId', params: { projectId: "123" } })
```
