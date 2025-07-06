import { useAuth } from "@/hooks/use-dynamic-auth"
import { getLink } from "@/links"
import { useNavigate, useRouter } from "@tanstack/react-router"
import { FileText, Home, LogOut, Settings } from "lucide-react"
import { Button } from "./ui/button"
import { Sidebar, SidebarContent, SidebarFooter, SidebarGroup, SidebarGroupContent, SidebarHeader } from "./ui/sidebar"

export function AppSidebar() {
  const navigate = useNavigate()
  const router = useRouter()

  const { logout, user } = useAuth();

  const handleLogout = () => {
    if (window.confirm('Are you sure you want to logout?')) {
      logout().then(() => {
        router.invalidate().finally(() => {
          navigate({ to: '/' })
        })
      })
    }
  }
  return (
    <Sidebar className="bg-pink-500">
      <SidebarHeader className="border-b">
        <div className="flex h-12 items-center gap-4 px-2">
          <img
            src={user?.avatarUrl}
            alt={`${user?.name}-avatar`}
            className="h-8 w-8 rounded-full"
          />
          <span className="font-bold">Hi, {user?.name}</span>
        </div>
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent className="space-y-1">
            <Button
              variant="ghost"
              onClick={() =>
                navigate({ viewTransition: true, to: '/projects' })
              }
              className="w-full justify-start gap-2"
            >
              <Home className="h-4 w-4" />
              Projects
            </Button>
            <Button
              variant="ghost"
              onClick={() =>
                navigate({ viewTransition: true, to: '/settings' })
              }
              className="w-full justify-start gap-2"
            >
              <Settings className="h-4 w-4" />
              Settings
            </Button>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <div className="flex flex-col items-center pb-3">
          <Button
            onClick={() => open(getLink('documentation'), '_blank')}
            variant="ghost"
            className="w-full justify-start gap-2"
          >
            <FileText className="h-4 w-4" />
            Documentation
          </Button>
          <Button
            onClick={handleLogout}
            variant="ghost"
            className="w-full justify-start gap-2"
          >
            <LogOut className="h-4 w-4" />
            Logout
          </Button>
        </div>
      </SidebarFooter>
    </Sidebar>
  )
}

