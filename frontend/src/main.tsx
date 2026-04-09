import React from 'react'
import ReactDOM from 'react-dom/client'
import { RouterProvider, createRouter } from '@tanstack/react-router'
import { routeTree } from './routeTree.gen'
import { useDynamicAuth } from './hooks/use-dynamic-auth'
import "./index.css";
import { QueryClientProvider } from '@tanstack/react-query';
import { queryClient } from './api-client';
import { Toaster } from './components/ui/toaster';
import { ModalProvider } from './hooks/use-modal';
import { LoadingAnimation } from './components/loading-animation';
import { HelmetProvider } from 'react-helmet-async'
import { ActionRegistryProvider } from './lib/command-palette/action-registry'
import { CommandPalette } from './components/command-palette'
import { useGlobalActions } from './lib/command-palette/global-actions'

const router = createRouter({
  routeTree,
  defaultPreload: 'intent',
  scrollRestoration: false,
  defaultPendingComponent: LoadingAnimation,
  context: {
    auth: undefined!
  }
})

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}

function GlobalActionsRegistrar({ children }: { children: React.ReactNode }) {
  useGlobalActions(router.navigate)
  return <>{children}</>
}

const App = () => {
  const { authComponents, isLoading, error } = useDynamicAuth();

  if (isLoading) {
    return null;
  }

  if (error) {
    return <div>Error loading auth: {error.message}</div>;
  }

  if (!authComponents) {
    return <div>Failed to load auth components</div>;
  }

  const { AuthProvider } = authComponents;

  const InnerApp = () => {
    const auth = authComponents.useDynamicAuth();
    return <RouterProvider router={router} context={{ auth }} />;
  };

  return (
    <AuthProvider>
      <HelmetProvider>
        <QueryClientProvider client={queryClient}>
          <ModalProvider>
            <ActionRegistryProvider>
              <GlobalActionsRegistrar>
                <InnerApp />
                <CommandPalette navigate={router.navigate} />
              </GlobalActionsRegistrar>
            </ActionRegistryProvider>
            <Toaster />
          </ModalProvider>
        </QueryClientProvider>
      </HelmetProvider>
    </AuthProvider>
  );
};

const rootElement = document.getElementById('app')!;

if (!rootElement?.innerHTML) {
  const root = ReactDOM.createRoot(rootElement)
  root.render(<App />)
}