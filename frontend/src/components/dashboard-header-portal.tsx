import { useEffect, useState } from 'react'
import { createPortal } from 'react-dom'

/**
 * Hook that returns a DOM element by ID, re-checking after mount
 * to handle cases where the target element isn't in the DOM yet
 * during the initial render (e.g. parent layout not mounted yet).
 */
function usePortalSlot(id: string) {
  const [slot, setSlot] = useState<HTMLElement | null>(() => document.getElementById(id))

  useEffect(() => {
    const el = document.getElementById(id)
    if (el !== slot) {
      setSlot(el)
    }
  }, [id, slot])

  return slot
}

/**
 * Renders children into the dashboard header breadcrumb row.
 * Sits inline with the sidebar trigger in a single horizontal row.
 */
export function DashboardHeaderBreadcrumb({ children }: { children: React.ReactNode }) {
  const slot = usePortalSlot('dashboard-header-breadcrumb')
  if (!slot) return null
  return createPortal(children, slot)
}

/**
 * Renders children into the dashboard header tabs row.
 * Sits below the breadcrumb row, full-width. Hidden when empty via CSS.
 */
export function DashboardHeaderTabs({ children }: { children: React.ReactNode }) {
  const slot = usePortalSlot('dashboard-header-tabs')
  if (!slot) return null
  return createPortal(children, slot)
}
