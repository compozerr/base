import { createFileRoute } from '@tanstack/react-router'
import { Building2, Settings, Users, CreditCard } from 'lucide-react'

export const Route = createFileRoute('/_auth/_dashboard/organizations/$orgId/')({
  component: OrganizationDetailPage,
})

function OrganizationDetailPage() {
  const { orgId } = Route.useParams()

  return (
    <div className="mx-auto max-w-4xl px-6 py-10">
      <div className="mb-8">
        <h1 className="text-2xl font-semibold text-white">Organization Settings</h1>
        <p className="mt-1 text-sm text-white/50">
          Manage your organization settings, members, and billing
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <a
          href={`/organizations/${orgId}/members`}
          className="group rounded-lg border border-white/[0.08] bg-white/[0.02] p-6 hover:border-white/[0.12] hover:bg-white/[0.04] transition-all"
        >
          <Users className="h-8 w-8 text-white/40 group-hover:text-white/60 transition-colors mb-3" />
          <h3 className="text-sm font-medium text-white">Members</h3>
          <p className="mt-1 text-xs text-white/40">Invite and manage team members</p>
        </a>

        <div className="group rounded-lg border border-white/[0.08] bg-white/[0.02] p-6 hover:border-white/[0.12] hover:bg-white/[0.04] transition-all">
          <Settings className="h-8 w-8 text-white/40 group-hover:text-white/60 transition-colors mb-3" />
          <h3 className="text-sm font-medium text-white">General</h3>
          <p className="mt-1 text-xs text-white/40">Organization name and settings</p>
        </div>

        <div className="group rounded-lg border border-white/[0.08] bg-white/[0.02] p-6 hover:border-white/[0.12] hover:bg-white/[0.04] transition-all">
          <CreditCard className="h-8 w-8 text-white/40 group-hover:text-white/60 transition-colors mb-3" />
          <h3 className="text-sm font-medium text-white">Billing</h3>
          <p className="mt-1 text-xs text-white/40">Manage subscriptions and invoices</p>
        </div>
      </div>
    </div>
  )
}
