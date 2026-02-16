import { api } from '@/api-client'
import FAQSection from '@/components/faq-section'
import Footer from '@/components/footer'
import LoadingButton from '@/components/loading-button'
import Navbar from '@/components/navbar'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAuth } from '@/hooks/use-dynamic-auth'
import { useN8nIntentEphemeralStorage } from '@/hooks/use-n8n-intent-ephemeral-storage.hook'
import { useSaleOffer } from '@/hooks/use-sale-offer'
import { createFileRoute, redirect, useNavigate } from '@tanstack/react-router'
import { Check, DollarSign, Rocket, Shield, Zap, ArrowRight } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'


export const Route = createFileRoute('/n8n')({
  component: N8nLandingPage,
  async beforeLoad(ctx) {
    const { collectN8nIntent, setN8nIntentValue } = useN8nIntentEphemeralStorage();

    const shouldAutoCreate = collectN8nIntent()?.action === "create"
    if (!shouldAutoCreate) return;

    const { data: projectsData } = await api.v1.getProjects()
    const { data: locationsDataForAutoCreate } = await api.v1.getCliLocations()
    const { data: tiersData } = await api.v1.getServersTiers()

    const existingProjects = projectsData?.projects ?? []
    const locations = locationsDataForAutoCreate ?? []
    const tiers = tiersData?.tiers ?? []

    const defaultName = 'My n8n service'
    const hasN8nProject = existingProjects.some(p => p?.type === "N8n")

    if (hasN8nProject) {
      console.warn('User already has an n8n project, skipping auto-creation.')
      throw redirect({ to: "/n8n" })
    };

    const selectedLocation = locations[0] ?? ''
    const t1Tier = tiers.find(t => t.id?.value === 'T1')
    const selectedTier = t1Tier?.id?.value ?? tiers[0]?.id?.value ?? ''

    if (!defaultName || !selectedLocation || !selectedTier) {
      console.error('Missing required parameters for project creation')
      throw redirect({ to: "/n8n" })
    }

    try {
      const result = await api.v1.postN8nProjects({
        body: {
          projectName: defaultName,
          locationIso: selectedLocation,
          tier: selectedTier,
        }
      })

      if (result.data?.projectId) {
        await api.v1.getProjects.invalidateQueries({})
        setN8nIntentValue({
          action: 'created',
          projectId: result.data.projectId,
          timestamp: Date.now()
        })

        throw redirect({
          to: "/projects/$projectId",
          params: { projectId: result.data.projectId }
        })
      }
    } catch (err) {
      if (err && typeof err === 'object' && ('status' in err && (err as any).status === 307)) {
        throw err
      }
      if (err && typeof err === 'object' && 'options' in err && (err as any).options?.to) {
        throw err
      }
      console.error('Failed to auto-create n8n project:', err)
      throw redirect({ to: "/n8n" })
    }
  }
})

function N8nLandingPage() {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? <AuthenticatedN8nFlow /> : <UnauthenticatedN8nFlow />
}

function UnauthenticatedN8nFlow() {
  const navigate = useNavigate()
  const { setN8nIntentValue } = useN8nIntentEphemeralStorage();
  const sale = useSaleOffer();

  const handleGetStarted = () => {
    setN8nIntentValue({ action: 'create' })
    navigate({ to: '/login', search: { redirect: location.href } })
  }

  return (
    <div className="relative min-h-screen bg-background">
      <Navbar />

      {/* Hero */}
      <section className="relative overflow-hidden">
        <div className="absolute inset-0 bg-grid-pattern mask-fade-out opacity-30" />
        <div className="pointer-events-none absolute left-1/2 top-0 -translate-x-1/2 h-[500px] w-[800px] bg-primary/[0.07] blur-[120px] rounded-full" />

        <div className="container relative max-w-6xl flex flex-col items-center justify-center min-h-[80vh] py-24 text-center">
          <Badge className={`animate-fade-up mb-6 ${sale.badgeColor} text-white text-xs px-3 py-1`}>
            {sale.badge} — {sale.discount}
          </Badge>

          <h1 className="animate-fade-up max-w-3xl text-4xl font-bold tracking-tight sm:text-5xl md:text-6xl lg:text-7xl" style={{ animationDelay: '100ms' }}>
            Deploy n8n{' '}
            <span className="bg-gradient-to-r from-primary via-blue-400 to-cyan-400 bg-clip-text text-transparent">
              in seconds
            </span>
          </h1>

          <p className="animate-fade-up mt-6 max-w-xl text-base text-muted-foreground sm:text-lg leading-relaxed" style={{ animationDelay: '200ms' }}>
            Managed n8n automation hosting with complete control. Independent infrastructure, custom domains, automatic SSL. No Big Tech dependencies.
          </p>

          {/* Pricing highlight */}
          <div className="animate-fade-up mt-8 flex items-center gap-4" style={{ animationDelay: '300ms' }}>
            <div className="text-center">
              <div className={`text-4xl md:text-5xl font-bold tracking-tight ${sale.priceColor}`}>
                ${sale.salePrice}<span className="text-lg text-muted-foreground">/mo</span>
              </div>
              <div className="text-sm text-muted-foreground/50 line-through">${sale.originalPrice}/mo</div>
            </div>
          </div>

          <div className="animate-fade-up mt-8 flex flex-col sm:flex-row items-center gap-3" style={{ animationDelay: '400ms' }}>
            <Button
              onClick={handleGetStarted}
              size="lg"
              className="bg-primary hover:bg-primary/90 text-primary-foreground font-medium px-8 h-12 text-base"
            >
              <Rocket className="mr-2 h-4 w-4" />
              Get started free
            </Button>
            <Button
              variant="ghost"
              size="lg"
              className="text-muted-foreground hover:text-foreground font-medium px-6 h-12"
              onClick={() => window.location.href = '/pricing'}
            >
              View all tiers
              <ArrowRight className="ml-2 h-4 w-4" />
            </Button>
          </div>

          <p className="animate-fade-up mt-4 text-xs text-muted-foreground/50" style={{ animationDelay: '500ms' }}>
            No credit card required &middot; Setup in seconds &middot; Cancel anytime
          </p>
        </div>
      </section>

      {/* Demo Video */}
      <section className="py-16">
        <div className="container max-w-4xl">
          <div className="rounded-2xl border border-white/[0.06] bg-[hsl(var(--surface))] overflow-hidden">
            <div className="relative aspect-video bg-black/50">
              <video
                className="w-full h-full object-cover"
                autoPlay
                loop
                muted
                playsInline
                preload="metadata"
                style={{ scale: '1.04' }}
              >
                <source src="/n8n-demo.mp4" type="video/mp4" />
                <div className="absolute inset-0 flex items-center justify-center">
                  <div className="text-center">
                    <div className="w-16 h-16 rounded-full bg-white/5 flex items-center justify-center mx-auto border border-white/10">
                      <Rocket className="h-8 w-8 text-primary" />
                    </div>
                    <p className="mt-4 text-sm text-muted-foreground">See n8n in action</p>
                  </div>
                </div>
              </video>
            </div>
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="py-16">
        <div className="container max-w-6xl">
          <div className="grid md:grid-cols-3 gap-4">
            {[
              {
                icon: Shield,
                color: 'blue',
                title: 'Independent Hosting',
                description: 'Your data, your rules. Complete independence and control over your automation workflows with full privacy.',
              },
              {
                icon: Zap,
                color: 'violet',
                title: 'One-Click Deploy',
                description: 'Deploy n8n in seconds, not hours. Automated setup handles configuration, domains, and SSL.',
              },
              {
                icon: DollarSign,
                color: 'emerald',
                title: 'Transparent Pricing',
                description: `Simple, predictable pricing. No hidden fees. Just $${sale.salePrice}/month for your n8n instance during our ${sale.name}.`,
              },
            ].map((feature) => {
              const colorMap: Record<string, string> = {
                blue: 'bg-blue-500/[0.06] border-blue-500/10 text-blue-400',
                violet: 'bg-violet-500/[0.06] border-violet-500/10 text-violet-400',
                emerald: 'bg-emerald-500/[0.06] border-emerald-500/10 text-emerald-400',
              }
              return (
                <div
                  key={feature.title}
                  className="rounded-xl border border-white/[0.06] bg-[hsl(var(--surface))] p-6 transition-colors hover:border-white/[0.1] hover:bg-[hsl(var(--surface-elevated))]"
                >
                  <div className={`mb-4 flex h-10 w-10 items-center justify-center rounded-lg border ${colorMap[feature.color]}`}>
                    <feature.icon className="h-5 w-5" />
                  </div>
                  <h3 className="text-[15px] font-semibold mb-2">{feature.title}</h3>
                  <p className="text-sm leading-relaxed text-muted-foreground">{feature.description}</p>
                </div>
              )
            })}
          </div>
        </div>
      </section>

      {/* Why Choose + FAQ */}
      <section className="py-16">
        <div className="container max-w-6xl">
          <div className="grid md:grid-cols-2 gap-8">
            {/* Why Choose */}
            <div>
              <h2 className="text-xl font-semibold mb-6">Why compozerr?</h2>
              <div className="rounded-2xl border border-white/[0.06] bg-[hsl(var(--surface))] p-6 space-y-4">
                {[
                  'Complete control over your data and workflows',
                  'No vendor lock-in or platform dependencies',
                  'Custom domains and full SSL support',
                  'Easy scaling without infrastructure headaches',
                  'Privacy-focused with no data mining',
                  'Simple setup — deploy in minutes, not days',
                ].map((feature, idx) => (
                  <div key={idx} className="flex items-center gap-3">
                    <div className="flex h-5 w-5 items-center justify-center rounded-full bg-emerald-500/10 flex-shrink-0">
                      <Check className="h-3 w-3 text-emerald-400" />
                    </div>
                    <span className="text-sm text-muted-foreground">{feature}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* FAQ */}
            <FAQSection />
          </div>
        </div>
      </section>

      {/* Bottom CTA */}
      <section className="py-16">
        <div className="container max-w-6xl text-center">
          <Button
            onClick={handleGetStarted}
            size="lg"
            className="bg-primary hover:bg-primary/90 text-primary-foreground font-medium px-10 h-12 text-base"
          >
            <Rocket className="mr-2 h-4 w-4" />
            Deploy your n8n instance
          </Button>
        </div>
      </section>

      <Footer />
    </div>
  )
}

function AuthenticatedN8nFlow() {
  const navigate = useNavigate();
  const { user } = useAuth();
  const { setN8nIntentValue } = useN8nIntentEphemeralStorage();
  const sale = useSaleOffer();

  const { data: locationsData } = api.v1.getCliLocations.useQuery()
  const { data: tiersData } = api.v1.getServersTiers.useQuery()
  const { data: projectsData } = api.v1.getProjects.useInfiniteQuery(
    { query: {} },
    {
      getNextPageParam: (lastPage) => {
        if (!lastPage) return undefined;
        const currentPage = lastPage.page ?? 1;
        const total = lastPage.totalProjectsCount ?? 0;
        const pageSize = lastPage.pageSize ?? 20;
        const totalPages = Math.ceil(total / pageSize);
        if (currentPage < totalPages) {
          return { query: { page: currentPage + 1 } };
        }
        return undefined;
      },
      initialPageParam: { query: { page: 1 } }
    }
  );

  const locations = useMemo(() => locationsData ?? [], [locationsData])
  const tiers = useMemo(() => tiersData?.tiers ?? [], [tiersData])

  const existingProjects = useMemo(() => {
    return projectsData?.pages.flatMap(page => page.projects ?? []) ?? []
  }, [projectsData])

  const defaultName = 'My n8n service'
  const hasN8nProject = existingProjects.some(p => p?.type === "N8n")

  const [projectName, setProjectName] = useState(defaultName)
  const [selectedLocation, setSelectedLocation] = useState<string>('')
  const [selectedTier, setSelectedTier] = useState<string>('')

  useEffect(() => {
    if (locations.length > 0 && !selectedLocation) {
      setSelectedLocation(locations[0] ?? '')
    }
  }, [locations, selectedLocation])

  useEffect(() => {
    if (tiers.length > 0 && !selectedTier) {
      const t1Tier = tiers.find(t => t.id?.value === 'T1')
      setSelectedTier(t1Tier?.id?.value ?? tiers[0]?.id?.value ?? '')
    }
  }, [tiers, selectedTier])

  const { mutateAsync: createN8nProject, isPending: isCreatingProject } = api.v1.postN8nProjects.useMutation()
  const isPending = isCreatingProject

  const handleCreate = async () => {
    if (!projectName || !selectedLocation || !selectedTier) return
    try {
      const result = await createN8nProject({
        body: {
          projectName: projectName,
          locationIso: selectedLocation,
          tier: selectedTier,
        }
      })

      if (result.projectId) {
        await api.v1.getProjects.invalidateQueries({})
        setN8nIntentValue({
          action: 'created',
          projectId: result.projectId,
          timestamp: Date.now()
        })
        navigate({ to: "/projects/$projectId", params: { projectId: result.projectId } });
        return;
      }
    } catch (err) {
      console.error('Failed to create n8n project:', err)
    }
  }

  return (
    <div className="relative min-h-screen bg-background">
      <Navbar />

      <section className="relative overflow-hidden">
        <div className="absolute inset-0 bg-grid-pattern mask-fade-out opacity-30" />
        <div className="pointer-events-none absolute left-1/2 top-0 -translate-x-1/2 h-[500px] w-[800px] bg-primary/[0.07] blur-[120px] rounded-full" />

        <div className="container relative max-w-6xl flex flex-col items-center justify-center min-h-[80vh] py-24 text-center">
          <Badge className={`animate-fade-up mb-6 ${sale.badgeColor} text-white text-xs px-3 py-1`}>
            {sale.badge} — {sale.discount}
          </Badge>

          <h1 className="animate-fade-up max-w-3xl text-4xl font-bold tracking-tight sm:text-5xl md:text-6xl" style={{ animationDelay: '100ms' }}>
            Hello, {user?.name}
          </h1>

          <p className="animate-fade-up mt-4 max-w-xl text-base text-muted-foreground sm:text-lg leading-relaxed" style={{ animationDelay: '200ms' }}>
            Deploy a new n8n automation instance. Self-hosted, independent, ready in seconds.
          </p>

          {/* Pricing */}
          <div className="animate-fade-up mt-8 flex items-center gap-4" style={{ animationDelay: '300ms' }}>
            <div className="text-center">
              <div className={`text-5xl md:text-6xl font-bold tracking-tight ${sale.priceColor}`}>
                ${sale.salePrice}<span className="text-xl text-muted-foreground">/mo</span>
              </div>
              <div className="text-sm text-muted-foreground/50 line-through mt-1">${sale.originalPrice}/mo</div>
            </div>
          </div>

          {/* Name input for additional projects */}
          {hasN8nProject && (
            <div className="animate-fade-up mt-8 w-full max-w-sm" style={{ animationDelay: '350ms' }}>
              <div className="rounded-xl border border-white/[0.06] bg-[hsl(var(--surface))] p-5">
                <Label htmlFor="projectName" className="text-sm font-medium mb-2 block text-left">Project name</Label>
                <Input
                  id="projectName"
                  placeholder="My n8n service"
                  value={projectName}
                  onChange={(e) => setProjectName(e.target.value)}
                  className="h-11 bg-background border-white/[0.06]"
                />
              </div>
            </div>
          )}

          {/* Create button */}
          <div className="animate-fade-up mt-8" style={{ animationDelay: '400ms' }}>
            <LoadingButton
              isLoading={!!isPending}
              disabled={!projectName || !selectedLocation || !selectedTier}
              onClick={handleCreate}
              size="lg"
              className="bg-primary hover:bg-primary/90 text-primary-foreground font-medium px-10 h-13 text-lg"
            >
              <Rocket className="mr-2 h-5 w-5" />
              Create my n8n instance
            </LoadingButton>
          </div>

          <p className="animate-fade-up mt-4 text-xs text-muted-foreground/50" style={{ animationDelay: '500ms' }}>
            Deploy instantly &middot; T1 Server &middot; Fully managed &middot; Cancel anytime
          </p>
        </div>
      </section>

      <Footer />
    </div>
  )
}

export default N8nLandingPage
