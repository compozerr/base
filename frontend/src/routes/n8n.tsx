import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { useMemo, useState, useEffect, useRef } from 'react'
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
import Footer from '@/components/footer'
import FAQSection from '@/components/faq-section'
import { Price } from '@/lib/price'
import { motion, useMotionValue, useSpring } from 'framer-motion'

export const Route = createFileRoute('/n8n')({
  component: N8nLandingPage,
})

function N8nLandingPage() {
  const { isAuthenticated } = useAuth();

  return isAuthenticated ? <AuthenticatedN8nFlow /> : <UnauthenticatedN8nFlow />
}

// Unauthenticated marketing landing page
function UnauthenticatedN8nFlow() {
  const { login } = useAuth();

  const handleGetStarted = () => {
    sessionStorage.setItem('n8nIntent', 'create')
    login();
  }

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
            <div className="h-screen snap-y snap-mandatory overflow-y-auto scroll-smooth">
              <div className="container mx-auto px-4">
              {/* Section 1: Hero */}
              <section className="snap-start min-h-screen flex items-center justify-center pb-10">
                <div className="text-center space-y-8 max-w-4xl mx-auto">
                  <Badge className="bg-red-600 text-white hover:bg-red-700">
                    🎉 Black Friday Special - 37% OFF
                  </Badge>
                  <h1 className="bg-gradient-to-br from-foreground from-30% via-foreground/90 to-foreground/70 bg-clip-text text-5xl font-bold tracking-tight text-transparent sm:text-6xl md:text-7xl lg:text-8xl">
                    Deploy n8n in Seconds
                  </h1>
                  <p className="text-xl md:text-2xl text-muted-foreground max-w-2xl mx-auto">
                    Create n8n automation workflows with complete control.
                    <br />
                    No Google, Amazon, Facebook, or Microsoft. Just pure, independent hosting - powered by <a href='https://www.ovhcloud.com/en/' className="underline" target="_blank" rel="noopener noreferrer">OVHcloud®</a>.
                  </p>

                  {/* Pricing Highlight */}
                  <div className="flex items-center justify-center gap-4">
                    <div className="text-center">
                      <div className="text-4xl md:text-5xl font-bold text-red-500">$5<span className="text-2xl text-muted-foreground">/mo</span></div>
                      <div className="text-sm text-muted-foreground line-through">$8/mo</div>
                    </div>
                    <Badge variant="outline" className="text-lg px-4 py-2">
                      Limited Time Offer
                    </Badge>
                  </div>

                  <Button
                    onClick={handleGetStarted}
                    size="lg"
                    className="bg-white text-black hover:bg-zinc-200 font-semibold text-lg px-8 py-6 h-auto transition-all duration-200 hover:scale-[1.02] hover:shadow-[0_0_20px_rgba(255,255,255,0.4)] animate-pulse-glow"
                  >
                    <Rocket className="mr-2 h-5 w-5" />
                    Get Started
                  </Button>
                  <p className="text-sm text-muted-foreground pb-10">
                    No credit card required • Setup in minutes • Cancel anytime
                  </p>
                </div>
              </section>

              {/* Section 2: Demo Video + Features */}
              <section className="snap-start min-h-screen flex items-center justify-center">
                <div className="w-full mb-32">
                  {/* Demo Video */}
                  <div className="max-w-5xl mx-auto mb-16">
                    <Card className="border-zinc-800 bg-zinc-900/50 backdrop-blur-sm overflow-hidden">
                      <CardContent className="p-0">
                        <div className="relative aspect-video bg-black">
                          <video
                            className="w-full h-full object-cover rounded-lg"
                            autoPlay
                            loop
                            muted
                            playsInline
                            preload="metadata"
                            style={{
                              maxHeight: '600px',
                              objectFit: 'cover',
                              scale: '1.04',
                            }}
                          >
                            <source src="/n8n-demo.mp4" type="video/mp4" />
                            {/* Fallback content */}
                            <div className="absolute inset-0 bg-gradient-to-br from-blue-500/10 to-purple-500/10 flex items-center justify-center">
                              <div className="text-center space-y-4">
                                <div className="w-20 h-20 rounded-full bg-white/10 flex items-center justify-center mx-auto backdrop-blur-sm border border-white/20">
                                  <Rocket className="h-10 w-10 text-white" />
                                </div>
                                <div>
                                  <h3 className="text-xl font-semibold mb-2">See n8n in Action</h3>
                                  <p className="text-sm text-muted-foreground">
                                    Your browser doesn't support video playback
                                  </p>
                                </div>
                              </div>
                            </div>
                          </video>
                        </div>
                      </CardContent>
                    </Card>
                  </div>

                  {/* Features Grid */}
                  <div className="grid md:grid-cols-3 gap-8 max-w-5xl mx-auto">
                <TiltCard>
                  <Card className="border-zinc-800 bg-zinc-900/50 backdrop-blur-sm h-full">
                    <CardHeader>
                      <div className="w-12 h-12 rounded-lg bg-blue-500/10 flex items-center justify-center mb-4">
                        <Shield className="h-6 w-6 text-blue-400" />
                      </div>
                      <CardTitle>Independent Hosting</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <p className="text-muted-foreground">
                        Your data, your rules. Complete independence and control over your automation workflows with full privacy.
                      </p>
                    </CardContent>
                  </Card>
                </TiltCard>

                <TiltCard>
                  <Card className="border-zinc-800 bg-zinc-900/50 backdrop-blur-sm h-full">
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
                </TiltCard>

                <TiltCard>
                  <Card className="border-zinc-800 bg-zinc-900/50 backdrop-blur-sm h-full">
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
                </TiltCard>
                  </div>
                </div>
              </section>

              {/* Section 3: Why Choose + FAQ */}
              <section className="snap-start min-h-screen flex items-center justify-center">
                <div className="w-full mb-32">
                  {/* Why Choose + FAQ Grid */}
                  <div className="grid md:grid-cols-2 gap-8 max-w-6xl mx-auto mb-16">
                    {/* Why Choose Compozerr Card */}
                    <Card className="border-zinc-800 bg-zinc-900/50 backdrop-blur-sm">
                      <CardHeader>
                        <CardTitle className="text-2xl font-bold text-center">Why Choose Compozerr?</CardTitle>
                      </CardHeader>
                      <CardContent>
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
                              <p className="text-muted-foreground text-sm">{feature}</p>
                            </div>
                          ))}
                        </div>
                      </CardContent>
                    </Card>

                    {/* FAQ Section */}
                    <div>
                      <FAQSection />
                    </div>
                  </div>

                  {/* CTA Button */}
                  <div className="text-center">
                    <Button
                      onClick={handleGetStarted}
                      size="lg"
                      className="bg-white text-black hover:bg-zinc-200 font-semibold text-lg px-8 py-6 h-auto transition-all duration-200 hover:scale-[1.02] hover:shadow-[0_0_20px_rgba(255,255,255,0.4)] animate-pulse-glow"
                    >
                      <Rocket className="mr-2 h-5 w-5" />
                      Deploy Your n8n Instance
                    </Button>
                  </div>
                </div>
              </section>

              {/* Footer Section */}
              <section className="snap-start">
                <Footer />
              </section>
              </div>
            </div>
          </div>
        </div>
      </>
    )
}

// Authenticated project creation UI
function AuthenticatedN8nFlow() {
  const navigate = useNavigate()
  const { user } = useAuth();

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
  const nameExists = existingProjects.some(p => p?.name === defaultName)

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
        sessionStorage.setItem('n8nIntent', JSON.stringify({
          action: 'created',
          projectId: result.projectId,
          timestamp: Date.now()
        }))
        navigate({ to: '/projects' })
      }
    } catch (err) {
      console.error('Failed to create n8n project:', err)
    }
  }

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

        <div className="relative z-10 flex flex-col min-h-screen">
          <Navbar />
          <div className="flex-1">
            <div className="container mx-auto px-4 py-8">
              {/* Hero Section with Welcome */}
              <section className="min-h-[80vh] flex items-center justify-center">
                <div className="text-center space-y-8 max-w-4xl mx-auto">
                  <Badge className="mb-4 bg-red-600 text-white hover:bg-red-700">
                    🎉 Black Friday Special - 37% OFF
                  </Badge>
                  <h1 className="bg-gradient-to-br from-foreground from-30% via-foreground/90 to-foreground/70 bg-clip-text text-5xl font-bold tracking-tight text-transparent sm:text-6xl md:text-7xl">
                    Hello {user?.name}!
                  </h1>
                  <p className="text-xl md:text-2xl text-muted-foreground max-w-2xl mx-auto">
                    You're one click away from deploying a new n8n automation instance.
                    Self-hosted, independent, and ready in seconds.
                  </p>

                  {/* Pricing Highlight */}
                  <div className="flex items-center justify-center gap-4 pt-4">
                    <div className="text-center">
                      <div className="text-5xl md:text-6xl font-bold text-red-500">
                        $5<span className="text-3xl text-muted-foreground">/mo</span>
                      </div>
                      <div className="text-lg text-muted-foreground line-through">$8/mo</div>
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
                      className="bg-white text-black hover:bg-zinc-200 font-semibold text-xl px-12 py-8 h-auto transition-all duration-200 hover:scale-[1.02] hover:shadow-[0_0_20px_rgba(255,255,255,0.4)] animate-pulse-glow"
                    >
                      <Rocket className="mr-3 h-6 w-6" />
                      Create My n8n Instance
                    </LoadingButton>
                  </div>

                  <p className="text-sm text-muted-foreground pt-4">
                    Deploy instantly • T1 Server • Fully managed • Cancel anytime
                  </p>

                  {/* Feature Highlights Grid */}
                  <div className="grid md:grid-cols-3 gap-8 pt-12 max-w-5xl mx-auto">
                    <TiltCard>
                      <Card className="border-zinc-800 bg-zinc-900/50 backdrop-blur-sm h-full">
                        <CardHeader>
                          <div className="w-12 h-12 rounded-lg bg-blue-500/10 flex items-center justify-center mb-4">
                            <Zap className="h-6 w-6 text-blue-400" />
                          </div>
                          <CardTitle>Instant Setup</CardTitle>
                        </CardHeader>
                        <CardContent>
                          <p className="text-muted-foreground text-sm">
                            Your instance will be live in under 60 seconds with automated configuration.
                          </p>
                        </CardContent>
                      </Card>
                    </TiltCard>

                    <TiltCard>
                      <Card className="border-zinc-800 bg-zinc-900/50 backdrop-blur-sm h-full">
                        <CardHeader>
                          <div className="w-12 h-12 rounded-lg bg-green-500/10 flex items-center justify-center mb-4">
                            <Shield className="h-6 w-6 text-green-400" />
                          </div>
                          <CardTitle>Independent Hosting</CardTitle>
                        </CardHeader>
                        <CardContent>
                          <p className="text-muted-foreground text-sm">
                            Complete independence and control over your automation workflows.
                          </p>
                        </CardContent>
                      </Card>
                    </TiltCard>

                    <TiltCard>
                      <Card className="border-zinc-800 bg-zinc-900/50 backdrop-blur-sm h-full">
                        <CardHeader>
                          <div className="w-12 h-12 rounded-lg bg-purple-500/10 flex items-center justify-center mb-4">
                            <Globe className="h-6 w-6 text-purple-400" />
                          </div>
                          <CardTitle>Custom Domains</CardTitle>
                        </CardHeader>
                        <CardContent>
                          <p className="text-muted-foreground text-sm">
                            Add your own domain after deployment with full SSL support.
                          </p>
                        </CardContent>
                      </Card>
                    </TiltCard>
                  </div>
                </div>
              </section>
            </div>
          </div>
          <Footer />
        </div>
      </div>
    </>
  )
}

// 3D Tilt Card Component
function TiltCard({ children, className = '' }: { children: React.ReactNode; className?: string }) {
  const ref = useRef<HTMLDivElement>(null)
  const rotateX = useMotionValue(0)
  const rotateY = useMotionValue(0)

  const smoothRotateX = useSpring(rotateX, { stiffness: 300, damping: 30 })
  const smoothRotateY = useSpring(rotateY, { stiffness: 300, damping: 30 })

  const handleMouseMove = (e: React.MouseEvent<HTMLDivElement>) => {
    if (!ref.current) return
    const rect = ref.current.getBoundingClientRect()
    const x = e.clientX - rect.left
    const y = e.clientY - rect.top
    const centerX = rect.width / 2
    const centerY = rect.height / 2
    const rotateXVal = ((y - centerY) / centerY) * -10
    const rotateYVal = ((x - centerX) / centerX) * 10

    rotateX.set(rotateXVal)
    rotateY.set(rotateYVal)
  }

  const handleMouseLeave = () => {
    rotateX.set(0)
    rotateY.set(0)
  }

  return (
    <motion.div
      ref={ref}
      onMouseMove={handleMouseMove}
      onMouseLeave={handleMouseLeave}
      style={{
        rotateX: smoothRotateX,
        rotateY: smoothRotateY,
        transformStyle: 'preserve-3d',
      }}
      className={className}
      whileHover={{ scale: 1.05 }}
      transition={{ type: 'spring', stiffness: 300, damping: 20 }}
    >
      {children}
    </motion.div>
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
        @keyframes pulse-glow {
          0% {
            box-shadow: 0 0 0 0 rgba(255, 255, 255, 0.4);
          }
          70% {
            box-shadow: 0 0 0 10px rgba(255, 255, 255, 0);
          }
          100% {
            box-shadow: 0 0 0 0 rgba(255, 255, 255, 0);
          }
        }
        .animate-pulse-glow:hover {
          animation: pulse-glow 1.5s infinite;
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
