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

export const Route = createFileRoute('/n8n')({
  component: N8nLandingPage,
})

function N8nLandingPage() {
  const navigate = useNavigate()
  const { user, login, isAuthenticated } = useAuth()
  const { data: locationsData } = api.v1.getLocations.useQuery({}, { enabled: isAuthenticated })
  const { data: tiersData } = api.v1.getServerTiers.useQuery({}, { enabled: isAuthenticated })

  const locations = useMemo(() => locationsData ?? [], [locationsData])
  const tiers = useMemo(() => tiersData ?? [], [tiersData])

  const [projectName, setProjectName] = useState('My n8n Workflow')
  const [selectedLocation, setSelectedLocation] = useState<string>('')
  const [selectedTier, setSelectedTier] = useState<string>('')

  // Set defaults when data loads
  useEffect(() => {
    if (locations.length > 0 && !selectedLocation) {
      setSelectedLocation(locations[0].isoCountryCode ?? '')
    }
  }, [locations, selectedLocation])

  useEffect(() => {
    if (tiers.length > 0 && !selectedTier) {
      setSelectedTier(tiers[0].id ?? '')
    }
  }, [tiers, selectedTier])

  const { mutateAsync: createProject, isPending } = api.v1.postCliProjects.useMutation()

  const handleGetStarted = () => {
    // Store intent in sessionStorage for after login redirect
    sessionStorage.setItem('n8nIntent', 'create')
    // Navigate to login with redirect back to /n8n
    window.location.href = `/login?redirect=${encodeURIComponent('/n8n')}`
  }

  const handleCreate = async () => {
    if (!projectName || !selectedLocation || !selectedTier) return
    try {
      const result = await createProject({
        repoName: projectName,
        repoUrl: 'https://github.com/compozerr/n8n-template',
        locationIso: selectedLocation,
        tier: selectedTier,
      })

      if (result.projectId) {
        // Invalidate queries to refresh data
        await api.v1.getProjects.invalidateQueries({})

        // Navigate to the new project
        navigate({ to: '/_auth/_dashboard/projects/$projectId', params: { projectId: result.projectId } })
      }
    } catch (err) {
      // Errors will be surfaced by the request layer/UI
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
                  Self-host your n8n automation workflows with complete control. 
                  No Google, Amazon, Facebook, or Microsoft lock-in. Just pure, independent hosting.
                </p>

                {/* Pricing Highlight */}
                <div className="flex items-center justify-center gap-4 pt-4">
                  <div className="text-center">
                    <div className="text-4xl md:text-5xl font-bold text-red-500">$5<span className="text-2xl text-muted-foreground">/mo</span></div>
                    <div className="text-sm text-muted-foreground line-through">$9/mo</div>
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
              <div className="max-w-3xl mx-auto mb-16">
                <h2 className="text-3xl font-bold text-center mb-8">Why Choose Independent Hosting?</h2>
                <div className="space-y-4">
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
                    Join thousands of developers who've chosen independence over convenience. 
                    Deploy your n8n instance today and experience the freedom of self-hosted automation.
                  </p>
                  <Button 
                    onClick={handleGetStarted}
                    size="lg" 
                    className="bg-white text-black hover:bg-zinc-200 font-semibold text-lg px-8 py-6 h-auto"
                  >
                    Start Free - No Credit Card Required
                  </Button>
                  <p className="text-xs text-muted-foreground">
                    Black Friday pricing: $5/month (normally $9/month) • Offer expires soon
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
    <div className="container mx-auto max-w-4xl py-10">
      <div className="mb-8 text-center">
        <Badge className="mb-4 bg-red-600 text-white">Black Friday Special - $5/mo</Badge>
        <h1 className="text-4xl font-bold mb-2">Create n8n Project</h1>
        <p className="text-muted-foreground">
          Launch your own n8n automation workflow instance in seconds.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Project Configuration</CardTitle>
          <CardDescription>
            Configure your n8n project settings. Your instance will be deployed automatically.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="space-y-2">
            <Label htmlFor="projectName">Project Name</Label>
            <Input
              id="projectName"
              placeholder="My n8n Workflow"
              value={projectName}
              onChange={(e) => setProjectName(e.target.value)}
            />
            <p className="text-xs text-muted-foreground">
              Choose a descriptive name for your n8n project
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="location">Location</Label>
            <Select
              value={selectedLocation}
              onValueChange={setSelectedLocation}
              disabled={locations.length === 0}
            >
              <SelectTrigger id="location">
                <SelectValue placeholder={locations.length === 0 ? 'Loading locations...' : 'Choose a location'} />
              </SelectTrigger>
              <SelectContent>
                {locations.map((loc, idx) => (
                  <SelectItem key={idx} value={loc.isoCountryCode ?? ''}>
                    {loc.isoCountryCode}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className="text-xs text-muted-foreground">
              Select the server location closest to you
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="tier">Server Tier</Label>
            <Select
              value={selectedTier}
              onValueChange={setSelectedTier}
              disabled={tiers.length === 0}
            >
              <SelectTrigger id="tier">
                <SelectValue placeholder={tiers.length === 0 ? 'Loading tiers...' : 'Choose a tier'} />
              </SelectTrigger>
              <SelectContent>
                {tiers.map((tier, idx) => (
                  <SelectItem key={idx} value={tier.id ?? ''}>
                    {tier.id} - ${tier.price}/mo
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className="text-xs text-muted-foreground">
              Select your preferred server tier and pricing
            </p>
          </div>

          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Globe className="h-4 w-4" />
            <span>After creation, manage domains and settings in Project Settings</span>
          </div>

          <div>
            <LoadingButton
              isLoading={!!isPending}
              disabled={!projectName || !selectedLocation || !selectedTier}
              onClick={handleCreate}
              className="w-full"
            >
              <Rocket className="mr-2 h-4 w-4" />
              Create n8n Project
            </LoadingButton>
          </div>
        </CardContent>
      </Card>
    </div>
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
