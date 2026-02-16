import { createContext, useContext, useState, type ReactNode } from 'react'

interface DashboardHeaderContextType {
  headerContent: ReactNode | null
  setHeaderContent: (content: ReactNode | null) => void
}

const DashboardHeaderContext = createContext<DashboardHeaderContextType>({
  headerContent: null,
  setHeaderContent: () => {},
})

export function DashboardHeaderProvider({ children }: { children: ReactNode }) {
  const [headerContent, setHeaderContent] = useState<ReactNode | null>(null)
  return (
    <DashboardHeaderContext.Provider value={{ headerContent, setHeaderContent }}>
      {children}
    </DashboardHeaderContext.Provider>
  )
}

export function useDashboardHeader() {
  return useContext(DashboardHeaderContext)
}
