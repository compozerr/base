# Advanced Frontend Patterns

This document outlines advanced frontend patterns used in the application, focusing on pagination, data management, and other common frontend patterns.

## Pagination Pattern

The application implements pagination for large data sets using the following approach:

```tsx
// Pagination example
function PaginatedList() {
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  
  // Fetch paginated data
  const { data, isLoading } = api.v1.getItems.useQuery({
    query: {
      page,
      pageSize,
    }
  });
  
  // Calculate total pages
  const totalPages = data?.totalCount 
    ? Math.ceil(data.totalCount / pageSize) 
    : 1;
  
  return (
    <div className="space-y-4">
      {/* Items list */}
      <div className="space-y-2">
        {data?.items.map(item => (
          <ItemCard key={item.id} item={item} />
        ))}
      </div>
      
      {/* Pagination controls */}
      <div className="flex items-center justify-between">
        <Button
          onClick={() => setPage(p => Math.max(1, p - 1))}
          disabled={page === 1 || isLoading}
          variant="outline"
        >
          Previous
        </Button>
        
        <span className="text-sm text-gray-500">
          Page {page} of {totalPages}
        </span>
        
        <Button
          onClick={() => setPage(p => p + 1)}
          disabled={page >= totalPages || isLoading}
          variant="outline"
        >
          Next
        </Button>
      </div>
    </div>
  );
}
```

## Infinite Scroll Pattern

For content that benefits from infinite scrolling:

```tsx
// Infinite scroll example
function InfiniteList() {
  // Use TanStack Query's useInfiniteQuery
  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = api.v1.getItems.useInfiniteQuery(
    {
      query: {
        pageSize: 10,
      }
    },
    {
      getNextPageParam: (lastPage) => {
        // Return undefined when there are no more pages
        return lastPage.hasMore ? lastPage.nextCursor : undefined;
      },
    }
  );
  
  // Detect when to load more
  const observer = useRef<IntersectionObserver | null>(null);
  const lastItemRef = useCallback(
    (node: HTMLDivElement | null) => {
      if (isFetchingNextPage) return;
      
      // Disconnect previous observer
      if (observer.current) observer.current.disconnect();
      
      // Create new observer
      observer.current = new IntersectionObserver(entries => {
        if (entries[0].isIntersecting && hasNextPage) {
          fetchNextPage();
        }
      });
      
      // Observe the last item
      if (node) observer.current.observe(node);
    },
    [isFetchingNextPage, fetchNextPage, hasNextPage]
  );
  
  return (
    <div className="space-y-4">
      {data?.pages.map((page, i) => (
        <React.Fragment key={i}>
          {page.items.map((item, index) => (
            <div 
              key={item.id} 
              ref={
                i === data.pages.length - 1 && 
                index === page.items.length - 1 
                  ? lastItemRef 
                  : null
              }
            >
              <ItemCard item={item} />
            </div>
          ))}
        </React.Fragment>
      ))}
      
      {isFetchingNextPage && <LoadingIndicator />}
    </div>
  );
}
```

## Optimistic Updates Pattern

For responsive UI updates before API calls complete:

```tsx
// Optimistic updates example
function TodoList() {
  const { data: todos } = api.v1.getTodos.useQuery();
  
  const { mutate: toggleTodo } = api.v1.putTodosToggle.useMutation({
    // Optimistically update the UI
    onMutate: async (variables) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries(['todos']);
      
      // Snapshot current state
      const previousTodos = queryClient.getQueryData(['todos']);
      
      // Optimistically update
      queryClient.setQueryData(['todos'], (old) => {
        return old.map(todo => 
          todo.id === variables.path.todoId 
            ? { ...todo, completed: !todo.completed }
            : todo
        );
      });
      
      // Return snapshotted value for rollback
      return { previousTodos };
    },
    
    // On error, roll back
    onError: (err, variables, context) => {
      queryClient.setQueryData(['todos'], context.previousTodos);
    },
    
    // After success or error, refetch
    onSettled: () => {
      queryClient.invalidateQueries(['todos']);
    },
  });
  
  return (
    <ul>
      {todos?.map(todo => (
        <li key={todo.id} onClick={() => toggleTodo({ path: { todoId: todo.id } })}>
          {todo.title} {todo.completed ? '✓' : '○'}
        </li>
      ))}
    </ul>
  );
}
```

## Debounced Input Pattern

For search fields and other inputs that should wait for user input to complete:

```tsx
// Debounced input example
function SearchInput({ onSearch }: { onSearch: (value: string) => void }) {
  const [value, setValue] = useState('');
  const debouncedValue = useDebouncedValue(value, 500);
  
  // Effect runs when debounced value changes
  useEffect(() => {
    onSearch(debouncedValue);
  }, [debouncedValue, onSearch]);
  
  return (
    <div className="relative">
      <Input
        type="text"
        placeholder="Search..."
        value={value}
        onChange={(e) => setValue(e.target.value)}
        className="pr-8"
      />
      {value !== debouncedValue && (
        <span className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400">
          <SpinnerIcon className="w-4 h-4 animate-spin" />
        </span>
      )}
    </div>
  );
}

// Custom hook for debouncing
function useDebouncedValue<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState(value);
  
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);
    
    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);
  
  return debouncedValue;
}
```

## Form Validation Pattern

The application uses Zod for form validation:

```tsx
// Form validation example
const formSchema = z.object({
  name: z.string().min(2, "Name must be at least 2 characters"),
  email: z.string().email("Invalid email address"),
  age: z.number().min(18, "Must be at least 18 years old"),
  preferences: z.object({
    notifications: z.boolean(),
    theme: z.enum(["light", "dark", "system"]),
  }),
});

function ProfileForm() {
  const form = useAppForm({
    schema: formSchema,
    defaultValues: {
      name: "",
      email: "",
      age: undefined,
      preferences: {
        notifications: true,
        theme: "system",
      },
    },
    onSubmit: async (values) => {
      await api.v1.putUserProfile.mutateAsync({
        body: values
      });
    },
  });
  
  return (
    <FormProvider {...form}>
      <form onSubmit={form.handleSubmit}>
        {/* Form fields */}
      </form>
    </FormProvider>
  );
}
```

## Data Refresh Patterns

Strategies for keeping data fresh:

```tsx
// Poll for updates
const { data: project } = api.v1.getProjectsProjectId.useQuery(
  { path: { projectId } },
  {
    // Poll every 2 seconds if project is starting, otherwise don't poll
    refetchInterval: (project) => 
      project?.state.data?.state === ProjectState.Starting ? 2000 : false,
  }
);

// Refetch on window focus
const { data } = api.v1.getNotifications.useQuery(
  {},
  {
    // Refetch when window regains focus
    refetchOnWindowFocus: true,
  }
);

// Refetch on interval
const { data } = api.v1.getSystemStatus.useQuery(
  {},
  {
    // Refetch every minute
    refetchInterval: 60 * 1000,
  }
);
```

## Skeleton Loading Pattern

Visual placeholders during loading:

```tsx
// Skeleton loading example
function ProjectCardSkeleton() {
  return (
    <Card>
      <CardHeader>
        <Skeleton className="h-7 w-3/4" />
      </CardHeader>
      <CardContent>
        <div className="space-y-2">
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-4/5" />
          <Skeleton className="h-4 w-2/3" />
        </div>
      </CardContent>
      <CardFooter>
        <Skeleton className="h-9 w-24" />
      </CardFooter>
    </Card>
  );
}

function ProjectList() {
  const { data, isLoading } = api.v1.getProjects.useQuery();
  
  if (isLoading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {Array(4).fill(0).map((_, i) => (
          <ProjectCardSkeleton key={i} />
        ))}
      </div>
    );
  }
  
  // Render actual content
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
      {data.projects.map(project => (
        <ProjectCard key={project.id} project={project} />
      ))}
    </div>
  );
}
```

## Toast Notification Pattern

Displaying transient feedback to users:

```tsx
// Toast notification example
import { useToast } from '@/components/ui/use-toast';

function ActionButton() {
  const { toast } = useToast();
  const { mutateAsync, isPending } = api.v1.postAction.useMutation();
  
  const handleClick = async () => {
    try {
      await mutateAsync({});
      toast({
        title: "Success",
        description: "Action completed successfully",
        variant: "success",
      });
    } catch (error) {
      toast({
        title: "Error",
        description: error.message || "Something went wrong",
        variant: "destructive",
      });
    }
  };
  
  return (
    <Button onClick={handleClick} disabled={isPending}>
      {isPending ? 'Processing...' : 'Perform Action'}
    </Button>
  );
}
```

These advanced patterns help maintain consistent, responsive, and user-friendly interfaces across the application.
