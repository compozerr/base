import { createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'
import Navbar from '@/components/navbar'
import Footer from '@/components/footer'
import FAQSection from '@/components/faq-section'
import { api } from '@/api-client'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Slider } from '@/components/ui/slider'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Check, Cpu, HardDrive, MemoryStick, Workflow, Plus, Zap, ArrowRight } from 'lucide-react'
import { Price } from '@/lib/price'
import { getLink } from '@/links'

export const Route = createFileRoute('/pricing')({
  component: Pricing,
  loader: () => api.v1.getServersTiers.prefetchQuery(),
})

function Pricing() {
  const { data: tiersData } = api.v1.getServersTiers.useQuery()
  const tiers = tiersData?.tiers ?? []
  const [selectedIndex, setSelectedIndex] = useState(1) // Default to T1

  const selectedTier = tiers[selectedIndex] || tiers[0]
  const isT1 = selectedTier?.id?.value === 'T1'
  const blackFridayPrice = 5

  // Helper to get price value
  const getPriceValue = (tier: typeof selectedTier) => {
    return tier?.price?.value ?? 0
  }

  return (
    <>
      <MouseMoveEffect />
      <div className="relative min-h-screen flex flex-col">
        {/* Background gradients */}
        <div className="pointer-events-none fixed inset-0">
          <div className="absolute inset-0 bg-gradient-to-b from-background via-background/90 to-background" />
          <div className="absolute right-0 top-0 h-[500px] w-[500px] bg-blue-500/10 blur-[100px]" />
          <div className="absolute bottom-0 left-0 h-[500px] w-[500px] bg-purple-500/10 blur-[100px]" />
        </div>

        <div className="relative z-10">
          <Navbar />
          <main className="flex-1">
            <div className="container max-w-5xl py-12 md:py-16">
              {/* Header */}
              <div className="text-center space-y-4 mb-12">
                <Badge className="mb-2 bg-red-600 text-white hover:bg-red-700">
                  🎉 Black Friday Special - 44% OFF on n8n
                </Badge>
                <h1 className="bg-gradient-to-br from-foreground from-30% via-foreground/90 to-foreground/70 bg-clip-text text-4xl md:text-5xl font-bold tracking-tight text-transparent">
                  Simple, Transparent Pricing
                </h1>
                <p className="text-lg md:text-xl text-muted-foreground max-w-2xl mx-auto">
                  Choose the server tier that fits your needs. Scale up or down anytime.
                </p>
              </div>

              {/* Interactive Tier Selector */}
              <Card className="border-zinc-800 bg-zinc-900/50 backdrop-blur-sm mb-12 relative overflow-visible">
                {isT1 && (
                  <div className="absolute -top-3 left-1/2 -translate-x-1/2 z-10">
                    <Badge className="bg-red-600 text-white hover:bg-red-700">
                      🔥 Black Friday Deal
                    </Badge>
                  </div>
                )}
                <CardContent className="p-8 md:p-12">
                  <div className="space-y-8">
                    {/* Slider */}
                    <div className="space-y-6">
                      <h2 className="text-xl font-semibold text-center">Select Your Server Tier</h2>
                      <div className="px-4">
                        <Slider
                          value={[selectedIndex]}
                          onValueChange={(value) => setSelectedIndex(value[0])}
                          max={tiers.length - 1}
                          step={1}
                          className="w-full"
                        />
                      </div>
                      <div className="flex justify-between px-2 text-sm text-muted-foreground">
                        {tiers.map((tier, idx) => (
                          <button
                            key={tier.id?.value}
                            onClick={() => setSelectedIndex(idx)}
                            className={`transition-colors hover:text-primary ${
                              idx === selectedIndex ? 'text-primary font-semibold' : ''
                            }`}
                          >
                            {tier.id?.value}
                          </button>
                        ))}
                      </div>
                    </div>

                    {/* Selected Tier Display */}
                    <div className="grid md:grid-cols-2 gap-8 items-center">
                      {/* Specs */}
                      <div className="space-y-6">
                        <div>
                          <h3 className="text-3xl font-bold mb-1">{selectedTier?.id?.value}</h3>
                          {isT1 ? (
                            <div className="flex items-baseline gap-2">
                              <span className="text-4xl font-bold text-red-500">${blackFridayPrice}</span>
                              <span className="text-2xl text-muted-foreground">/mo</span>
                              <span className="text-sm text-muted-foreground line-through ml-2">
                                ${getPriceValue(selectedTier)}/mo
                              </span>
                            </div>
                          ) : (
                            <div className="flex items-baseline gap-2">
                              <span className="text-4xl font-bold">
                                ${getPriceValue(selectedTier)}
                              </span>
                              <span className="text-2xl text-muted-foreground">/mo</span>
                            </div>
                          )}
                        </div>

                        <div className="grid grid-cols-3 gap-4">
                          <div className="text-center p-4 rounded-lg bg-blue-500/10 border border-blue-500/20">
                            <Cpu className="h-6 w-6 text-blue-400 mx-auto mb-2" />
                            <div className="text-2xl font-bold">{selectedTier?.cores}</div>
                            <div className="text-xs text-muted-foreground">vCPU</div>
                          </div>
                          <div className="text-center p-4 rounded-lg bg-purple-500/10 border border-purple-500/20">
                            <MemoryStick className="h-6 w-6 text-purple-400 mx-auto mb-2" />
                            <div className="text-2xl font-bold">{selectedTier?.ramGb}GB</div>
                            <div className="text-xs text-muted-foreground">RAM</div>
                          </div>
                          <div className="text-center p-4 rounded-lg bg-green-500/10 border border-green-500/20">
                            <HardDrive className="h-6 w-6 text-green-400 mx-auto mb-2" />
                            <div className="text-2xl font-bold">{selectedTier?.diskGb}GB</div>
                            <div className="text-xs text-muted-foreground">Storage</div>
                          </div>
                        </div>
                      </div>

                      {/* Features & CTA */}
                      <div className="space-y-6">
                        <div className="space-y-3">
                          {[
                            'Full n8n hosting',
                            'Custom domain support',
                            'SSL certificates',
                            'Automatic backups',
                            '24/7 infrastructure monitoring',
                          ].map((feature, idx) => (
                            <div key={idx} className="flex items-start gap-2">
                              <Check className="h-5 w-5 text-green-400 flex-shrink-0 mt-0.5" />
                              <span className="text-sm text-muted-foreground">{feature}</span>
                            </div>
                          ))}
                        </div>

                        {isT1 ? (
                          <Button
                            size="lg"
                            className="w-full bg-red-600 hover:bg-red-700 text-white"
                            onClick={() => window.location.href = '/n8n'}
                          >
                            <Workflow className="mr-2 h-5 w-5" />
                            Create n8n Project
                          </Button>
                        ) : (
                          <Button
                            size="lg"
                            className="w-full"
                            onClick={() => open(getLink('addNewService'), '_blank')}
                          >
                            <Plus className="mr-2 h-5 w-5" />
                            Add New Service
                          </Button>
                        )}
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>

              {/* Comparison Table */}
              <Card className="border-zinc-800 bg-zinc-900/50 backdrop-blur-sm mb-12">
                <CardHeader>
                  <CardTitle className="text-center">Quick Comparison</CardTitle>
                  <CardDescription className="text-center">
                    Compare all server tiers at a glance
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="overflow-x-auto">
                    <Table>
                      <TableHeader>
                        <TableRow className="border-zinc-800 hover:bg-transparent">
                          <TableHead className="w-[120px]"></TableHead>
                          {tiers.map((tier) => (
                            <TableHead key={tier.id?.value} className="text-center">
                              <div className="font-semibold">{tier.id?.value}</div>
                            </TableHead>
                          ))}
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        <TableRow className="border-zinc-800 hover:bg-zinc-800/50">
                          <TableCell className="font-medium">Price</TableCell>
                          {tiers.map((tier) => {
                            const isT1 = tier.id?.value === 'T1'
                            return (
                              <TableCell key={tier.id?.value} className="text-center">
                                {isT1 ? (
                                  <div className="space-y-1">
                                    <div className="text-lg font-bold text-red-500">${blackFridayPrice}/mo</div>
                                    <div className="text-xs text-muted-foreground line-through">
                                      ${getPriceValue(tier)}/mo
                                    </div>
                                  </div>
                                ) : (
                                  <div className="text-lg font-bold">
                                    ${getPriceValue(tier)}/mo
                                  </div>
                                )}
                              </TableCell>
                            )
                          })}
                        </TableRow>
                        <TableRow className="border-zinc-800 hover:bg-zinc-800/50">
                          <TableCell className="font-medium">CPU</TableCell>
                          {tiers.map((tier) => (
                            <TableCell key={tier.id?.value} className="text-center">
                              <span className="text-blue-400 font-semibold">{tier.cores} vCPU</span>
                            </TableCell>
                          ))}
                        </TableRow>
                        <TableRow className="border-zinc-800 hover:bg-zinc-800/50">
                          <TableCell className="font-medium">RAM</TableCell>
                          {tiers.map((tier) => (
                            <TableCell key={tier.id?.value} className="text-center">
                              <span className="text-purple-400 font-semibold">{tier.ramGb}GB</span>
                            </TableCell>
                          ))}
                        </TableRow>
                        <TableRow className="border-zinc-800 hover:bg-zinc-800/50">
                          <TableCell className="font-medium">Storage</TableCell>
                          {tiers.map((tier) => (
                            <TableCell key={tier.id?.value} className="text-center">
                              <span className="text-green-400 font-semibold">{tier.diskGb}GB</span>
                            </TableCell>
                          ))}
                        </TableRow>
                        <TableRow className="border-zinc-800 hover:bg-zinc-800/50">
                          <TableCell className="font-medium">n8n Hosting</TableCell>
                          {tiers.map((tier) => (
                            <TableCell key={tier.id?.value} className="text-center">
                              <Check className="h-5 w-5 text-green-400 mx-auto" />
                            </TableCell>
                          ))}
                        </TableRow>
                        <TableRow className="border-zinc-800 hover:bg-zinc-800/50">
                          <TableCell className="font-medium">Custom Domain</TableCell>
                          {tiers.map((tier) => (
                            <TableCell key={tier.id?.value} className="text-center">
                              <Check className="h-5 w-5 text-green-400 mx-auto" />
                            </TableCell>
                          ))}
                        </TableRow>
                        <TableRow className="border-zinc-800 hover:bg-zinc-800/50">
                          <TableCell className="font-medium">SSL & Backups</TableCell>
                          {tiers.map((tier) => (
                            <TableCell key={tier.id?.value} className="text-center">
                              <Check className="h-5 w-5 text-green-400 mx-auto" />
                            </TableCell>
                          ))}
                        </TableRow>
                      </TableBody>
                    </Table>
                  </div>
                </CardContent>
              </Card>

              {/* Additional Info */}
              <div className="max-w-3xl mx-auto space-y-8">
                <FAQSection />

                {/* CTA Section */}
                <Card className="border-zinc-800 bg-gradient-to-br from-zinc-900/90 to-zinc-800/50 backdrop-blur-sm">
                  <CardContent className="p-8 text-center space-y-6">
                    <h3 className="text-2xl font-bold">Ready to Get Started?</h3>
                    <p className="text-muted-foreground">
                      Deploy your n8n automation instance today with our Black Friday special pricing.
                    </p>
                    <div className="flex flex-col sm:flex-row gap-4 justify-center">
                      <Button
                        size="lg"
                        className="bg-white text-black hover:bg-zinc-200 font-semibold"
                        onClick={() => window.location.href = '/n8n'}
                      >
                        <Workflow className="mr-2 h-5 w-5" />
                        Create n8n Project
                      </Button>
                      <Button
                        size="lg"
                        variant="outline"
                        onClick={() => open(getLink('addNewService'), '_blank')}
                      >
                        <Plus className="mr-2 h-5 w-5" />
                        Add Custom Service
                      </Button>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      No credit card required to start • Setup in minutes • Cancel anytime
                    </p>
                  </CardContent>
                </Card>
              </div>
            </div>
          </main>
          <Footer />
        </div>
      </div>
    </>
  )
}

// Mouse move effect component (similar to n8n page)
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
