import { apiBaseUrl } from "@/api-client"
import { api } from "@/api-client"
import {
  getCurrentOrgId,
  setCurrentOrgId as persistOrgId,
  orgFetch,
} from "@/lib/org-storage"
import { useAuth } from "@/hooks/use-dynamic-auth"
import { useNavigate } from "@tanstack/react-router"
import { AnimatePresence, motion } from "framer-motion"
import {
  Building2,
  Check,
  ChevronDown,
  Settings,
  User,
} from "lucide-react"
import { useEffect, useRef, useState } from "react"
import { useQuery } from "@tanstack/react-query"

interface Organization {
  id: string
  name: string
  slug: string
  isPersonal: boolean
  avatarUrl?: string | null
}

export function MobileOrgPicker() {
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const { user } = useAuth()

  const { data: organizations } = useQuery<Organization[]>({
    queryKey: ["organizations"],
    queryFn: async () => {
      const res = await orgFetch(`${apiBaseUrl}/v1/organizations`)
      if (!res.ok) return []
      return res.json()
    },
    staleTime: 60_000,
  })

  const [currentOrgId, setCurrentOrgId] = useState<string | null>(() =>
    getCurrentOrgId()
  )

  useEffect(() => {
    if (!organizations || organizations.length === 0) return
    if (currentOrgId && organizations.find((o) => o.id === currentOrgId)) return
    const personal =
      organizations.find((o) => o.isPersonal) ?? organizations[0]
    if (personal) {
      setCurrentOrgId(personal.id)
      persistOrgId(personal.id)
    }
  }, [organizations, currentOrgId])

  const currentOrg = organizations?.find((o) => o.id === currentOrgId)

  const switchOrg = (org: Organization) => {
    setCurrentOrgId(org.id)
    persistOrgId(org.id)
    setOpen(false)
    api.v1.getProjects.invalidateQueries()
  }

  // Don't render if only one org
  const hasMultipleOrgs = organizations && organizations.length > 1

  return (
    <div ref={containerRef} className="relative md:hidden">
      {/* Trigger bar */}
      <button
        onClick={() => setOpen((v) => !v)}
        className="flex w-full items-center gap-2.5 px-4 py-2.5 transition-colors active:bg-white/[0.04]"
      >
        {/* Org avatar */}
        <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg bg-white/[0.06] ring-1 ring-white/[0.08]">
          {currentOrg?.isPersonal ? (
            user?.avatarUrl ? (
              <img
                src={user.avatarUrl}
                alt=""
                className="h-7 w-7 rounded-lg"
              />
            ) : (
              <User className="h-3.5 w-3.5 text-white/50" />
            )
          ) : (
            <Building2 className="h-3.5 w-3.5 text-white/50" />
          )}
        </div>

        {/* Org name */}
        <span className="flex-1 truncate text-left text-[13px] font-medium text-white/80">
          {currentOrg?.isPersonal
            ? "Personal Account"
            : currentOrg?.name ?? "Select org..."}
        </span>

        {/* Chevron indicator */}
        {hasMultipleOrgs && (
          <motion.div
            animate={{ rotate: open ? 180 : 0 }}
            transition={{ duration: 0.15, ease: "easeInOut" }}
          >
            <ChevronDown className="h-3.5 w-3.5 text-white/30" />
          </motion.div>
        )}
      </button>

      {/* Separator line */}
      <div className="h-px bg-gradient-to-r from-transparent via-white/[0.06] to-transparent" />

      {/* Dropdown */}
      <AnimatePresence>
        {open && (
          <>
            {/* Backdrop */}
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              transition={{ duration: 0.15 }}
              className="fixed inset-0 z-40 bg-black/60 backdrop-blur-[2px]"
              onClick={() => setOpen(false)}
            />

            {/* Dropdown panel */}
            <motion.div
              initial={{ opacity: 0, y: -8, scale: 0.98 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: -8, scale: 0.98 }}
              transition={{ duration: 0.18, ease: [0.23, 1, 0.32, 1] }}
              className="absolute left-3 right-3 top-full z-50 mt-1 overflow-hidden rounded-xl border border-white/[0.08] bg-zinc-900/95 shadow-2xl shadow-black/40 backdrop-blur-xl"
            >
              {/* Org list */}
              <div className="p-1.5">
                {organizations?.map((org) => {
                  const isSelected = org.id === currentOrgId
                  return (
                    <button
                      key={org.id}
                      onClick={() => switchOrg(org)}
                      className={`flex w-full items-center gap-3 rounded-lg px-3 py-2.5 transition-colors ${
                        isSelected
                          ? "bg-white/[0.06]"
                          : "active:bg-white/[0.04]"
                      }`}
                    >
                      {/* Avatar */}
                      <div
                        className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-lg ${
                          isSelected
                            ? "bg-white/[0.1] ring-1 ring-white/[0.12]"
                            : "bg-white/[0.04]"
                        }`}
                      >
                        {org.isPersonal ? (
                          user?.avatarUrl ? (
                            <img
                              src={user.avatarUrl}
                              alt=""
                              className="h-8 w-8 rounded-lg"
                            />
                          ) : (
                            <User className="h-4 w-4 text-white/50" />
                          )
                        ) : (
                          <Building2 className="h-4 w-4 text-white/50" />
                        )}
                      </div>

                      {/* Name */}
                      <span
                        className={`flex-1 truncate text-left text-[13px] ${
                          isSelected
                            ? "font-medium text-white"
                            : "text-white/60"
                        }`}
                      >
                        {org.isPersonal ? "Personal Account" : org.name}
                      </span>

                      {/* Check */}
                      {isSelected && (
                        <Check className="h-4 w-4 shrink-0 text-white/50" />
                      )}
                    </button>
                  )
                })}
              </div>

              {/* Divider + Manage link */}
              <div className="border-t border-white/[0.06] p-1.5">
                <button
                  onClick={() => {
                    setOpen(false)
                    navigate({ to: "/organizations" })
                  }}
                  className="flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-white/40 transition-colors active:bg-white/[0.04]"
                >
                  <Settings className="h-4 w-4" />
                  <span className="text-[13px]">Manage Organizations</span>
                </button>
              </div>
            </motion.div>
          </>
        )}
      </AnimatePresence>
    </div>
  )
}
