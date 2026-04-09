import type { LucideIcon } from "lucide-react"

export type ActionGroup = "action" | "navigation" | "project" | "organization"

export interface Action {
  id: string
  name: string
  icon?: LucideIcon
  group: ActionGroup
  keywords?: string[]
  shortcut?: string[]
  perform: () => void | Promise<void>
  enabled?: boolean | (() => boolean)
  priority?: number
  subtitle?: string
}
