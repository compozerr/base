import { getLink } from "@/links"
import { useNavigate, useRouterState } from "@tanstack/react-router"
import { FileText, Home, Settings, User } from "lucide-react"

const navItems = [
  { label: "Projects", icon: Home, path: "/projects" as const },
  { label: "Settings", icon: Settings, path: "/settings" as const },
  { label: "Docs", icon: FileText, path: "docs" as const },
  { label: "Profile", icon: User, path: "/settings" as const },
] as const

export function MobileBottomNav() {
  const navigate = useNavigate()
  const routerState = useRouterState()
  const currentPath = routerState.location.pathname

  const handleNav = (item: (typeof navItems)[number]) => {
    if (item.path === "docs") {
      window.open(getLink("documentation"), "_blank")
      return
    }
    navigate({ viewTransition: true, to: item.path })
  }

  const isActive = (item: (typeof navItems)[number]) => {
    if (item.path === "docs") return false
    return currentPath.startsWith(item.path)
  }

  return (
    <nav
      className="fixed bottom-0 left-0 right-0 z-50 flex items-center justify-around border-t border-white/[0.08] bg-black/90 backdrop-blur-lg px-2 pb-[env(safe-area-inset-bottom)] md:hidden"
      role="navigation"
      aria-label="Mobile navigation"
    >
      {navItems.map((item) => {
        const active = isActive(item)
        return (
          <button
            key={item.label}
            onClick={() => handleNav(item)}
            className={`flex flex-1 flex-col items-center gap-0.5 py-2.5 text-[10px] font-medium transition-colors ${
              active
                ? "text-[hsl(var(--nav-accent))]"
                : "text-white/45 active:text-white/70"
            }`}
            aria-current={active ? "page" : undefined}
          >
            <item.icon className="h-5 w-5" strokeWidth={active ? 2 : 1.5} />
            <span>{item.label}</span>
          </button>
        )
      })}
    </nav>
  )
}
