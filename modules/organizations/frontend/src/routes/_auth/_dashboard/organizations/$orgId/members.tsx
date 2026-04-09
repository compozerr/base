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
import { api } from '@/api-client'
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
  email?: string | null
  githubUsername?: string | null
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

  const { data: searchResults, isFetching: isSearching } = api.v1.getOrganizationsGithubUserSearch.useQuery(
    { query: { q: debouncedQuery } },
    { enabled: debouncedQuery.length >= 2 && !selectedUser },
  ) as { data: { users: GithubUser[] } | undefined, isFetching: boolean }

  const { data: members, isLoading: membersLoading } = api.v1.getOrganizationsOrganizationIdMembers.useQuery(
    { path: { organizationId: orgId } },
  ) as { data: Member[] | undefined, isLoading: boolean }

  const { data: invitations } = api.v1.getOrganizationsOrganizationIdInvitations.useQuery(
    { path: { organizationId: orgId } },
  ) as { data: Invitation[] | undefined }

  const inviteMutation = api.v1.postOrganizationsOrganizationIdInvitations.useMutation(
    { path: { organizationId: orgId } },
    {
      onSuccess: () => {
        const label = selectedUser?.login || selectedUser?.email || inviteEmail
        toast({ title: 'Invitation sent', description: `Invitation sent to ${label}`, variant: 'success' })
        api.v1.getOrganizationsOrganizationIdInvitations.invalidateQueries({ parameters: { path: { organizationId: orgId } } })
        handleCloseInviteDialog()
      },
      onError: (error) => {
        toast({ title: 'Error', description: String(error), variant: 'destructive' })
      },
    },
  )

  const removeMemberMutation = api.v1.deleteOrganizationsOrganizationIdMembersMemberId.useMutation(
    undefined as void,
    {
      onSuccess: () => {
        toast({ title: 'Member removed', variant: 'success' })
        api.v1.getOrganizationsOrganizationIdMembers.invalidateQueries({ parameters: { path: { organizationId: orgId } } })
        setRemoveMemberId(null)
      },
      onError: (error) => {
        toast({ title: 'Error', description: String(error), variant: 'destructive' })
      },
    },
  )

  const handleCloseInviteDialog = () => {
    setInviteDialogOpen(false)
    setSearchQuery('')
    setDebouncedQuery('')
    setSelectedUser(null)
    setInviteEmail('')
    setInviteRole('Member')
  }

  const handleInvite = () => {
    const email = selectedUser?.email || inviteEmail || undefined
    const githubUsername = selectedUser?.login || undefined

    if (!email && !githubUsername) {
      toast({ title: 'Recipient required', description: 'Select a GitHub user or enter an email address.', variant: 'destructive' })
      return
    }

    inviteMutation.mutate({
      email: email?.trim() ?? null,
      githubUsername: githubUsername ?? null,
      role: inviteRole === 'Admin' ? 0 : 1,
    })
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
                  <TableHead>Invited</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Expires</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {invitations.map((inv) => (
                  <TableRow key={inv.id}>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        {inv.githubUsername && (
                          <div className="flex items-center gap-1.5">
                            <Github className="h-3.5 w-3.5 text-muted-foreground" />
                            <span className="text-sm font-medium">{inv.githubUsername}</span>
                          </div>
                        )}
                        {inv.email && (
                          <span className="font-mono text-sm text-muted-foreground">{inv.email}</span>
                        )}
                      </div>
                    </TableCell>
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
                    {!selectedUser.email && (
                      <p className="text-xs text-muted-foreground">Will be auto-accepted when they log in via GitHub</p>
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

            {/* Email - shown if user selected but has no public email, allow optional email entry */}
            {(selectedUser && !selectedUser.email) && (
              <div className="space-y-2">
                <label className="text-sm font-medium">
                  Email address (optional)
                </label>
                <Input
                  type="email"
                  value={inviteEmail}
                  onChange={(e) => setInviteEmail(e.target.value)}
                  placeholder={`Enter ${selectedUser.login}'s email to also send a notification`}
                />
                <p className="text-xs text-muted-foreground">
                  If provided, they'll receive an invitation email. Otherwise, the invitation is accepted automatically when they log in.
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
                  inviteMutation.mutate({
                    email: searchQuery.trim(),
                    githubUsername: null,
                    role: inviteRole === 'Admin' ? 0 : 1,
                  })
                } else {
                  handleInvite()
                }
              }}
              disabled={inviteMutation.isPending || (!selectedUser && !inviteEmail.trim() && !searchQuery.includes('@'))}
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
              onClick={() => removeMemberId && removeMemberMutation.mutate({ path: { organizationId: orgId, memberId: removeMemberId } })}
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
