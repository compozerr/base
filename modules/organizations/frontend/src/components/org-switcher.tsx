import { useOrganization } from './org-provider'
import { ChevronDown, Plus, Building2, User } from 'lucide-react'
import { useState } from 'react'

export function OrgSwitcher() {
  const { organizations, currentOrganization, setCurrentOrganization } = useOrganization()
  const [open, setOpen] = useState(false)

  if (!currentOrganization) return null

  return (
    <div className="relative">
      <button
        onClick={() => setOpen(!open)}
        className="flex w-full items-center gap-2 rounded-lg px-3 py-2 text-sm text-white/80 hover:bg-white/[0.06] hover:text-white transition-colors"
      >
        {currentOrganization.isPersonal ? (
          <User className="h-4 w-4 shrink-0 text-white/40" />
        ) : (
          <Building2 className="h-4 w-4 shrink-0 text-white/40" />
        )}
        <span className="truncate flex-1 text-left font-medium">
          {currentOrganization.isPersonal ? 'Personal Account' : currentOrganization.name}
        </span>
        <ChevronDown className={`h-3.5 w-3.5 shrink-0 text-white/40 transition-transform ${open ? 'rotate-180' : ''}`} />
      </button>

      {open && (
        <>
          <div className="fixed inset-0 z-40" onClick={() => setOpen(false)} />
          <div className="absolute left-0 right-0 top-full z-50 mt-1 rounded-lg border border-white/[0.08] bg-neutral-900 p-1 shadow-xl">
            {organizations.map((org) => (
              <button
                key={org.id}
                onClick={() => {
                  setCurrentOrganization(org)
                  setOpen(false)
                }}
                className={`flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm transition-colors ${
                  org.id === currentOrganization.id
                    ? 'bg-white/[0.08] text-white'
                    : 'text-white/60 hover:bg-white/[0.04] hover:text-white'
                }`}
              >
                {org.isPersonal ? (
                  <User className="h-3.5 w-3.5 shrink-0" />
                ) : (
                  <Building2 className="h-3.5 w-3.5 shrink-0" />
                )}
                <span className="truncate">{org.isPersonal ? 'Personal Account' : org.name}</span>
              </button>
            ))}
            <div className="my-1 border-t border-white/[0.06]" />
            <a
              href="/organizations"
              className="flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm text-white/50 hover:bg-white/[0.04] hover:text-white transition-colors"
            >
              <Plus className="h-3.5 w-3.5" />
              <span>Create Organization</span>
            </a>
          </div>
        </>
      )}
    </div>
  )
}
