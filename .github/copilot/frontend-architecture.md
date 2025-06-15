# Frontend Architecture Documentation

This document explains the frontend architecture, focusing on key patterns used for routing, API integration, and component structure. Understanding these patterns will help GitHub Copilot provide more accurate and contextually relevant assistance when working on frontend features.

## Overview

The frontend is built using:

- **React**: As the core UI library
- **TanStack Router**: For routing and navigation
- **TanStack Query**: For data fetching and state management
- **OpenAPI Qraft**: For generating type-safe API clients from OpenAPI specifications
- **Tailwind CSS with Shadcn/UI**: For styling and component library

## Directory Structure

The frontend code follows this basic structure:

```
/frontend
  ├── /src
  │   ├── /components          # Reusable UI components
  │   │   ├── /ui              # Base UI components (from shadcn/ui)
  │   │   └── /project         # Feature-specific components
  │   ├── /generated           # Auto-generated API client code
  │   │   └── /services        # Generated API services
  │   ├── /hooks               # Custom React hooks
  │   ├── /lib                 # Utility functions and constants
  │   ├── /routes              # Route-specific components
  │   │   ├── __root.tsx       # Root route configuration
  │   │   ├── /index.tsx       # Home page route
  │   │   └── /_auth/...       # Authenticated routes
  │   ├── api-client.ts        # API client configuration
  │   └── main.tsx            # Application entry point
  └── vite.config.ts           # Build configuration
```

## Key Technologies and Patterns

1. **TanStack Router**: File-based routing system
2. **TanStack Query with OpenAPI Qraft**: Typed API client generation and data management
3. **Component Architecture**: Composition and organization patterns
4. **Data Fetching Patterns**: Query and mutation patterns
5. **Form Handling**: Form validation and submission

Each of these areas is explained in detail in separate documentation files.
