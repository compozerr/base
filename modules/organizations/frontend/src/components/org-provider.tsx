import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'

export interface Organization {
  id: string
  name: string
  slug: string
  isPersonal: boolean
  avatarUrl?: string | null
}

interface OrganizationContextType {
  organizations: Organization[]
  currentOrganization: Organization | null
  setCurrentOrganization: (org: Organization) => void
  isLoading: boolean
}

const OrganizationContext = createContext<OrganizationContextType>({
  organizations: [],
  currentOrganization: null,
  setCurrentOrganization: () => {},
  isLoading: true,
})

export function useOrganization() {
  return useContext(OrganizationContext)
}

const STORAGE_KEY = 'compozerr-current-org-id'

export function OrganizationProvider({ children, organizations, isLoading }: {
  children: ReactNode
  organizations: Organization[]
  isLoading: boolean
}) {
  const [currentOrganization, setCurrentOrgState] = useState<Organization | null>(null)

  useEffect(() => {
    if (organizations.length === 0) return

    const savedOrgId = localStorage.getItem(STORAGE_KEY)
    const savedOrg = savedOrgId ? organizations.find(o => o.id === savedOrgId) : null

    if (savedOrg) {
      setCurrentOrgState(savedOrg)
    } else {
      // Default to personal org
      const personalOrg = organizations.find(o => o.isPersonal) ?? organizations[0]
      setCurrentOrgState(personalOrg)
      if (personalOrg) localStorage.setItem(STORAGE_KEY, personalOrg.id)
    }
  }, [organizations])

  const setCurrentOrganization = (org: Organization) => {
    setCurrentOrgState(org)
    localStorage.setItem(STORAGE_KEY, org.id)
  }

  return (
    <OrganizationContext.Provider value={{ organizations, currentOrganization, setCurrentOrganization, isLoading }}>
      {children}
    </OrganizationContext.Provider>
  )
}
