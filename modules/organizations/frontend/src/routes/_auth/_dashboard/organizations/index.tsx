import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { Building2, Plus, User, ChevronRight, Users } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { useToast } from '@/hooks/use-toast'
import { motion } from 'framer-motion'
import { apiBaseUrl } from '@/api-client'
import { useAuth } from '@/hooks/use-dynamic-auth'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import * as React from 'react'

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
  const { toast } = useToast()
  const queryClient = useQueryClient()
  const [createDialogOpen, setCreateDialogOpen] = React.useState(false)
  const [orgName, setOrgName] = React.useState('')
  const [orgSlug, setOrgSlug] = React.useState('')
  const [slugManuallyEdited, setSlugManuallyEdited] = React.useState(false)

  const { data: organizations, isLoading } = useQuery<Organization[]>({
    queryKey: ['organizations'],
    queryFn: async () => {
      const res = await fetch(`${apiBaseUrl}/organizations`, { credentials: 'include' })
      if (!res.ok) throw new Error('Failed to fetch organizations')
      return res.json()
    },
  })

  const createOrgMutation = useMutation({
    mutationFn: async ({ name, slug }: { name: string; slug: string }) => {
      const res = await fetch(`${apiBaseUrl}/organizations`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, slug }),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => null)
        throw new Error(err?.detail || err?.errors?.Slug?.[0] || 'Failed to create organization')
      }
      return res.json()
    },
    onSuccess: (data) => {
      toast({ title: 'Organization created', description: `${orgName} is ready to go.`, variant: 'success' })
      queryClient.invalidateQueries({ queryKey: ['organizations'] })
      setCreateDialogOpen(false)
      setOrgName('')
      setOrgSlug('')
      setSlugManuallyEdited(false)
      navigate({ to: '/organizations/$orgId', params: { orgId: data.id } })
    },
    onError: (error: Error) => {
      toast({ title: 'Error', description: error.message, variant: 'destructive' })
    },
  })

  const handleNameChange = (name: string) => {
    setOrgName(name)
    if (!slugManuallyEdited) {
      setOrgSlug(name.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, ''))
    }
  }

  const handleCreate = () => {
    if (!orgName.trim() || !orgSlug.trim()) return
    createOrgMutation.mutate({ name: orgName.trim(), slug: orgSlug.trim() })
  }

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
          <Button onClick={() => setCreateDialogOpen(true)}>
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
          <p className="text-sm text-muted-foreground text-center py-8">
            No organizations found. Create one to get started.
          </p>
        )}
      </div>

      {/* Create Organization Dialog */}
      <Dialog open={createDialogOpen} onOpenChange={setCreateDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create Organization</DialogTitle>
            <DialogDescription>
              Create a new organization to collaborate on projects with your team.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <label htmlFor="org-name" className="text-sm font-medium">
                Organization name
              </label>
              <Input
                id="org-name"
                value={orgName}
                onChange={(e) => handleNameChange(e.target.value)}
                placeholder="My Team"
                onKeyDown={(e) => e.key === 'Enter' && handleCreate()}
              />
            </div>
            <div className="space-y-2">
              <label htmlFor="org-slug" className="text-sm font-medium">
                URL slug
              </label>
              <Input
                id="org-slug"
                value={orgSlug}
                onChange={(e) => {
                  setSlugManuallyEdited(true)
                  setOrgSlug(e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, ''))
                }}
                placeholder="my-team"
                className="font-mono"
              />
              <p className="text-xs text-muted-foreground">
                Only lowercase letters, numbers, and hyphens. Must be unique.
              </p>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCreateDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleCreate}
              disabled={!orgName.trim() || !orgSlug.trim() || createOrgMutation.isPending}
            >
              {createOrgMutation.isPending ? 'Creating...' : 'Create Organization'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </motion.div>
  )
}
