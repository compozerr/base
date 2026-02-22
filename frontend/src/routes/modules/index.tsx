import { createFileRoute, Link } from '@tanstack/react-router'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import {
  Search,
  Download,
  ArrowRight,
  Package,
  X,
  Loader2,
} from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import Navbar from '@/components/navbar'
import Footer from '@/components/footer'
import SEO from '@/components/seo'
import { cn } from '@/lib/utils'
import {
  searchModules,
  type SearchModuleItem,
} from '@/lib/marketplace-api'

export const Route = createFileRoute('/modules/')({
  component: MarketplaceBrowse,
})

// -- Module icon color mapping (deterministic from name) --
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

// -- Known tags for quick filtering --
const POPULAR_TAGS = [
  'auth',
  'database',
  'billing',
  'payments',
  'email',
  'storage',
  'analytics',
  'api',
  'security',
  'ui',
]

function MarketplaceBrowse() {
  const [query, setQuery] = useState('')
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [modules, setModules] = useState<SearchModuleItem[]>([])
  const [total, setTotal] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [debouncedQuery, setDebouncedQuery] = useState('')

  // Debounce search input
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedQuery(query), 300)
    return () => clearTimeout(timer)
  }, [query])

  // Fetch modules
  const fetchModules = useCallback(async () => {
    setIsLoading(true)
    try {
      const result = await searchModules({
        query: debouncedQuery || undefined,
        tags: selectedTags.length > 0 ? selectedTags : undefined,
        limit: 30,
      })
      setModules(result.modules)
      setTotal(result.total)
    } catch {
      setModules([])
      setTotal(0)
    } finally {
      setIsLoading(false)
    }
  }, [debouncedQuery, selectedTags])

  useEffect(() => {
    fetchModules()
  }, [fetchModules])

  const toggleTag = (tag: string) => {
    setSelectedTags((prev) =>
      prev.includes(tag) ? prev.filter((t) => t !== tag) : [...prev, tag],
    )
  }

  // Collect all tags from results for the filter bar, merged with popular tags
  const allTags = useMemo(() => {
    const resultTags = new Set<string>()
    modules.forEach((m) => m.tags.forEach((t) => resultTags.add(t)))
    POPULAR_TAGS.forEach((t) => resultTags.add(t))
    return Array.from(resultTags).sort()
  }, [modules])

  const structuredData = {
    '@context': 'https://schema.org',
    '@type': 'ItemList',
    name: 'Compozerr Module Marketplace',
    description:
      'Browse production-ready modules for auth, billing, database, and more.',
    numberOfItems: total,
    itemListElement: modules.slice(0, 10).map((mod, i) => ({
      '@type': 'ListItem',
      position: i + 1,
      item: {
        '@type': 'SoftwareApplication',
        name: mod.name,
        description: mod.description,
        applicationCategory: 'DeveloperApplication',
        operatingSystem: 'Cross-platform',
        offers: { '@type': 'Offer', price: '0', priceCurrency: 'USD' },
      },
    })),
  }

  return (
    <div className="relative min-h-screen bg-background">
      <SEO
        title="Module Marketplace"
        description="Browse and discover production-ready modules for your full-stack projects. Auth, billing, database, and more."
        canonical="/modules"
        structuredData={structuredData}
      />
      <Navbar />

      {/* -- Hero section -- */}
      <section className="relative overflow-hidden border-b border-white/[0.06]">
        {/* Background texture */}
        <div className="absolute inset-0 bg-grid-pattern-fine mask-fade-out opacity-30" />
        <div className="pointer-events-none absolute left-1/2 top-0 -translate-x-1/2 h-[500px] w-[800px] bg-gradient-to-b from-white/[0.03] to-transparent blur-[100px] rounded-full" />

        <div className="container relative max-w-6xl pt-24 pb-16 sm:pt-32 sm:pb-20">
          {/* Eyebrow */}
          <div
            className="animate-fade-up mb-5 inline-flex items-center gap-2 rounded-full border border-white/[0.08] bg-white/[0.03] px-4 py-1.5 text-xs font-medium text-white/50"
            style={{ animationDelay: '0ms' }}
          >
            <Package className="h-3 w-3" />
            {total > 0 ? `${total} modules available` : 'Module Marketplace'}
          </div>

          {/* Heading */}
          <h1
            className="animate-fade-up text-4xl font-bold tracking-tight sm:text-5xl lg:text-6xl"
            style={{ animationDelay: '80ms' }}
          >
            Find the perfect{' '}
            <span className="bg-gradient-to-r from-white via-white/80 to-white/50 bg-clip-text text-transparent">
              module
            </span>
          </h1>

          <p
            className="animate-fade-up mt-4 max-w-xl text-base text-white/45 leading-relaxed sm:text-lg"
            style={{ animationDelay: '160ms' }}
          >
            Production-ready building blocks for your stack. Install in seconds,
            ship in minutes.
          </p>

          {/* Search bar */}
          <div
            className="animate-fade-up mt-10 max-w-2xl"
            style={{ animationDelay: '240ms' }}
          >
            <div className="relative group">
              <Search className="absolute left-4 top-1/2 -translate-y-1/2 h-4.5 w-4.5 text-white/30 transition-colors group-focus-within:text-white/60" />
              <Input
                type="text"
                placeholder="Search modules..."
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                className="h-13 w-full rounded-xl border-white/[0.08] bg-white/[0.03] pl-12 pr-4 text-[15px] text-white placeholder:text-white/25 focus-visible:ring-1 focus-visible:ring-white/20 focus-visible:border-white/[0.15] transition-all backdrop-blur-sm"
              />
              {query && (
                <button
                  onClick={() => setQuery('')}
                  className="absolute right-4 top-1/2 -translate-y-1/2 p-0.5 rounded-md text-white/30 hover:text-white/60 transition-colors"
                >
                  <X className="h-4 w-4" />
                </button>
              )}
            </div>
          </div>

          {/* Tag pills */}
          <div
            className="animate-fade-up mt-5 flex flex-wrap gap-2"
            style={{ animationDelay: '320ms' }}
          >
            {allTags.map((tag) => {
              const isSelected = selectedTags.includes(tag)
              return (
                <button
                  key={tag}
                  onClick={() => toggleTag(tag)}
                  className={cn(
                    'rounded-full px-3 py-1 text-xs font-medium transition-all',
                    isSelected
                      ? 'bg-white/[0.12] text-white border border-white/[0.15]'
                      : 'bg-white/[0.03] text-white/35 border border-white/[0.06] hover:bg-white/[0.06] hover:text-white/55',
                  )}
                >
                  {tag}
                </button>
              )
            })}
            {selectedTags.length > 0 && (
              <button
                onClick={() => setSelectedTags([])}
                className="rounded-full px-3 py-1 text-xs font-medium text-white/30 hover:text-white/55 transition-colors"
              >
                Clear all
              </button>
            )}
          </div>
        </div>
      </section>

      {/* -- Results section -- */}
      <section className="container max-w-6xl py-12 sm:py-16">
        {/* Result count */}
        <div className="mb-8 flex items-center justify-between">
          <p className="text-sm text-white/35">
            {isLoading ? (
              <span className="inline-flex items-center gap-2">
                <Loader2 className="h-3.5 w-3.5 animate-spin" />
                Searching...
              </span>
            ) : (
              <>
                {total} module{total !== 1 ? 's' : ''} found
              </>
            )}
          </p>
        </div>

        {/* Loading skeleton grid */}
        {isLoading && (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <div
                key={i}
                className="rounded-xl border border-white/[0.06] bg-white/[0.02] p-5"
              >
                <div className="flex items-start gap-3.5">
                  <Skeleton className="h-10 w-10 rounded-lg shrink-0" />
                  <div className="flex-1 space-y-2">
                    <Skeleton className="h-4 w-28" />
                    <Skeleton className="h-3 w-20" />
                  </div>
                </div>
                <Skeleton className="mt-4 h-3 w-full" />
                <Skeleton className="mt-2 h-3 w-3/4" />
                <div className="mt-5 flex items-center gap-2">
                  <Skeleton className="h-5 w-14 rounded-full" />
                  <Skeleton className="h-5 w-14 rounded-full" />
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Empty state */}
        {!isLoading && modules.length === 0 && (
          <motion.div
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            className="flex flex-col items-center justify-center py-24 text-center"
          >
            <div className="flex h-16 w-16 items-center justify-center rounded-2xl border border-white/[0.06] bg-white/[0.03]">
              <Package className="h-7 w-7 text-white/20" />
            </div>
            <h3 className="mt-5 text-lg font-semibold text-white/70">
              No modules found
            </h3>
            <p className="mt-2 max-w-sm text-sm text-white/35">
              {query || selectedTags.length > 0
                ? 'Try adjusting your search or filters.'
                : 'Modules will appear here once they are published.'}
            </p>
            {(query || selectedTags.length > 0) && (
              <button
                onClick={() => {
                  setQuery('')
                  setSelectedTags([])
                }}
                className="mt-4 text-sm font-medium text-white/50 hover:text-white/70 transition-colors"
              >
                Clear search and filters
              </button>
            )}
          </motion.div>
        )}

        {/* Module grid */}
        {!isLoading && modules.length > 0 && (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <AnimatePresence mode="popLayout">
              {modules.map((mod, idx) => (
                <ModuleCard key={`${mod.organization}/${mod.name}`} module={mod} index={idx} />
              ))}
            </AnimatePresence>
          </div>
        )}
      </section>

      {/* CTA section */}
      <section className="border-t border-white/[0.06] py-20">
        <div className="container max-w-6xl text-center">
          <h2 className="text-2xl font-bold tracking-tight sm:text-3xl">
            Build your own module
          </h2>
          <p className="mx-auto mt-3 max-w-lg text-sm text-white/40 leading-relaxed">
            Package your backend features and frontend components into reusable
            modules. Share them with the community or keep them private.
          </p>
          <div className="mt-8 flex flex-col sm:flex-row items-center justify-center gap-3">
            <a
              href="https://docs.compozerr.com/modules/publishing"
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex h-10 items-center gap-2 rounded-lg bg-white px-5 text-sm font-medium text-black transition-colors hover:bg-white/90"
            >
              Publishing guide
              <ArrowRight className="h-3.5 w-3.5" />
            </a>
          </div>
        </div>
      </section>

      <Footer />
    </div>
  )
}

// -- Module Card component --
function ModuleCard({
  module: mod,
  index,
}: {
  module: SearchModuleItem
  index: number
}) {
  const color = getModuleColor(mod.name)

  return (
    <motion.div
      layout
      initial={{ opacity: 0, y: 16 }}
      animate={{ opacity: 1, y: 0 }}
      exit={{ opacity: 0, scale: 0.96 }}
      transition={{ duration: 0.35, delay: Math.min(index * 0.05, 0.3) }}
    >
      <Link
        to="/modules/$org/$name"
        params={{ org: mod.organization, name: mod.name }}
        className="group block"
      >
        <div className="relative rounded-xl border border-white/[0.06] bg-white/[0.015] p-5 transition-all duration-300 hover:bg-white/[0.035] hover:border-white/[0.1]">
          {/* Hover glow */}
          <div className="pointer-events-none absolute inset-0 rounded-xl opacity-0 transition-opacity duration-300 group-hover:opacity-100 bg-gradient-to-br from-white/[0.02] via-transparent to-transparent" />

          {/* Header */}
          <div className="relative flex items-start gap-3.5">
            {/* Icon */}
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
                <span className={cn('text-sm font-bold uppercase', color.text)}>
                  {mod.name.charAt(0)}
                </span>
              </div>
            )}

            <div className="min-w-0 flex-1">
              <div className="flex items-center gap-2">
                <h3 className="truncate text-sm font-semibold text-white/90 group-hover:text-white transition-colors">
                  {mod.name}
                </h3>
                <Badge
                  variant="outline"
                  className="shrink-0 border-white/[0.08] bg-white/[0.03] text-white/35 text-[10px] px-1.5 py-0 font-mono"
                >
                  v{mod.latestVersion}
                </Badge>
              </div>
              <p className="text-xs text-white/30 mt-0.5">
                {mod.organization}
              </p>
            </div>
          </div>

          {/* Description */}
          <p className="relative mt-3.5 text-[13px] leading-relaxed text-white/40 line-clamp-2">
            {mod.description}
          </p>

          {/* Footer: tags + stats */}
          <div className="relative mt-4 flex items-center justify-between">
            <div className="flex flex-wrap gap-1.5">
              {mod.tags.slice(0, 3).map((tag) => (
                <span
                  key={tag}
                  className="rounded-full bg-white/[0.04] px-2 py-0.5 text-[10px] font-medium text-white/30"
                >
                  {tag}
                </span>
              ))}
              {mod.tags.length > 3 && (
                <span className="rounded-full px-1.5 py-0.5 text-[10px] text-white/20">
                  +{mod.tags.length - 3}
                </span>
              )}
            </div>
            <div className="flex items-center gap-1 text-white/25 shrink-0">
              <Download className="h-3 w-3" />
              <span className="text-[11px] font-medium">
                {formatInstallCount(mod.installCount)}
              </span>
            </div>
          </div>
        </div>
      </Link>
    </motion.div>
  )
}
