import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { useMemo, useState, useEffect } from 'react'
import { api } from '@/api-client'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import LoadingButton from '@/components/loading-button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAuth } from '@/hooks/use-dynamic-auth'
import { Badge } from '@/components/ui/badge'
import { Check, Zap, Shield, DollarSign, Rocket, Globe } from 'lucide-react'
import Navbar from '@/components/navbar'
import { Price } from '@/lib/price'

export const Route = createFileRoute('/n8n')({
  component: N8nLandingPage,
})

function N8nLandingPage() {
  const navigate = useNavigate()
  const { isAuthenticated, user, login } = useAuth();

  const { data: locationsData } = api.v1.getCliLocations.useQuery(undefined, { enabled: isAuthenticated });
  const { data: tiersData } = api.v1.getServersTiers.useQuery(undefined, { enabled: isAuthenticated })
  const { data: projectsData } = api.v1.getProjects.useInfiniteQuery(
    { query: {} },
    {
      enabled: isAuthenticated,
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

  // Check if "My n8n service" already exists
  const existingProjects = useMemo(() => {
    return projectsData?.pages.flatMap(page => page.projects ?? []) ?? []
  }, [projectsData])

  const defaultName = 'My n8n service'
  const nameExists = existingProjects.some(p => p?.name === defaultName)

  const [projectName, setProjectName] = useState(defaultName)
  const [selectedLocation, setSelectedLocation] = useState<string>('')
  const [selectedTier, setSelectedTier] = useState<string>('')

  // Set defaults when data loads
  useEffect(() => {
    if (locations.length > 0 && !selectedLocation) {
      setSelectedLocation(locations[0] ?? '')
    }
  }, [locations, selectedLocation])

  useEffect(() => {
    if (tiers.length > 0 && !selectedTier) {
      // Default to T1 tier
      const t1Tier = tiers.find(t => t.id?.value === 'T1')
      setSelectedTier(t1Tier?.id?.value ?? tiers[0]?.id?.value ?? '')
    }
  }, [tiers, selectedTier])

  const { mutateAsync: createN8nProject, isPending: isCreatingProject } = api.v1.postN8nProjects.useMutation()
  const { mutateAsync: createSubscription, isPending: isCreatingSubscription } = api.v1.postStripeSubscriptionsUpsert.useMutation()

  const isPending = isCreatingProject || isCreatingSubscription

  // Black Friday coupon code - auto-applied for T1 tier
  const BLACK_FRIDAY_COUPON = 'BLACK_FRIDAY_2025'

  const handleGetStarted = () => {
    sessionStorage.setItem('n8nIntent', 'create')
    // Navigate to login with redirect back to /n8n
    login();
  }

  const handleCreate = async () => {
    if (!projectName || !selectedLocation || !selectedTier) return
    try {
      // Step 1: Create the n8n project
      const result = await createN8nProject({
        body: {
          projectName: projectName,
          locationIso: selectedLocation,
          tier: selectedTier,
        }
      })

      if (result.projectId) {
        // Step 2: Create subscription with Black Friday coupon (auto-applied for T1)
        const shouldApplyCoupon = selectedTier === 'T1'

        await createSubscription({
          body: {
            projectId: result.projectId,
            tier: selectedTier,
            couponCode: shouldApplyCoupon ? BLACK_FRIDAY_COUPON : undefined,
          } as any
        })

        await api.v1.getProjects.invalidateQueries({})

        // Store n8n creation intent for projects page
        sessionStorage.setItem('n8nIntent', JSON.stringify({
          action: 'created',
          projectId: result.projectId,
          timestamp: Date.now()
        }))

        // Navigate to projects list page
        navigate({ to: '/projects' })
      }
    } catch (err) {
      console.error('Failed to create n8n project:', err)
    }
  }

  // Check for login intent after authentication
  useEffect(() => {
    if (isAuthenticated && sessionStorage.getItem('n8nIntent') === 'create') {
      sessionStorage.removeItem('n8nIntent')
      // User is now authenticated, show the creation UI
    }
  }, [isAuthenticated])

  // Show marketing landing page if not authenticated
  if (!isAuthenticated) {
    return (
      <>
        <MouseMoveEffect />
        <div className="relative min-h-screen">
          {/* Background gradients */}
          <div className="pointer-events-none fixed inset-0">
            <div className="absolute inset-0 bg-gradient-to-b from-background via-background/90 to-background" />
            <div className="absolute right-0 top-0 h-[500px] w-[500px] bg-blue-500/10 blur-[100px]" />
            <div className="absolute bottom-0 left-0 h-[500px] w-[500px] bg-purple-500/10 blur-[100px]" />
          </div>

          <div className="relative z-10">
            <Navbar />
            <div className="container mx-auto px-4 py-16 md:py-24">
              {/* Hero Section */}
              <div className="text-center space-y-8 max-w-4xl mx-auto mb-16">
                <Badge className="mb-4 bg-red-600 text-white hover:bg-red-700">
                  🎉 Black Friday Special - 44% OFF
                </Badge>
                <h1 className="bg-gradient-to-br from-foreground from-30% via-foreground/90 to-foreground/70 bg-clip-text text-5xl font-bold tracking-tight text-transparent sm:text-6xl md:text-7xl lg:text-8xl">
                  Deploy n8n in Seconds
                  <br />
                  <span className="text-red-500">Not on GAFM</span>
                </h1>
                <p className="text-xl md:text-2xl text-muted-foreground max-w-2xl mx-auto">
                  Create n8n automation workflows with complete control.
                  <br />
                  No Google, Amazon, Facebook, or Microsoft. Just pure, independent hosting - powered by <a href='https://www.ovhcloud.com/en/' className="underline" target="_blank" rel="noopener noreferrer">OVHcloud®</a>.
                </p>

                {/* Pricing Highlight */}
                <div className="flex items-center justify-center gap-4 pt-4">
                  <div className="text-center">
                    <div className="text-4xl md:text-5xl font-bold text-red-500">$5<span className="text-2xl text-muted-foreground">/mo</span></div>
                    <div className="text-sm text-muted-foreground line-through">$28/mo</div>
                  </div>
                  <Badge variant="outline" className="text-lg px-4 py-2">
                    Limited Time Offer
                  </Badge>
                </div>

                <Button
                  onClick={handleGetStarted}
                  size="lg"
                  className="bg-white text-black hover:bg-zinc-200 font-semibold text-lg px-8 py-6 h-auto"
                >
                  <Rocket className="mr-2 h-5 w-5" />
                  Get Started Free
                </Button>
                <p className="text-sm text-muted-foreground">
                  No credit card required • Setup in minutes • Cancel anytime
                </p>
              </div>

              {/* Features Grid */}
              <div className="grid md:grid-cols-3 gap-8 mb-16 max-w-5xl mx-auto">
                <Card className="border-zinc-800 bg-zinc-900/50 backdrop-blur-sm">
                  <CardHeader>
                    <div className="w-12 h-12 rounded-lg bg-blue-500/10 flex items-center justify-center mb-4">
                      <Shield className="h-6 w-6 text-blue-400" />
                    </div>
                    <CardTitle>GAFM-Free Hosting</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-muted-foreground">
                      Your data, your rules. No dependency on Big Tech platforms.
                      Complete independence and control over your automation workflows.
                    </p>
                  </CardContent>
                </Card>

                <Card className="border-zinc-800 bg-zinc-900/50 backdrop-blur-sm">
                  <CardHeader>
                    <div className="w-12 h-12 rounded-lg bg-purple-500/10 flex items-center justify-center mb-4">
                      <Zap className="h-6 w-6 text-purple-400" />
                    </div>
                    <CardTitle>One-Click Deploy</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-muted-foreground">
                      Deploy n8n in seconds, not hours. Our automated setup handles
                      everything from configuration to domain assignment.
                    </p>
                  </CardContent>
                </Card>

                <Card className="border-zinc-800 bg-zinc-900/50 backdrop-blur-sm">
                  <CardHeader>
                    <div className="w-12 h-12 rounded-lg bg-green-500/10 flex items-center justify-center mb-4">
                      <DollarSign className="h-6 w-6 text-green-400" />
                    </div>
                    <CardTitle>Transparent Pricing</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <p className="text-muted-foreground">
                      Simple, predictable pricing. No hidden fees, no surprise charges.
                      Just $5/month for your n8n instance during this Black Friday special.
                    </p>
                  </CardContent>
                </Card>
              </div>

              {/* Why Choose Us */}
              <div className="max-w-2xl mx-auto mb-16">
                <h2 className="text-3xl font-bold text-center mb-8">Why Choose Independent Hosting?</h2>
                <div className="space-y-4 w-fit mx-auto">
                  {[
                    'Complete control over your data and workflows',
                    'No vendor lock-in or platform dependencies',
                    'Custom domains and full SSL support',
                    'Easy scaling without infrastructure headaches',
                    'Privacy-focused with no data mining',
                    'Simple setup - deploy in minutes, not days',
                  ].map((feature, idx) => (
                    <div key={idx} className="flex items-start gap-3">
                      <Check className="h-5 w-5 text-green-400 flex-shrink-0 mt-0.5" />
                      <p className="text-muted-foreground">{feature}</p>
                    </div>
                  ))}
                </div>
              </div>

              {/* CTA Section */}
              <Card className="border-zinc-800 bg-gradient-to-br from-zinc-900/90 to-zinc-800/50 backdrop-blur-sm max-w-2xl mx-auto">
                <CardContent className="p-8 text-center space-y-6">
                  <h3 className="text-2xl font-bold">Ready to Take Control?</h3>
                  <p className="text-muted-foreground">
                    Deploy your n8n instance today and experience the freedom of automation.
                  </p>
                  <Button
                    onClick={handleGetStarted}
                    size="lg"
                    className="bg-white text-black hover:bg-zinc-200 font-semibold text-lg px-8 py-6 h-auto"
                  >
                    Start Free - No Credit Card Required
                  </Button>
                  <p className="text-xs text-muted-foreground">
                    Black Friday pricing: $5/month (on n8n cloud it is $28/month) • Offer expires soon
                  </p>
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      </>
    )
  }

  // Authenticated user - show project creation UI
  return (
    <>
      <MouseMoveEffect />
      <div className="relative min-h-screen">
        {/* Background gradients */}
        <div className="pointer-events-none fixed inset-0">
          <div className="absolute inset-0 bg-gradient-to-b from-background via-background/90 to-background" />
          <div className="absolute right-0 top-0 h-[500px] w-[500px] bg-blue-500/10 blur-[100px]" />
          <div className="absolute bottom-0 left-0 h-[500px] w-[500px] bg-purple-500/10 blur-[100px]" />
        </div>

        <div className="relative z-10">
          <Navbar />
          <div className="container mx-auto px-4 py-16 md:py-24">
            {/* Hero Section with Welcome */}
            <div className="text-center space-y-8 max-w-4xl mx-auto">
              <Badge className="mb-4 bg-red-600 text-white hover:bg-red-700">
                🎉 Black Friday Special - 44% OFF
              </Badge>
              <h1 className="bg-gradient-to-br from-foreground from-30% via-foreground/90 to-foreground/70 bg-clip-text text-5xl font-bold tracking-tight text-transparent sm:text-6xl md:text-7xl">
                Hello {user?.name}!
              </h1>
              <p className="text-xl md:text-2xl text-muted-foreground max-w-2xl mx-auto">
                You're one click away from deploying your own n8n automation instance.
                Self-hosted, independent, and ready in seconds.
              </p>

              {/* Pricing Highlight */}
              <div className="flex items-center justify-center gap-4 pt-4">
                <div className="text-center">
                  <div className="text-5xl md:text-6xl font-bold text-red-500">
                    $5<span className="text-3xl text-muted-foreground">/mo</span>
                  </div>
                  <div className="text-lg text-muted-foreground line-through">$28/mo</div>
                </div>
                <Badge variant="outline" className="text-lg px-4 py-2">
                  Limited Time
                </Badge>
              </div>

              {/* Conditional Name Input */}
              {nameExists && (
                <Card className="max-w-md mx-auto border-zinc-800 bg-zinc-900/50 backdrop-blur-sm">
                  <CardContent className="pt-6 space-y-2">
                    <Label htmlFor="projectName" className="text-base">Project Name</Label>
                    <Input
                      id="projectName"
                      placeholder="My n8n service"
                      value={projectName}
                      onChange={(e) => setProjectName(e.target.value)}
                      className="h-12 text-base"
                    />
                    <p className="text-sm text-muted-foreground">
                      A project with this name already exists. Please choose a different name.
                    </p>
                  </CardContent>
                </Card>
              )}

              {/* Big Create Button */}
              <div className="pt-8">
                <LoadingButton
                  isLoading={!!isPending}
                  disabled={!projectName || !selectedLocation || !selectedTier}
                  onClick={handleCreate}
                  size="lg"
                  className="bg-white text-black hover:bg-zinc-200 font-semibold text-xl px-12 py-8 h-auto shadow-2xl hover:shadow-white/20 transition-all"
                >
                  <Rocket className="mr-3 h-6 w-6" />
                  Create My n8n Instance
                </LoadingButton>
              </div>

              <p className="text-sm text-muted-foreground pt-4">
                Deploy instantly • T1 Server • Fully managed • Cancel anytime
              </p>

              {/* Feature Highlights */}
              <div className="grid md:grid-cols-3 gap-6 pt-12 max-w-3xl mx-auto">
                <div className="text-center space-y-2">
                  <div className="w-12 h-12 rounded-full bg-blue-500/10 flex items-center justify-center mx-auto">
                    <Zap className="h-6 w-6 text-blue-400" />
                  </div>
                  <h3 className="font-semibold">Instant Setup</h3>
                  <p className="text-sm text-muted-foreground">
                    Your instance will be live in under 60 seconds
                  </p>
                </div>
                <div className="text-center space-y-2">
                  <div className="w-12 h-12 rounded-full bg-green-500/10 flex items-center justify-center mx-auto">
                    <Shield className="h-6 w-6 text-green-400" />
                  </div>
                  <h3 className="font-semibold">GAFM-Free</h3>
                  <p className="text-sm text-muted-foreground">
                    No Big Tech dependencies, complete control
                  </p>
                </div>
                <div className="text-center space-y-2">
                  <div className="w-12 h-12 rounded-full bg-purple-500/10 flex items-center justify-center mx-auto">
                    <Globe className="h-6 w-6 text-purple-400" />
                  </div>
                  <h3 className="font-semibold">Custom Domains</h3>
                  <p className="text-sm text-muted-foreground">
                    Add your own domain after deployment
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  )
}

// Mouse move effect component (similar to frontpage)
function MouseMoveEffect() {
  return (
    <div className="absolute inset-0 pointer-events-none">
      <style>{`
        @keyframes wave {
          0%, 100% {
            transform: scale(0.8);
            opacity: 0.1;
          }
          50% {
            transform: scale(1);
            opacity: 0.3;
          }
        }
        .wave-dot {
          animation: wave 3s ease-in-out infinite;
        }
      `}</style>
      <div className="grid gap-6 p-4" style={{
        gridTemplateColumns: 'repeat(auto-fit, minmax(6px, 1fr))',
        minHeight: '100vh',
        width: '100vw'
      }}>
        {Array.from({ length: 800 }, (_, i) => {
          const row = Math.floor(i / 40);
          const col = i % 40;
          const delay = (row * 0.08 + col * 0.03) % 3;
          return (
            <div
              key={i}
              className="w-1.5 h-1.5 bg-blue-500/20 rounded-full wave-dot opacity-0"
              style={{
                animationDelay: `${delay}s`,
              }}
            />
          );
        })}
      </div>
    </div>
  )
}

export default N8nLandingPage
