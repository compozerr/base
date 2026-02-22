import { createFileRoute, Link } from '@tanstack/react-router'
import { useEffect, useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import {
  ArrowLeft,
  Download,
  ExternalLink,
  Calendar,
  Tag,
  GitCommit,
  ChevronDown,
  Package,
  Copy,
  Check,
  Clock,
} from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import Navbar from '@/components/navbar'
import Footer from '@/components/footer'
import SEO from '@/components/seo'
import { cn } from '@/lib/utils'
import {
  getModuleInfo,
  type ModuleInfoResponse,
  type ModuleVersionDto,
} from '@/lib/marketplace-api'
import { MODULE_COLORS, getModuleColor, formatInstallCount } from '@/lib/module-utils'

export const Route = createFileRoute('/modules/$org/$name')({
  component: ModuleDetail,
})

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

function relativeTime(dateStr: string): string {
  const now = new Date()
  const date = new Date(dateStr)
  const diffMs = now.getTime() - date.getTime()
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays === 0) return 'today'
  if (diffDays === 1) return 'yesterday'
  if (diffDays < 30) return `${diffDays}d ago`
  if (diffDays < 365) return `${Math.floor(diffDays / 30)}mo ago`
  return `${Math.floor(diffDays / 365)}y ago`
}

function ModuleDetail() {
  const { org, name } = Route.useParams()
  const [moduleData, setModuleData] = useState<ModuleInfoResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setIsLoading(true)
    setError(null)
    getModuleInfo(org, name)
      .then((data) => {
        if (!data.found) {
          setError('Module not found')
        } else {
          setModuleData(data)
        }
      })
      .catch((err) => setError(err.message))
      .finally(() => setIsLoading(false))
  }, [org, name])

  const installCommand = `compozerr module add ${org}/${name}`
  const color = getModuleColor(name)

  const structuredData = moduleData
    ? {
        '@context': 'https://schema.org',
        '@type': 'SoftwareApplication',
        name: moduleData.name,
        description: moduleData.description,
        applicationCategory: 'DeveloperApplication',
        operatingSystem: 'Cross-platform',
        softwareVersion: moduleData.latestVersion,
        author: {
          '@type': 'Organization',
          name: moduleData.organization,
        },
        offers: {
          '@type': 'Offer',
          price: '0',
          priceCurrency: 'USD',
        },
      }
    : undefined

  return (
    <div className="relative min-h-screen bg-background">
      <SEO
        title={
          moduleData
            ? `${moduleData.name} by ${moduleData.organization}`
            : `${name} - Module`
        }
        description={
          moduleData?.description ??
          `View ${name} module details on the Compozerr marketplace.`
        }
        canonical={`/modules/${org}/${name}`}
        structuredData={structuredData}
      />
      <Navbar />

      {/* Hero / Header */}
      <section className="relative overflow-hidden border-b border-white/[0.06]">
        <div className="absolute inset-0 bg-grid-pattern-fine mask-fade-out opacity-20" />
        <div className="container relative max-w-6xl pt-12 pb-12 sm:pt-16 sm:pb-14">
          {/* Breadcrumb */}
          <nav
            className="animate-fade-up mb-8 flex items-center gap-2 text-sm text-white/30"
            style={{ animationDelay: '0ms' }}
          >
            <Link
              to="/modules"
              className="flex items-center gap-1.5 hover:text-white/55 transition-colors"
            >
              <ArrowLeft className="h-3.5 w-3.5" />
              Marketplace
            </Link>
            <span className="text-white/15">/</span>
            <span className="text-white/50">
              {org}/{name}
            </span>
          </nav>

          {isLoading ? (
            <ModuleDetailSkeleton />
          ) : error ? (
            <ModuleNotFound error={error} />
          ) : moduleData ? (
            <ModuleHeader
              data={moduleData}
              color={color}
              installCommand={installCommand}
            />
          ) : null}
        </div>
      </section>

      {/* Detail content */}
      {!isLoading && !error && moduleData && (
        <section className="container max-w-6xl py-12 sm:py-16">
          <div className="grid gap-10 lg:grid-cols-[1fr_320px]">
            {/* Main column */}
            <div className="space-y-10">
              {/* Install section */}
              <InstallSection command={installCommand} />

              {/* Version history */}
              {moduleData.versions && moduleData.versions.length > 0 && (
                <VersionHistory versions={moduleData.versions} />
              )}
            </div>

            {/* Sidebar */}
            <div className="space-y-6">
              {/* Quick stats */}
              <div className="rounded-xl border border-white/[0.06] bg-white/[0.015] p-5 space-y-4">
                <h3 className="text-xs font-semibold uppercase tracking-wider text-white/30">
                  Details
                </h3>
                <div className="space-y-3">
                  <StatRow icon={Download} label="Installs" value={formatInstallCount(moduleData.installCount)} />
                  <StatRow icon={Tag} label="Latest" value={`v${moduleData.latestVersion}`} />
                  {moduleData.publishedAt && (
                    <StatRow
                      icon={Calendar}
                      label="Published"
                      value={formatDate(moduleData.publishedAt)}
                    />
                  )}
                  <StatRow icon={Package} label="Organization" value={moduleData.organization ?? org} />
                </div>
              </div>

              {/* Tags */}
              {moduleData.tags && moduleData.tags.length > 0 && (
                <div className="rounded-xl border border-white/[0.06] bg-white/[0.015] p-5 space-y-3">
                  <h3 className="text-xs font-semibold uppercase tracking-wider text-white/30">
                    Tags
                  </h3>
                  <div className="flex flex-wrap gap-1.5">
                    {moduleData.tags.map((tag) => (
                      <Link
                        key={tag}
                        to="/modules"
                        search={{ tag }}
                        className="rounded-full border border-white/[0.06] bg-white/[0.03] px-2.5 py-1 text-xs font-medium text-white/40 hover:text-white/60 hover:border-white/[0.1] transition-all"
                      >
                        {tag}
                      </Link>
                    ))}
                  </div>
                </div>
              )}

              {/* GitHub link */}
              {moduleData.repoUrl && (
                <a
                  href={moduleData.repoUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center justify-center gap-2 rounded-xl border border-white/[0.06] bg-white/[0.015] px-4 py-3 text-sm font-medium text-white/50 transition-all hover:bg-white/[0.04] hover:text-white/70 hover:border-white/[0.1]"
                >
                  <ExternalLink className="h-4 w-4" />
                  View on GitHub
                </a>
              )}
            </div>
          </div>
        </section>
      )}

      <Footer />
    </div>
  )
}

// -- Module header in hero area --
function ModuleHeader({
  data,
  color,
  installCommand: _installCommand,
}: {
  data: ModuleInfoResponse
  color: (typeof MODULE_COLORS)[number]
  installCommand: string
}) {
  return (
    <div className="animate-fade-up" style={{ animationDelay: '80ms' }}>
      <div className="flex items-start gap-5">
        {/* Icon */}
        {data.iconUrl ? (
          <img
            src={data.iconUrl}
            alt={data.name ?? ''}
            className="h-16 w-16 rounded-xl object-cover sm:h-20 sm:w-20"
          />
        ) : (
          <div
            className={cn(
              'flex h-16 w-16 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br border sm:h-20 sm:w-20',
              color.bg,
              color.border,
            )}
          >
            <span
              className={cn(
                'text-2xl font-bold uppercase sm:text-3xl',
                color.text,
              )}
            >
              {(data.name ?? '?').charAt(0)}
            </span>
          </div>
        )}

        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-3">
            <h1 className="text-3xl font-bold tracking-tight sm:text-4xl">
              {data.name}
            </h1>
            <Badge
              variant="outline"
              className="border-white/[0.08] bg-white/[0.03] text-white/40 font-mono text-xs"
            >
              v{data.latestVersion}
            </Badge>
          </div>

          <p className="mt-1 text-sm text-white/35">
            by{' '}
            <span className="text-white/50 font-medium">
              {data.organization}
            </span>
          </p>

          {data.description && (
            <p className="mt-3 max-w-2xl text-base text-white/45 leading-relaxed">
              {data.description}
            </p>
          )}

          {/* Quick stats row */}
          <div className="mt-5 flex flex-wrap items-center gap-5">
            <div className="flex items-center gap-1.5 text-sm text-white/30">
              <Download className="h-3.5 w-3.5" />
              <span>{formatInstallCount(data.installCount)} installs</span>
            </div>
            {data.publishedAt && (
              <div className="flex items-center gap-1.5 text-sm text-white/30">
                <Clock className="h-3.5 w-3.5" />
                <span>Published {relativeTime(data.publishedAt)}</span>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}

// -- Install command section --
function InstallSection({ command }: { command: string }) {
  const [copied, setCopied] = useState(false)

  const handleCopy = async () => {
    await navigator.clipboard.writeText(command)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Installation</h2>
      <div className="relative flex items-center rounded-xl border border-white/[0.06] bg-[hsl(var(--surface))] px-5 py-4 font-mono text-[13px] overflow-hidden group">
        <span className="mr-3 select-none text-white/25 shrink-0">$</span>
        <span className="text-white/70 overflow-x-auto whitespace-nowrap flex-1">
          {command}
        </span>
        <motion.button
          whileTap={{ scale: 0.92 }}
          onClick={handleCopy}
          className="ml-3 shrink-0 rounded-md p-2 hover:bg-white/[0.06] transition-colors"
        >
          <motion.div
            animate={copied ? { scale: [1, 0.8, 1] } : {}}
            transition={{ duration: 0.2 }}
          >
            {copied ? (
              <Check className="h-4 w-4 text-emerald-400" />
            ) : (
              <Copy className="h-4 w-4 text-white/30" />
            )}
          </motion.div>
        </motion.button>
      </div>
    </div>
  )
}

// -- Version history --
function VersionHistory({ versions }: { versions: ModuleVersionDto[] }) {
  const [expandedVersion, setExpandedVersion] = useState<string | null>(null)
  const [showAll, setShowAll] = useState(false)

  const displayVersions = showAll ? versions : versions.slice(0, 5)

  return (
    <div>
      <h2 className="text-lg font-semibold mb-4">Version History</h2>
      <div className="space-y-2">
        {displayVersions.map((version, idx) => {
          const isExpanded = expandedVersion === version.version
          const isLatest = idx === 0

          return (
            <div
              key={version.version}
              className="rounded-xl border border-white/[0.06] bg-white/[0.015] overflow-hidden"
            >
              <button
                onClick={() =>
                  setExpandedVersion(isExpanded ? null : version.version)
                }
                className="flex w-full items-center justify-between px-5 py-3.5 text-left transition-colors hover:bg-white/[0.02]"
              >
                <div className="flex items-center gap-3">
                  <div
                    className={cn(
                      'h-2 w-2 rounded-full',
                      isLatest
                        ? 'bg-emerald-400'
                        : 'bg-white/15',
                    )}
                  />
                  <span className="text-sm font-mono font-medium text-white/70">
                    v{version.version}
                  </span>
                  {isLatest && (
                    <Badge
                      variant="outline"
                      className="border-emerald-500/20 bg-emerald-500/10 text-emerald-400 text-[10px] px-1.5 py-0"
                    >
                      latest
                    </Badge>
                  )}
                  <span className="text-xs text-white/25">
                    {formatDate(version.createdAt)}
                  </span>
                </div>
                <motion.div
                  animate={{ rotate: isExpanded ? 180 : 0 }}
                  transition={{ duration: 0.15 }}
                >
                  <ChevronDown className="h-4 w-4 text-white/20" />
                </motion.div>
              </button>

              <AnimatePresence initial={false}>
                {isExpanded && (
                  <motion.div
                    initial={{ height: 0, opacity: 0 }}
                    animate={{ height: 'auto', opacity: 1 }}
                    exit={{ height: 0, opacity: 0 }}
                    transition={{ duration: 0.2 }}
                    className="overflow-hidden"
                  >
                    <div className="border-t border-white/[0.04] px-5 py-4 space-y-3">
                      {version.releaseNotes && (
                        <p className="text-sm text-white/45 leading-relaxed">
                          {version.releaseNotes}
                        </p>
                      )}
                      <div className="flex items-center gap-2 text-xs text-white/25">
                        <GitCommit className="h-3 w-3" />
                        <span className="font-mono">
                          {version.commitHash.slice(0, 8)}
                        </span>
                      </div>
                    </div>
                  </motion.div>
                )}
              </AnimatePresence>
            </div>
          )
        })}
      </div>

      {!showAll && versions.length > 5 && (
        <Button
          variant="ghost"
          onClick={() => setShowAll(true)}
          className="mt-3 w-full text-white/35 hover:text-white/55"
        >
          Show all {versions.length} versions
        </Button>
      )}
    </div>
  )
}

// -- Stat row for sidebar --
function StatRow({
  icon: Icon,
  label,
  value,
}: {
  icon: React.ComponentType<{ className?: string }>
  label: string
  value: string
}) {
  return (
    <div className="flex items-center justify-between">
      <div className="flex items-center gap-2 text-sm text-white/30">
        <Icon className="h-3.5 w-3.5" />
        <span>{label}</span>
      </div>
      <span className="text-sm font-medium text-white/60">{value}</span>
    </div>
  )
}

// -- Loading skeleton --
function ModuleDetailSkeleton() {
  return (
    <div className="flex items-start gap-5">
      <Skeleton className="h-16 w-16 rounded-xl sm:h-20 sm:w-20 shrink-0" />
      <div className="flex-1 space-y-3">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-4 w-32" />
        <Skeleton className="h-4 w-96" />
        <div className="flex gap-5 pt-2">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-4 w-28" />
        </div>
      </div>
    </div>
  )
}

// -- Not found state --
function ModuleNotFound({ error }: { error: string }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      className="flex flex-col items-center justify-center py-16 text-center"
    >
      <div className="flex h-16 w-16 items-center justify-center rounded-2xl border border-white/[0.06] bg-white/[0.03]">
        <Package className="h-7 w-7 text-white/20" />
      </div>
      <h2 className="mt-5 text-xl font-semibold text-white/70">
        Module not found
      </h2>
      <p className="mt-2 text-sm text-white/35">{error}</p>
      <Link
        to="/modules"
        className="mt-6 inline-flex items-center gap-1.5 text-sm font-medium text-white/50 hover:text-white/70 transition-colors"
      >
        <ArrowLeft className="h-3.5 w-3.5" />
        Back to marketplace
      </Link>
    </motion.div>
  )
}
