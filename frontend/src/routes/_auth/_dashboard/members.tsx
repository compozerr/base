import { createFileRoute } from '@tanstack/react-router'
import { Plus, Shield, User, UserMinus } from 'lucide-react'
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
import { api } from '@/api-client'
import { getCurrentOrgId } from '@/lib/org-storage'
import * as React from 'react'

export const Route = createFileRoute('/_auth/_dashboard/members')({
  component: MembersPage,
})

function MembersPage() {
  const orgId = getCurrentOrgId()
  const { toast } = useToast()
  const { user: currentUser } = useAuth()
  const [inviteDialogOpen, setInviteDialogOpen] = React.useState(false)
  const [inviteEmail, setInviteEmail] = React.useState('')
  const [inviteRole, setInviteRole] = React.useState('Member')
  const [removeMemberId, setRemoveMemberId] = React.useState<string | null>(null)

  const { data: membersData, isLoading: membersLoading } = api.v1.getOrganizationsOrganizationIdMembers.useQuery(
    { path: { organizationId: orgId! } },
    { enabled: !!orgId },
  )

  const { data: invitationsData } = api.v1.getOrganizationsOrganizationIdInvitations.useQuery(
    { path: { organizationId: orgId! } },
    { enabled: !!orgId },
  )

  const inviteMutation = api.v1.postOrganizationsOrganizationIdInvitations.useMutation(
    { path: { organizationId: orgId! } },
    {
      onSuccess: () => {
        toast({ title: 'Invitation sent', description: `Invitation sent to ${inviteEmail}`, variant: 'success' })
        void api.v1.getOrganizationsOrganizationIdInvitations.invalidateQueries(
          { parameters: { path: { organizationId: orgId! } } },
        )
        handleCloseInviteDialog()
      },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } as any,
  )

  const removeMemberMutation = api.v1.deleteOrganizationsOrganizationIdMembersMemberId.useMutation(
    undefined,
    {
      onSuccess: () => {
        toast({ title: 'Member removed', variant: 'success' })
        void api.v1.getOrganizationsOrganizationIdMembers.invalidateQueries(
          { parameters: { path: { organizationId: orgId! } } },
        )
        setRemoveMemberId(null)
      },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } as any,
  )

  const handleCloseInviteDialog = () => {
    setInviteDialogOpen(false)
    setInviteEmail('')
    setInviteRole('Member')
  }

  const handleInvite = () => {
    if (!inviteEmail?.trim()) {
      toast({ title: 'Email required', description: 'Please enter an email address.', variant: 'destructive' })
      return
    }
    inviteMutation.mutate({ email: inviteEmail.trim(), role: inviteRole === 'Admin' ? 0 : 1 })
  }

  const formatDate = (dateStr: string) => new Date(dateStr).toLocaleDateString()

  // Cast response data since some endpoints may have untyped responses
  const members = membersData as unknown as Array<{
    id: string; userId: string; name: string; email: string;
    avatarUrl?: string | null; role: 'Admin' | 'Member'; joinedAtUtc: string;
  }> | undefined

  const invitations = invitationsData as unknown as Array<{
    id: string; email: string; role: 'Admin' | 'Member';
    status: string; expiresAtUtc: string; createdAtUtc: string;
  }> | undefined

  if (!orgId) {
    return (
      <main>
        <h1 className="text-3xl font-bold mb-6">Members</h1>
        <p className="text-muted-foreground">Select an organization to manage members.</p>
      </main>
    )
  }

  return (
    <main>
      <header className="mb-6">
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

      <div className="space-y-4">
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
          <Card>
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
      </div>

      {/* Invite Dialog */}
      <Dialog open={inviteDialogOpen} onOpenChange={(open) => { if (!open) handleCloseInviteDialog() }}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Invite a Member</DialogTitle>
            <DialogDescription>
              Enter an email address to send an invitation.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">Email address</label>
              <Input
                type="email"
                value={inviteEmail}
                onChange={(e) => setInviteEmail(e.target.value)}
                placeholder="colleague@example.com"
                onKeyDown={(e) => e.key === 'Enter' && handleInvite()}
              />
            </div>

            <div className="space-y-2">
              <label className="text-sm font-medium">Role</label>
              <Select value={inviteRole} onValueChange={setInviteRole}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="Member">Member - can view and deploy projects</SelectItem>
                  <SelectItem value="Admin">Admin - full access including billing and members</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={handleCloseInviteDialog}>Cancel</Button>
            <Button
              onClick={handleInvite}
              disabled={inviteMutation.isPending || !inviteEmail.trim()}
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
              onClick={() => removeMemberId && (removeMemberMutation.mutate as any)({ path: { organizationId: orgId!, memberId: removeMemberId } })}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Remove Member
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </main>
  )
}
