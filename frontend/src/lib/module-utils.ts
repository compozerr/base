// Shared utilities for module marketplace UI

export const MODULE_COLORS = [
  { bg: 'from-violet-500/20 to-fuchsia-500/20', border: 'border-violet-500/20', text: 'text-violet-400' },
  { bg: 'from-sky-500/20 to-cyan-500/20', border: 'border-sky-500/20', text: 'text-sky-400' },
  { bg: 'from-emerald-500/20 to-teal-500/20', border: 'border-emerald-500/20', text: 'text-emerald-400' },
  { bg: 'from-amber-500/20 to-orange-500/20', border: 'border-amber-500/20', text: 'text-amber-400' },
  { bg: 'from-rose-500/20 to-pink-500/20', border: 'border-rose-500/20', text: 'text-rose-400' },
  { bg: 'from-indigo-500/20 to-blue-500/20', border: 'border-indigo-500/20', text: 'text-indigo-400' },
] as const

export function getModuleColor(name: string) {
  let hash = 0
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash)
  }
  return MODULE_COLORS[Math.abs(hash) % MODULE_COLORS.length]!
}

export function formatInstallCount(count: number): string {
  if (count >= 1000) return `${(count / 1000).toFixed(1)}k`
  return String(count)
}
