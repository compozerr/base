import * as React from 'react'
import { createFileRoute, redirect } from '@tanstack/react-router'
import { z } from 'zod'
import { useAuth } from '@/hooks/use-dynamic-auth'
import { LoadingAnimation } from '@/components/loading-animation'

const fallback = '/projects' as const

export const Route = createFileRoute('/login')({
  validateSearch: z.object({
    redirect: z.string().optional().catch(''),
  }),
  beforeLoad: ({ context, search }) => {
    if (context.auth.isAuthenticated) {
      throw redirect({ to: search.redirect || fallback })
    }
  },
  component: RouteComponent,
})

function RouteComponent() {
  const auth = useAuth()

  React.useEffect(() => {
    auth.login();
  }, [auth])

  return <LoadingAnimation />
}
