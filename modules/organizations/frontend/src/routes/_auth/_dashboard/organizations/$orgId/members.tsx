import { createFileRoute } from '@tanstack/react-router'
import { Plus, Shield, User, MoreVertical, Mail } from 'lucide-react'
import { useState } from 'react'

export const Route = createFileRoute('/_auth/_dashboard/organizations/$orgId/members')({
  component: MembersPage,
})

function MembersPage() {
  const { orgId } = Route.useParams()
  const [showInviteDialog, setShowInviteDialog] = useState(false)
  const [inviteEmail, setInviteEmail] = useState('')
  const [inviteRole, setInviteRole] = useState<'Admin' | 'Member'>('Member')

  return (
    <div className="mx-auto max-w-3xl px-6 py-10">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-semibold text-white">Members</h1>
          <p className="mt-1 text-sm text-white/50">
            Manage who has access to this organization
          </p>
        </div>
        <button
          onClick={() => setShowInviteDialog(true)}
          className="inline-flex items-center gap-2 rounded-lg bg-white px-4 py-2 text-sm font-medium text-black hover:bg-white/90 transition-colors"
        >
          <Plus className="h-4 w-4" />
          Invite Member
        </button>
      </div>

      {/* Members list placeholder */}
      <div className="rounded-lg border border-white/[0.08] overflow-hidden">
        <div className="border-b border-white/[0.06] px-4 py-3">
          <p className="text-xs font-medium uppercase tracking-wider text-white/40">Members</p>
        </div>
        <div className="divide-y divide-white/[0.06]">
          {/* Placeholder member row */}
          <div className="flex items-center gap-3 px-4 py-3">
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-white/[0.08]">
              <User className="h-4 w-4 text-white/50" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-white">You</p>
              <p className="text-xs text-white/40">Owner</p>
            </div>
            <span className="inline-flex items-center gap-1 rounded-full bg-white/[0.06] px-2.5 py-0.5 text-xs font-medium text-white/60">
              <Shield className="h-3 w-3" />
              Admin
            </span>
          </div>
        </div>
      </div>

      {/* Invite dialog */}
      {showInviteDialog && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
          <div className="w-full max-w-md rounded-xl border border-white/[0.08] bg-neutral-900 p-6 shadow-2xl">
            <h2 className="text-lg font-semibold text-white mb-1">Invite a member</h2>
            <p className="text-sm text-white/50 mb-6">
              They'll receive an email invitation to join your organization.
            </p>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-white/70 mb-1.5">Email address</label>
                <div className="relative">
                  <Mail className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-white/30" />
                  <input
                    type="email"
                    value={inviteEmail}
                    onChange={(e) => setInviteEmail(e.target.value)}
                    placeholder="colleague@company.com"
                    className="w-full rounded-lg border border-white/[0.08] bg-white/[0.04] pl-10 pr-4 py-2.5 text-sm text-white placeholder-white/30 focus:border-white/20 focus:outline-none focus:ring-1 focus:ring-white/20"
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-white/70 mb-1.5">Role</label>
                <div className="flex gap-2">
                  {(['Member', 'Admin'] as const).map((role) => (
                    <button
                      key={role}
                      onClick={() => setInviteRole(role)}
                      className={`flex-1 rounded-lg border px-3 py-2 text-sm font-medium transition-colors ${
                        inviteRole === role
                          ? 'border-white/20 bg-white/[0.08] text-white'
                          : 'border-white/[0.06] text-white/50 hover:border-white/[0.1] hover:text-white/70'
                      }`}
                    >
                      {role}
                    </button>
                  ))}
                </div>
              </div>
            </div>
            <div className="mt-6 flex gap-3 justify-end">
              <button
                onClick={() => setShowInviteDialog(false)}
                className="rounded-lg px-4 py-2 text-sm font-medium text-white/60 hover:text-white transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={() => {
                  // TODO: Call API to invite
                  setShowInviteDialog(false)
                  setInviteEmail('')
                }}
                className="rounded-lg bg-white px-4 py-2 text-sm font-medium text-black hover:bg-white/90 transition-colors"
              >
                Send Invitation
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
