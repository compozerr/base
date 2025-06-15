# Component Patterns

This document outlines the component patterns and best practices used in the frontend application, focusing on component organization, reusability, and integration with data fetching.

## Component Organization

The frontend application organizes components into several categories:

1. **UI Components**: Basic, reusable UI elements (`/components/ui/`)
2. **Feature Components**: Domain-specific components for particular features
3. **Layout Components**: Components defining the structure of the application
4. **Route Components**: Components specific to routes, usually combining other components
5. **Form Components**: Specialized components for handling forms and user input

## UI Components

These are low-level, reusable UI components built with Shadcn/UI:

```typescript
// Example UI component
// filepath: /components/ui/button.tsx
import * as React from "react"
import { Slot } from "@radix-ui/react-slot"
import { cn } from "@/lib/utils"

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  asChild?: boolean,
  variant?: "default" | "destructive" | "outline" | "secondary",
  size?: "default" | "sm" | "lg" | "icon",
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = "default", size = "default", asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : "button"
    return (
      <Comp
        className={cn(/*...styles*/)}
        ref={ref}
        {...props}
      />
    )
  }
)
Button.displayName = "Button"

export { Button }
```

## Feature Components

These components implement domain-specific functionality:

```tsx
// Example feature component
// filepath: /components/project/project-startstop-button.tsx
import { api } from '@/api-client'
import { Button } from '@/components/ui/button'
import { ProjectState } from '@/lib/project-state'
import { useCallback } from 'react'

interface StartStopProjectButtonProps {
  projectId: string
  state: ProjectState
}

export default function StartStopProjectButton({ projectId, state }: StartStopProjectButtonProps) {
  const mutateStart = api.v1.postProjectsProjectIdStart.useMutation({
    path: { projectId }
  })
  
  const mutateStop = api.v1.postProjectsProjectIdStop.useMutation({
    path: { projectId }
  })
  
  const onStartStopClick = useCallback(async () => {
    if (state === ProjectState.Stopped) {
      await mutateStart.mutateAsync({})
    } else {
      await mutateStop.mutateAsync({})
    }
    
    api.v1.getProjectsProjectId.invalidateQueries({
      parameters: { path: { projectId } }
    })
  }, [state, projectId])
  
  return (
    <Button 
      onClick={onStartStopClick} 
      disabled={state === ProjectState.Starting}
      variant={state === ProjectState.Stopped ? "default" : "destructive"}
    >
      {state === ProjectState.Stopped ? "Start" : "Stop"}
    </Button>
  )
}
```

## Form Handling Pattern

Forms are implemented using a custom hook approach with Zod for validation:

```tsx
// Form handling example
// filepath: /routes/_auth/_dashboard/projects/$projectId/settings/domains.tsx
import { z } from "zod"
import { useAppForm } from '@/components/form/use-app-form'

// Schema definition with validation
const addDomainSchema = z.object({
  domain: z.string().min(4).max(255)
    .regex(/^[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,}$/, 'Invalid domain name')
    .transform((val) => val.trim()),
  serviceName: z.enum(SystemTypes)
});

// Form component
function DomainForm() {
  // Using the form hook with schema
  const form = useAppForm({
    schema: addDomainSchema,
    defaultValues: {
      domain: '',
      serviceName: SystemType.Web
    },
    onSubmit: async (values) => {
      await mutateAsync({
        body: values
      });
      
      // Reset form and refetch data
      form.reset();
      invalidate();
    }
  });
  
  return (
    <FormProvider {...form}>
      <form onSubmit={form.handleSubmit}>
        {/* Form fields */}
        <FormField
          control={form.control}
          name="domain"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Domain</FormLabel>
              <FormControl>
                <Input placeholder="example.com" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        
        {/* Submit button */}
        <LoadingButton loading={form.formState.isSubmitting} type="submit">
          Add Domain
        </LoadingButton>
      </form>
    </FormProvider>
  );
}
```

## Data Integration Pattern

Components that require data use a consistent pattern for data fetching:

```tsx
// Data integration example
function ProjectsList() {
  // Fetch data with TanStack Query via the API client
  const { data, isLoading, error } = api.v1.getProjects.useQuery();
  
  // Loading state
  if (isLoading) {
    return <LoadingPlaceholder />;
  }
  
  // Error state
  if (error) {
    return <ErrorDisplay message={error.message} />;
  }
  
  // Empty state
  if (!data || data.projects.length === 0) {
    return <EmptyState message="No projects found" />;
  }
  
  // Data display
  return (
    <div>
      {data.projects.map(project => (
        <ProjectCard key={project.id} project={project} />
      ))}
    </div>
  );
}
```

## Component Composition Pattern

The application uses component composition to build complex UIs:

```tsx
// Component composition example
function ProjectDetailsPage() {
  const { projectId } = Route.useParams();
  const { data: project } = api.v1.getProjectsProjectId.useQuery({
    path: { projectId }
  });
  
  return (
    <div className="space-y-6">
      {/* Header component */}
      <PageHeader
        title={project?.name || "Project"}
        description="Project overview and status"
      />
      
      {/* Status card */}
      <ProjectStatusCard project={project} />
      
      {/* Control panel */}
      <Card>
        <CardHeader>
          <CardTitle>Controls</CardTitle>
        </CardHeader>
        <CardContent>
          <StartStopProjectButton 
            projectId={projectId}
            state={project?.state?.data?.state}
          />
        </CardContent>
      </Card>
      
      {/* Usage stats */}
      <UsageStatsPanel projectId={projectId} />
    </div>
  );
}
```

## Responsive Design Pattern

Components use Tailwind CSS for responsive design:

```tsx
// Responsive design example
<div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
  {items.map(item => (
    <Card key={item.id} className="flex flex-col">
      <CardHeader>
        <CardTitle className="text-lg md:text-xl">{item.title}</CardTitle>
      </CardHeader>
      <CardContent className="flex-grow">
        <p className="text-sm md:text-base">{item.description}</p>
      </CardContent>
      <CardFooter className="pt-2 flex justify-end">
        <Button size="sm" variant="outline">View Details</Button>
      </CardFooter>
    </Card>
  ))}
</div>
```

## Error Handling Pattern

Components handle errors at the appropriate level:

```tsx
// Error boundary component
function ErrorBoundary({ children }: { children: React.ReactNode }) {
  return (
    <ErrorBoundaryComponent 
      fallback={({ error }) => (
        <div className="p-4 border border-red-300 bg-red-50 rounded-md">
          <h3 className="text-lg font-medium text-red-800">Something went wrong</h3>
          <p className="mt-2 text-sm text-red-700">{error.message}</p>
        </div>
      )}
    >
      {children}
    </ErrorBoundaryComponent>
  );
}

// Usage
<ErrorBoundary>
  <ComplexComponent />
</ErrorBoundary>
```

## Reusable Patterns

The codebase emphasizes pattern reuse in several areas:

1. **Lists and Tables**: Consistent patterns for displaying collections of items
2. **Forms**: Consistent structure using `useAppForm` hook and schema validation
3. **Cards**: Standard card layouts for displaying information
4. **Empty/Loading States**: Consistent placeholders and empty state messaging
5. **Action Patterns**: Standard patterns for confirmations and actions (e.g., "Are You Sure?" dialogs)

These patterns should be followed when creating new components to maintain consistency.
