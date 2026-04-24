import { createRouter } from '@tanstack/react-router'
import { routeTree } from './routeTree.gen'
import { LoadingAnimation } from './components/loading-animation'

export const router = createRouter({
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
