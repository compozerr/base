import * as React from 'react'
import { createFileRoute, redirect } from '@tanstack/react-router'
import { z } from 'zod'
import { useAuth } from '../auth'

const fallback = '/dashboard' as const

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
  const [loading, setLoading] = React.useState(true)
  const [error, setError] = React.useState<Error | null>(null)
  const auth = useAuth()
  React.useEffect(() => {
    auth.login();
  }, [auth])
  
  return (
    <div className="p-2 h-full">
      {loading && <p>Loading...</p>}
      {error && <p>Error: {error.message}</p>}
    </div>
  )
}
