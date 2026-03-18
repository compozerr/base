import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { Building2, Plus, User, ChevronRight, Users } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { motion } from 'framer-motion'
import { apiBaseUrl } from '@/api-client'
import { useAuth } from '@/hooks/use-dynamic-auth'
import { useQuery } from '@tanstack/react-query'

interface Organization {
  id: string
  name: string
  slug: string
  isPersonal: boolean
  avatarUrl?: string | null
  memberCount?: number
}

export const Route = createFileRoute('/_auth/_dashboard/organizations/')({
  component: OrganizationsPage,
})

function OrganizationsPage() {
  const navigate = useNavigate()
  const { user } = useAuth()

  const { data: organizations, isLoading } = useQuery<Organization[]>({
    queryKey: ['organizations'],
    queryFn: async () => {
      const res = await fetch(`${apiBaseUrl}/organizations`, { credentials: 'include' })
      if (!res.ok) throw new Error('Failed to fetch organizations')
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
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold mb-2">Organizations</h1>
            <p className="text-muted-foreground">
              Manage your organizations and team access
            </p>
          </div>
          <Button onClick={() => {/* TODO: open create org dialog */}}>
            <Plus className="h-4 w-4 mr-2" />
            New Organization
          </Button>
        </div>
      </header>

      <div className="space-y-3">
        {isLoading ? (
          <>
            <Skeleton className="h-[72px] w-full rounded-lg" />
            <Skeleton className="h-[72px] w-full rounded-lg" />
          </>
        ) : organizations && organizations.length > 0 ? (
          organizations.map((org, index) => (
            <motion.div
              key={org.id}
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ duration: 0.25, delay: index * 0.04 }}
            >
              <Card
                className="cursor-pointer transition-all duration-200 hover:-translate-y-0.5 hover:border-primary/30 hover:shadow-md hover:shadow-primary/5"
                onClick={() => navigate({ to: '/organizations/$orgId', params: { orgId: org.id } })}
              >
                <CardContent className="flex items-center gap-4 p-4">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-muted">
                    {org.isPersonal ? (
                      user?.avatarUrl ? (
                        <img src={user.avatarUrl} alt="" className="h-10 w-10 rounded-lg" />
                      ) : (
                        <User className="h-5 w-5 text-muted-foreground" />
                      )
                    ) : (
                      <Building2 className="h-5 w-5 text-muted-foreground" />
                    )}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <p className="text-sm font-semibold truncate">
                        {org.isPersonal ? 'Personal Account' : org.name}
                      </p>
                      {org.isPersonal && (
                        <Badge variant="secondary" className="text-xs">Personal</Badge>
                      )}
                    </div>
                    <p className="text-xs text-muted-foreground font-mono">{org.slug}</p>
                  </div>
                  {org.memberCount !== undefined && org.memberCount > 1 && (
                    <div className="flex items-center gap-1.5 text-muted-foreground">
                      <Users className="h-3.5 w-3.5" />
                      <span className="text-xs">{org.memberCount}</span>
                    </div>
                  )}
                  <ChevronRight className="h-4 w-4 text-muted-foreground" />
                </CardContent>
              </Card>
            </motion.div>
          ))
        ) : (
          <Card>
            <CardContent className="flex items-center gap-4 p-4">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-muted">
                <User className="h-5 w-5 text-muted-foreground" />
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold">Personal Account</p>
                <p className="text-xs text-muted-foreground">Your personal projects</p>
              </div>
              <ChevronRight className="h-4 w-4 text-muted-foreground" />
            </CardContent>
          </Card>
        )}
      </div>
    </motion.div>
  )
}
