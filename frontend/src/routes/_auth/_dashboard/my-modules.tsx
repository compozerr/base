import { createFileRoute, Link } from '@tanstack/react-router'
import { useCallback, useEffect, useState } from 'react'
import { motion } from 'framer-motion'
import {
  Package,
  Download,
  ArrowRight,
  ExternalLink,
  Search,
  Plus,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import {
  searchModules,
  type SearchModuleItem,
} from '@/lib/marketplace-api'

export const Route = createFileRoute('/_auth/_dashboard/my-modules')({
  component: MyModulesPage,
})

// -- Deterministic color from name --
const MODULE_COLORS = [
  { bg: 'from-violet-500/20 to-fuchsia-500/20', border: 'border-violet-500/20', text: 'text-violet-400' },
  { bg: 'from-sky-500/20 to-cyan-500/20', border: 'border-sky-500/20', text: 'text-sky-400' },
  { bg: 'from-emerald-500/20 to-teal-500/20', border: 'border-emerald-500/20', text: 'text-emerald-400' },
  { bg: 'from-amber-500/20 to-orange-500/20', border: 'border-amber-500/20', text: 'text-amber-400' },
  { bg: 'from-rose-500/20 to-pink-500/20', border: 'border-rose-500/20', text: 'text-rose-400' },
  { bg: 'from-indigo-500/20 to-blue-500/20', border: 'border-indigo-500/20', text: 'text-indigo-400' },
]

function getModuleColor(name: string) {
  let hash = 0
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash)
  }
  return MODULE_COLORS[Math.abs(hash) % MODULE_COLORS.length]!
}

function formatInstallCount(count: number): string {
  if (count >= 1000) return `${(count / 1000).toFixed(1)}k`
  return String(count)
}

function MyModulesPage() {
  // For now, we fetch all modules as a proxy for "your published modules."
  // In a real implementation, filter by current user's organization.
  const [modules, setModules] = useState<SearchModuleItem[]>([])
  const [isLoading, setIsLoading] = useState(true)

  const fetchModules = useCallback(async () => {
    setIsLoading(true)
    try {
      const result = await searchModules({ limit: 20 })
      setModules(result.modules)
    } catch {
      setModules([])
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    fetchModules()
  }, [fetchModules])

  return (
    <main>
      <h1 className="text-3xl font-bold mb-6">Modules</h1>

      <div className="space-y-8">
        {/* Published modules section */}
        <section>
          <div className="flex items-center justify-between mb-4">
            <div>
              <h2 className="text-lg font-semibold">Your Modules</h2>
              <p className="text-sm text-white/35 mt-0.5">
                Modules published under your organization
              </p>
            </div>
            <Button
              variant="outline"
              size="sm"
              className="gap-2 border-white/[0.08] text-white/50 hover:text-white/70"
              onClick={() =>
                window.open(
                  'https://docs.compozerr.com/modules/publishing',
                  '_blank',
                )
              }
            >
              <Plus className="h-3.5 w-3.5" />
              Publish Module
            </Button>
          </div>

          {isLoading ? (
            <div className="grid gap-4 sm:grid-cols-2">
              {Array.from({ length: 4 }).map((_, i) => (
                <Card key={i}>
                  <CardContent className="p-5">
                    <div className="flex items-start gap-3.5">
                      <Skeleton className="h-10 w-10 rounded-lg shrink-0" />
                      <div className="flex-1 space-y-2">
                        <Skeleton className="h-4 w-28" />
                        <Skeleton className="h-3 w-full" />
                        <Skeleton className="h-3 w-3/4" />
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          ) : modules.length === 0 ? (
            <Card>
              <CardContent className="flex flex-col items-center justify-center py-16 text-center">
                <div className="flex h-14 w-14 items-center justify-center rounded-2xl border border-white/[0.06] bg-white/[0.03]">
                  <Package className="h-6 w-6 text-white/20" />
                </div>
                <h3 className="mt-4 text-base font-semibold text-white/60">
                  No published modules yet
                </h3>
                <p className="mt-1.5 max-w-sm text-sm text-white/30">
                  Package your features into reusable modules and share them
                  with the community.
                </p>
                <Button
                  variant="outline"
                  size="sm"
                  className="mt-5 gap-2 border-white/[0.08]"
                  onClick={() =>
                    window.open(
                      'https://docs.compozerr.com/modules/publishing',
                      '_blank',
                    )
                  }
                >
                  <ArrowRight className="h-3.5 w-3.5" />
                  Publishing guide
                </Button>
              </CardContent>
            </Card>
          ) : (
            <div className="grid gap-4 sm:grid-cols-2">
              {modules.map((mod, idx) => {
                const color = getModuleColor(mod.name)
                return (
                  <motion.div
                    key={`${mod.organization}/${mod.name}`}
                    initial={{ opacity: 0, y: 8 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{
                      duration: 0.3,
                      delay: Math.min(idx * 0.04, 0.2),
                    }}
                  >
                    <Card className="group transition-colors hover:bg-white/[0.02]">
                      <CardContent className="p-5">
                        <div className="flex items-start gap-3.5">
                          {/* Module icon */}
                          {mod.iconUrl ? (
                            <img
                              src={mod.iconUrl}
                              alt={mod.name}
                              className="h-10 w-10 rounded-lg object-cover"
                            />
                          ) : (
                            <div
                              className={cn(
                                'flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-gradient-to-br border',
                                color.bg,
                                color.border,
                              )}
                            >
                              <span
                                className={cn(
                                  'text-sm font-bold uppercase',
                                  color.text,
                                )}
                              >
                                {mod.name.charAt(0)}
                              </span>
                            </div>
                          )}

                          <div className="min-w-0 flex-1">
                            <div className="flex items-center gap-2">
                              <h3 className="truncate text-sm font-semibold text-white/80">
                                {mod.name}
                              </h3>
                              <Badge
                                variant="outline"
                                className="shrink-0 border-white/[0.08] bg-white/[0.03] text-white/35 text-[10px] px-1.5 py-0 font-mono"
                              >
                                v{mod.latestVersion}
                              </Badge>
                            </div>
                            <p className="mt-1 text-xs text-white/30 line-clamp-2 leading-relaxed">
                              {mod.description}
                            </p>

                            <div className="mt-3 flex items-center gap-4">
                              <div className="flex items-center gap-1 text-white/25">
                                <Download className="h-3 w-3" />
                                <span className="text-[11px] font-medium">
                                  {formatInstallCount(mod.installCount)}
                                </span>
                              </div>
                              <Link
                                to="/modules/$org/$name"
                                params={{
                                  org: mod.organization,
                                  name: mod.name,
                                }}
                                className="flex items-center gap-1 text-[11px] font-medium text-white/25 hover:text-white/50 transition-colors"
                              >
                                <ExternalLink className="h-3 w-3" />
                                View
                              </Link>
                            </div>
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  </motion.div>
                )
              })}
            </div>
          )}
        </section>

        {/* Browse marketplace CTA */}
        <section>
          <Card className="relative overflow-hidden">
            <div className="pointer-events-none absolute inset-0 bg-gradient-to-br from-white/[0.02] via-transparent to-transparent" />
            <CardContent className="relative flex flex-col sm:flex-row items-center justify-between gap-4 p-6">
              <div className="flex items-center gap-4">
                <div className="flex h-11 w-11 items-center justify-center rounded-xl border border-white/[0.06] bg-white/[0.03]">
                  <Search className="h-5 w-5 text-white/30" />
                </div>
                <div>
                  <h3 className="text-base font-semibold">
                    Browse the Marketplace
                  </h3>
                  <p className="text-sm text-white/35">
                    Discover production-ready modules for your projects
                  </p>
                </div>
              </div>
              <Button asChild variant="outline" className="gap-2 border-white/[0.08] shrink-0">
                <Link to="/modules">
                  Explore modules
                  <ArrowRight className="h-3.5 w-3.5" />
                </Link>
              </Button>
            </CardContent>
          </Card>
        </section>
      </div>
    </main>
  )
}
