import { Home, Package, Settings, Building2, Key } from "lucide-react"
import { useRegisterActions } from "./action-registry"
import type { Action } from "./types"

type NavigateFn = (opts: { to: string; params?: Record<string, string> }) => void

export function useGlobalActions(navigate: NavigateFn) {
  const actions: Action[] = [
    {
      id: "nav-projects",
      name: "Go to Projects",
      icon: Home,
      group: "navigation",
      keywords: ["home", "dashboard", "services"],
      perform: () => navigate({ to: "/projects" }),
    },
    {
      id: "nav-settings",
      name: "Go to Settings",
      icon: Settings,
      group: "navigation",
      keywords: ["account", "preferences", "tokens", "billing"],
      perform: () => navigate({ to: "/settings" }),
    },
    {
      id: "nav-organizations",
      name: "Go to Organizations",
      icon: Building2,
      group: "navigation",
      keywords: ["orgs", "teams", "members"],
      perform: () => navigate({ to: "/organizations" }),
    },
    {
      id: "nav-modules",
      name: "Go to Modules",
      icon: Package,
      group: "navigation",
      keywords: ["marketplace", "plugins", "extensions"],
      perform: () => navigate({ to: "/my-modules" }),
    },
    {
      id: "action-create-api-token",
      name: "Create API Token",
      icon: Key,
      group: "action",
      keywords: ["token", "api", "key", "auth"],
      priority: 80,
      perform: () => navigate({ to: "/settings" }),
    },
  ]

  useRegisterActions(actions, [navigate])
}
