import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { Plus, Shield, User, ChevronLeft, UserMinus, Search, Github } from 'lucide-react'
import { useAuth } from '@/hooks/use-dynamic-auth'
import { Button } from '@/components/ui/button'
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/ui/card'
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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { useToast } from '@/hooks/use-toast'
import { motion } from 'framer-motion'
import { apiBaseUrl } from '@/api-client'
import { orgFetch } from '@/lib/org-storage'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useRegisterActions } from '@/lib/command-palette/action-registry'
import * as React from 'react'

interface Member {
  id: string
  userId: string
  name: string
  email: string
  avatarUrl?: string | null
  role: 'Admin' | 'Member'
  joinedAtUtc: string
}

interface Invitation {
  id: string
  email: string
  role: 'Admin' | 'Member'
  status: string
  expiresAtUtc: string
  createdAtUtc: string
}

interface GithubUser {
  login: string
  avatarUrl: string
  name?: string
  email?: string
}

export const Route = createFileRoute('/_auth/_dashboard/organizations/$orgId/members')({
  component: MembersPage,
})

function MembersPage() {
  const { orgId } = Route.useParams()
  const navigate = useNavigate()
  const { toast } = useToast()
  const { user: currentUser } = useAuth()
  const queryClient = useQueryClient()
  const [inviteDialogOpen, setInviteDialogOpen] = React.useState(false)
  const [searchQuery, setSearchQuery] = React.useState('')
  const [debouncedQuery, setDebouncedQuery] = React.useState('')
  const [selectedUser, setSelectedUser] = React.useState<GithubUser | null>(null)
  const [inviteEmail, setInviteEmail] = React.useState('')
  const [inviteRole, setInviteRole] = React.useState('Member')
  const [removeMemberId, setRemoveMemberId] = React.useState<string | null>(null)

  useRegisterActions([
    {
      id: "invite-member",
      name: "Invite member",
      icon: Plus,
      group: "action" as const,
      keywords: ["add", "team", "user", "invitation"],
      perform: () => setInviteDialogOpen(true),
    },
  ], []);

  // Debounce search
  React.useEffect(() => {
    const timer = setTimeout(() => setDebouncedQuery(searchQuery), 300)
    return () => clearTimeout(timer)
  }, [searchQuery])

  const { data: searchResults, isFetching: isSearching } = useQuery<{ users: GithubUser[] }>({
    queryKey: ['github-user-search', debouncedQuery],
    queryFn: async () => {
      if (!res.ok) return { users: [] }
      return res.json()
    },
    enabled: debouncedQuery.length >= 2 && !selectedUser,
  })

  const { data: members, isLoading: membersLoading } = useQuery<Member[]>({
    queryKey: ['organization-members', orgId],
    queryFn: async () => {
      if (!res.ok) throw new Error('Failed to fetch members')
      return res.json()
    },
  })

  const { data: invitations } = useQuery<Invitation[]>({
    queryKey: ['organization-invitations', orgId],
    queryFn: async () => {
      if (!res.ok) return []
      return res.json()
    },
  })

  const inviteMutation = useMutation({
    mutationFn: async ({ email, role }: { email: string; role: string }) => {
      const res = await orgFetch(`${apiBaseUrl}/v1/organizations/${orgId}/invitations`, {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, role: role === 'Admin' ? 0 : 1 }),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => null)
        throw new Error(err?.detail || 'Failed to send invitation')
      }
      return res.json()
    },
    onSuccess: () => {
      const email = selectedUser?.email || inviteEmail
      toast({ title: 'Invitation sent', description: `Invitation sent to ${email}`, variant: 'success' })
      queryClient.invalidateQueries({ queryKey: ['organization-invitations', orgId] })
      handleCloseInviteDialog()
    },
    onError: (error: Error) => {
      toast({ title: 'Error', description: error.message, variant: 'destructive' })
    },
  })

  const removeMemberMutation = useMutation({
    mutationFn: async (memberId: string) => {
      const res = await orgFetch(`${apiBaseUrl}/v1/organizations/${orgId}/members/${memberId}`, {
        method: 'DELETE',
        credentials: 'include',
      })
      if (!res.ok) {
        const err = await res.json().catch(() => null)
        throw new Error(err?.detail || 'Failed to remove member')
      }
    },
    onSuccess: () => {
      toast({ title: 'Member removed', variant: 'success' })
      queryClient.invalidateQueries({ queryKey: ['organization-members', orgId] })
      setRemoveMemberId(null)
    },
    onError: (error: Error) => {
      toast({ title: 'Error', description: error.message, variant: 'destructive' })
    },
  })

  const handleCloseInviteDialog = () => {
    setInviteDialogOpen(false)
    setSearchQuery('')
    setDebouncedQuery('')
    setSelectedUser(null)
    setInviteEmail('')
    setInviteRole('Member')
  }

  const handleInvite = () => {
    const email = selectedUser?.email || inviteEmail
    if (!email?.trim()) {
      toast({ title: 'Email required', description: 'Please enter an email address or select a GitHub user with a public email.', variant: 'destructive' })
      return
    }
    inviteMutation.mutate({ email: email.trim(), role: inviteRole })
  }

  const handleSelectUser = (user: GithubUser) => {
    setSelectedUser(user)
    setSearchQuery(user.login)
    if (user.email) {
      setInviteEmail(user.email)
    }
  }

  const formatDate = (dateStr: string) => new Date(dateStr).toLocaleDateString()

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      transition={{ duration: 0.3 }}
    >
      <header className="mb-6">
        <button
          onClick={() => navigate({ to: '/organizations/$orgId', params: { orgId } })}
          className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors mb-4"
        >
          <ChevronLeft className="h-4 w-4" />
          Back to organization
        </button>
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold mb-2">Members</h1>
            <p className="text-muted-foreground">
              Manage who has access to this organization
            </p>
          </div>
          <Button onClick={() => setInviteDialogOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            Invite Member
          </Button>
        </div>
      </header>

      <Card>
        <CardHeader>
          <CardTitle>Team Members</CardTitle>
          <CardDescription>
            {membersLoading ? (
              <Skeleton className="h-4 w-32" />
            ) : (
              `${members?.length ?? 0} member${(members?.length ?? 0) !== 1 ? 's' : ''}`
            )}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {membersLoading ? (
            <div className="space-y-3">
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
            </div>
          ) : members && members.length > 0 ? (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Member</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Joined</TableHead>
                  <TableHead className="w-[50px]"></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {members.map((member) => (
                  <TableRow key={member.id}>
                    <TableCell>
                      <div className="flex items-center gap-3">
                        {member.avatarUrl ? (
                          <img src={member.avatarUrl} alt="" className="h-8 w-8 rounded-full ring-1 ring-white/10" />
                        ) : (
                          <div className="flex h-8 w-8 items-center justify-center rounded-full bg-muted">
                            <User className="h-4 w-4 text-muted-foreground" />
                          </div>
                        )}
                        <div>
                          <p className="text-sm font-medium">{member.name}</p>
                          <p className="text-xs text-muted-foreground">{member.email}</p>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant={member.role === 'Admin' ? 'default' : 'secondary'}>
                        {member.role === 'Admin' && <Shield className="h-3 w-3 mr-1" />}
                        {member.role}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-muted-foreground text-sm">
                      {formatDate(member.joinedAtUtc)}
                    </TableCell>
                    <TableCell>
                      {member.userId !== currentUser?.id && (
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-8 w-8 p-0"
                          onClick={() => setRemoveMemberId(member.id)}
                        >
                          <UserMinus className="h-4 w-4 text-destructive" />
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          ) : (
            <p className="text-muted-foreground text-sm">No members found.</p>
          )}
        </CardContent>
      </Card>

      {invitations && invitations.length > 0 && (
        <Card className="mt-4">
          <CardHeader>
            <CardTitle className="text-base">Pending Invitations</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Email</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Expires</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {invitations.map((inv) => (
                  <TableRow key={inv.id}>
                    <TableCell className="font-mono text-sm">{inv.email}</TableCell>
                    <TableCell>
                      <Badge variant="outline">{inv.role}</Badge>
                    </TableCell>
                    <TableCell className="text-muted-foreground text-sm">
                      {formatDate(inv.expiresAtUtc)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}

      {/* Invite Dialog with GitHub User Search */}
      <Dialog open={inviteDialogOpen} onOpenChange={(open) => { if (!open) handleCloseInviteDialog() }}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Invite a Member</DialogTitle>
            <DialogDescription>
              Search by GitHub username or enter an email address directly.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            {/* GitHub User Search */}
            <div className="space-y-2">
              <label className="text-sm font-medium">
                GitHub username or email
              </label>
              <div className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  value={searchQuery}
                  onChange={(e) => {
                    setSearchQuery(e.target.value)
                    setSelectedUser(null)
                  }}
                  placeholder="Search GitHub username or enter email..."
                  className="pl-9"
                  onKeyDown={(e) => e.key === 'Enter' && handleInvite()}
                />
              </div>

              {/* Search Results Dropdown */}
              {!selectedUser && debouncedQuery.length >= 2 && (
                <div className="rounded-md border bg-card overflow-hidden">
                  {isSearching ? (
                    <div className="p-3 space-y-2">
                      <Skeleton className="h-8 w-full" />
                      <Skeleton className="h-8 w-full" />
                    </div>
                  ) : searchResults?.users && searchResults.users.length > 0 ? (
                    <div className="max-h-48 overflow-y-auto">
                      {searchResults.users.map((user) => (
                        <button
                          key={user.login}
                          onClick={() => handleSelectUser(user)}
                          className="flex w-full items-center gap-3 px-3 py-2 text-left transition-colors hover:bg-muted/50"
                        >
                          <img
                            src={user.avatarUrl}
                            alt=""
                            className="h-7 w-7 rounded-full ring-1 ring-white/10"
                          />
                          <div className="flex-1 min-w-0">
                            <p className="text-sm font-medium truncate">{user.login}</p>
                            {user.name && (
                              <p className="text-xs text-muted-foreground truncate">{user.name}</p>
                            )}
                          </div>
                          <Github className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
                        </button>
                      ))}
                    </div>
                  ) : (
                    <p className="p-3 text-sm text-muted-foreground">
                      No GitHub users found. You can enter an email address directly.
                    </p>
                  )}
                </div>
              )}

              {/* Selected User */}
              {selectedUser && (
                <div className="flex items-center gap-3 rounded-md border bg-muted/30 px-3 py-2">
                  <img
                    src={selectedUser.avatarUrl}
                    alt=""
                    className="h-8 w-8 rounded-full ring-1 ring-white/10"
                  />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium">{selectedUser.login}</p>
                    {selectedUser.name && (
                      <p className="text-xs text-muted-foreground">{selectedUser.name}</p>
                    )}
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-7 text-xs"
                    onClick={() => {
                      setSelectedUser(null)
                      setSearchQuery('')
                      setInviteEmail('')
                    }}
                  >
                    Change
                  </Button>
                </div>
              )}
            </div>

            {/* Email - shown if user selected but has no public email, or if typing raw email */}
            {(selectedUser && !selectedUser.email) && (
              <div className="space-y-2">
                <label className="text-sm font-medium">
                  Email address
                </label>
                <Input
                  type="email"
                  value={inviteEmail}
                  onChange={(e) => setInviteEmail(e.target.value)}
                  placeholder={`Enter ${selectedUser.login}'s email address`}
                />
                <p className="text-xs text-muted-foreground">
                  This user doesn't have a public email on GitHub. Enter their email to send the invitation.
                </p>
              </div>
            )}

            {/* If no GitHub user selected and query looks like email, show email field */}
            {!selectedUser && searchQuery.includes('@') && (
              <div className="flex items-center gap-2 rounded-md border bg-muted/30 px-3 py-2">
                <p className="text-sm text-muted-foreground">
                  Inviting by email: <strong className="text-foreground">{searchQuery}</strong>
                </p>
              </div>
            )}

            {/* Role Selection */}
            <div className="space-y-2">
              <label className="text-sm font-medium">Role</label>
              <Select value={inviteRole} onValueChange={setInviteRole}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Member">Member — can view and deploy projects</SelectItem>
                  <SelectItem value="Admin">Admin — full access including billing and members</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={handleCloseInviteDialog}>
              Cancel
            </Button>
            <Button
              onClick={() => {
                // If user typed an email directly (contains @), use that
                if (!selectedUser && searchQuery.includes('@')) {
                  setInviteEmail(searchQuery)
                  inviteMutation.mutate({ email: searchQuery.trim(), role: inviteRole })
                } else {
                  handleInvite()
                }
              }}
              disabled={inviteMutation.isPending || (!selectedUser?.email && !inviteEmail.trim() && !searchQuery.includes('@'))}
            >
              {inviteMutation.isPending ? 'Sending...' : 'Send Invitation'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Remove Member Confirmation */}
      <AlertDialog open={!!removeMemberId} onOpenChange={(open) => { if (!open) setRemoveMemberId(null) }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove Member</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to remove this member? They will lose access to all projects in this organization immediately.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => removeMemberId && removeMemberMutation.mutate(removeMemberId)}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Remove Member
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </motion.div>
  )
}
