import { createPortal } from 'react-dom'

/**
 * Renders children into the dashboard header slot via portal.
 * The slot is the #dashboard-header-slot div in the dashboard layout.
 */
export function DashboardHeaderPortal({ children }: { children: React.ReactNode }) {
  const slot = document.getElementById('dashboard-header-slot')
  if (!slot) return null
  return createPortal(children, slot)
}
