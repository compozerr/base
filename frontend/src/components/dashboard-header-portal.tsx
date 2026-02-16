import { createPortal } from 'react-dom'

/**
 * Renders children into the dashboard header breadcrumb row.
 * Sits inline with the sidebar trigger in a single horizontal row.
 */
export function DashboardHeaderBreadcrumb({ children }: { children: React.ReactNode }) {
  const slot = document.getElementById('dashboard-header-breadcrumb')
  if (!slot) return null
  return createPortal(children, slot)
}

/**
 * Renders children into the dashboard header tabs row.
 * Sits below the breadcrumb row, full-width. Hidden when empty via CSS.
 */
export function DashboardHeaderTabs({ children }: { children: React.ReactNode }) {
  const slot = document.getElementById('dashboard-header-tabs')
  if (!slot) return null
  return createPortal(children, slot)
}
