import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { Building2, Users, ChevronLeft, User } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { motion } from 'framer-motion'
import { apiBaseUrl } from '@/api-client'
import { useQuery } from '@tanstack/react-query'

interface OrganizationDetail {
  id: string
  name: string
  slug: string
  isPersonal: boolean
  avatarUrl?: string | null
  memberCount: number
}

export const Route = createFileRoute('/_auth/_dashboard/organizations/$orgId/')({
  component: OrganizationDetailPage,
})

function OrganizationDetailPage() {
  const { orgId } = Route.useParams()
  const navigate = useNavigate()

  const { data: org, isLoading } = useQuery<OrganizationDetail>({
    queryKey: ['organization', orgId],
    queryFn: async () => {
      const res = await fetch(`${apiBaseUrl}/v1/organizations/${orgId}`, { credentials: 'include' })
      if (!res.ok) throw new Error('Failed to fetch organization')
      return res.json()
    },
  })

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      transition={{ duration: 0.3 }}
    >
      <header className="mb-6">
        <button
          onClick={() => navigate({ to: '/organizations' })}
          className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors mb-4"
        >
          <ChevronLeft className="h-4 w-4" />
          Back to organizations
        </button>
        <div className="flex items-center gap-3">
          {isLoading ? (
            <Skeleton className="h-10 w-10 rounded-lg" />
          ) : (
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-muted">
              {org?.isPersonal ? (
                <User className="h-5 w-5 text-muted-foreground" />
              ) : (
                <Building2 className="h-5 w-5 text-muted-foreground" />
              )}
            </div>
          )}
          <div>
            {isLoading ? (
              <Skeleton className="h-8 w-48" />
            ) : (
              <h1 className="text-3xl font-bold">
                {org?.isPersonal ? 'Personal Account' : org?.name}
              </h1>
            )}
            {!isLoading && (
              <p className="text-muted-foreground text-sm font-mono">{org?.slug}</p>
            )}
          </div>
          {!isLoading && org?.isPersonal && (
            <Badge variant="secondary">Personal</Badge>
          )}
        </div>
      </header>

      <Card
        className="cursor-pointer transition-all duration-200 hover:-translate-y-0.5 hover:border-primary/30 hover:shadow-md hover:shadow-primary/5"
        onClick={() => navigate({ to: '/organizations/$orgId/members', params: { orgId } })}
      >
        <CardContent className="p-6">
          <Users className="h-8 w-8 text-muted-foreground mb-3" />
          <h3 className="text-sm font-semibold">Members</h3>
          <p className="mt-1 text-xs text-muted-foreground">
            {isLoading ? (
              <Skeleton className="h-3 w-24" />
            ) : (
              `${org?.memberCount ?? 0} member${(org?.memberCount ?? 0) !== 1 ? 's' : ''} — invite and manage access`
            )}
          </p>
        </CardContent>
      </Card>
    </motion.div>
  )
}
