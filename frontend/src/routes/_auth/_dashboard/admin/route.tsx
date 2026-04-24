import { createFileRoute, Outlet, redirect } from '@tanstack/react-router'

export const Route = createFileRoute('/_auth/_dashboard/admin')({
  beforeLoad: ({ context }) => {
    if (!context.auth.user?.roles?.includes('admin')) {
      throw redirect({ to: '/projects' })
    }
  },
  component: AdminLayout,
})

function AdminLayout() {
  return <Outlet />
}
