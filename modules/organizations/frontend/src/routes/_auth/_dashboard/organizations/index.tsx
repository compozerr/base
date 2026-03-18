import { createFileRoute, Link } from '@tanstack/react-router'
import { Building2, Plus, User, ChevronRight } from 'lucide-react'

export const Route = createFileRoute('/_auth/_dashboard/organizations/')({
  component: OrganizationsPage,
})

function OrganizationsPage() {
  // TODO: Replace with actual API call once OpenAPI client is regenerated
  // const { data } = api.v1.getOrganizations.useQuery()

  return (
    <div className="mx-auto max-w-3xl px-6 py-10">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-semibold text-white">Organizations</h1>
          <p className="mt-1 text-sm text-white/50">
            Manage your organizations and team access
          </p>
        </div>
        <Link
          to="/organizations"
          className="inline-flex items-center gap-2 rounded-lg bg-white px-4 py-2 text-sm font-medium text-black hover:bg-white/90 transition-colors"
        >
          <Plus className="h-4 w-4" />
          New Organization
        </Link>
      </div>

      <div className="space-y-2">
        {/* Placeholder - will be populated from API */}
        <div className="rounded-lg border border-white/[0.08] bg-white/[0.02] p-4">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-white/[0.06]">
              <User className="h-5 w-5 text-white/60" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-white">Personal Account</p>
              <p className="text-xs text-white/40">Your personal projects</p>
            </div>
            <ChevronRight className="h-4 w-4 text-white/20" />
          </div>
        </div>
      </div>
    </div>
  )
}
