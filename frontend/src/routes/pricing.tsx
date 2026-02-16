import { api } from '@/api-client'
import FAQSection from '@/components/faq-section'
import Footer from '@/components/footer'
import Navbar from '@/components/navbar'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Slider } from '@/components/ui/slider'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { useSaleOffer } from '@/hooks/use-sale-offer'
import { getLink } from '@/links'
import { createFileRoute } from '@tanstack/react-router'
import { Check, Cpu, HardDrive, MemoryStick, Plus, Workflow, ArrowRight } from 'lucide-react'
import { useState } from 'react'

export const Route = createFileRoute('/pricing')({
  component: Pricing,
  loader: () => api.v1.getServersTiers.prefetchQuery(),
})

function Pricing() {
  const { data: tiersData } = api.v1.getServersTiers.useQuery()
  const tiers = tiersData?.tiers ?? []
  const [selectedIndex, setSelectedIndex] = useState(1)
  const sale = useSaleOffer()

  const selectedTier = tiers[selectedIndex] || tiers[0]
  const isT1 = selectedTier?.id?.value === 'T1'

  const getPriceValue = (tier: typeof selectedTier) => {
    return tier?.price?.value ?? 0
  }

  return (
    <div className="relative min-h-screen bg-background">
      <Navbar />

      <main>
        <div className="container max-w-5xl py-16 md:py-24">
          {/* Header */}
          <div className="text-center mb-16">
            <div className="animate-fade-up mb-4 inline-flex items-center gap-2 rounded-full border border-primary/20 bg-primary/[0.06] px-4 py-1.5 text-xs font-medium text-primary">
              Simple pricing
            </div>
            <h1 className="animate-fade-up text-4xl font-bold tracking-tight md:text-5xl" style={{ animationDelay: '100ms' }}>
              Transparent, predictable pricing
            </h1>
            <p className="animate-fade-up mt-4 text-lg text-muted-foreground max-w-xl mx-auto" style={{ animationDelay: '200ms' }}>
              Choose the server tier that fits your workload. Scale up or down anytime, no lock-in.
            </p>
          </div>

          {/* Interactive Tier Selector */}
          <div className="animate-fade-up relative rounded-2xl border border-white/[0.06] bg-[hsl(var(--surface))] p-8 md:p-12 mb-16" style={{ animationDelay: '300ms' }}>
            {isT1 && (
              <div className="absolute -top-3 left-1/2 -translate-x-1/2 z-10">
                <Badge className={`${sale.badgeColor} text-white text-xs px-3 py-1`}>
                  {sale.badge}
                </Badge>
              </div>
            )}

            <div className="space-y-10">
              {/* Slider */}
              <div className="space-y-6">
                <h2 className="text-lg font-semibold text-center text-muted-foreground">Select your tier</h2>
                <div className="px-4 max-w-2xl mx-auto">
                  <Slider
                    value={[selectedIndex]}
                    onValueChange={(value) => setSelectedIndex(value[0]!)}
                    max={tiers.length - 1}
                    step={1}
                    className="w-full"
                  />
                </div>
                <div className="flex justify-between px-2 max-w-2xl mx-auto">
                  {tiers.map((tier, idx) => (
                    <button
                      key={tier.id?.value}
                      onClick={() => setSelectedIndex(idx)}
                      className={`text-sm transition-all duration-200 ${
                        idx === selectedIndex
                          ? 'text-foreground font-semibold'
                          : 'text-muted-foreground/60 hover:text-muted-foreground'
                      }`}
                    >
                      {tier.id?.value}
                    </button>
                  ))}
                </div>
              </div>

              {/* Divider */}
              <div className="border-t border-white/[0.04]" />

              {/* Selected Tier Display */}
              <div className="grid md:grid-cols-2 gap-10 items-start">
                {/* Specs */}
                <div className="space-y-6">
                  <div>
                    <div className="flex items-baseline gap-3 mb-1">
                      <span className="text-sm font-medium text-muted-foreground">{selectedTier?.id?.value}</span>
                    </div>
                    {isT1 ? (
                      <div className="flex items-baseline gap-2">
                        <span className={`text-5xl font-bold tracking-tight ${sale.priceColor}`}>${sale.salePrice}</span>
                        <span className="text-xl text-muted-foreground">/mo</span>
                        <span className="text-sm text-muted-foreground/50 line-through ml-2">
                          ${getPriceValue(selectedTier)}
                        </span>
                      </div>
                    ) : (
                      <div className="flex items-baseline gap-2">
                        <span className="text-5xl font-bold tracking-tight">
                          ${getPriceValue(selectedTier)}
                        </span>
                        <span className="text-xl text-muted-foreground">/mo</span>
                      </div>
                    )}
                  </div>

                  <div className="grid grid-cols-3 gap-3">
                    <div className="rounded-xl border border-blue-500/10 bg-blue-500/[0.04] p-4 text-center">
                      <Cpu className="h-5 w-5 text-blue-400 mx-auto mb-2" />
                      <div className="text-xl font-bold">{selectedTier?.cores}</div>
                      <div className="text-[11px] text-muted-foreground mt-0.5">vCPU</div>
                    </div>
                    <div className="rounded-xl border border-violet-500/10 bg-violet-500/[0.04] p-4 text-center">
                      <MemoryStick className="h-5 w-5 text-violet-400 mx-auto mb-2" />
                      <div className="text-xl font-bold">{selectedTier?.ramGb}GB</div>
                      <div className="text-[11px] text-muted-foreground mt-0.5">RAM</div>
                    </div>
                    <div className="rounded-xl border border-emerald-500/10 bg-emerald-500/[0.04] p-4 text-center">
                      <HardDrive className="h-5 w-5 text-emerald-400 mx-auto mb-2" />
                      <div className="text-xl font-bold">{selectedTier?.diskGb}GB</div>
                      <div className="text-[11px] text-muted-foreground mt-0.5">Storage</div>
                    </div>
                  </div>
                </div>

                {/* Features & CTA */}
                <div className="space-y-6">
                  <div className="space-y-3">
                    {[
                      'Full n8n hosting',
                      'Custom domain support',
                      'SSL certificates included',
                      'Automatic backups',
                      '24/7 infrastructure monitoring',
                    ].map((feature, idx) => (
                      <div key={idx} className="flex items-center gap-3">
                        <div className="flex h-5 w-5 items-center justify-center rounded-full bg-emerald-500/10">
                          <Check className="h-3 w-3 text-emerald-400" />
                        </div>
                        <span className="text-sm text-muted-foreground">{feature}</span>
                      </div>
                    ))}
                  </div>

                  {isT1 ? (
                    <Button
                      size="lg"
                      className="w-full bg-primary hover:bg-primary/90 text-primary-foreground font-medium h-12"
                      onClick={() => window.location.href = '/n8n'}
                    >
                      <Workflow className="mr-2 h-4 w-4" />
                      Create n8n Project
                    </Button>
                  ) : (
                    <Button
                      size="lg"
                      className="w-full bg-primary hover:bg-primary/90 text-primary-foreground font-medium h-12"
                      onClick={() => open(getLink('addNewService'), '_blank')}
                    >
                      <Plus className="mr-2 h-4 w-4" />
                      Add New Service
                    </Button>
                  )}
                </div>
              </div>
            </div>
          </div>

          {/* Comparison Table */}
          <div className="rounded-2xl border border-white/[0.06] bg-[hsl(var(--surface))] overflow-hidden mb-16">
            <div className="p-8 pb-0">
              <h2 className="text-xl font-semibold text-center">Compare all tiers</h2>
              <p className="text-sm text-muted-foreground text-center mt-1">Full specs at a glance</p>
            </div>
            <div className="p-6 overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow className="border-white/[0.04] hover:bg-transparent">
                    <TableHead className="w-[120px] text-xs font-medium text-muted-foreground/60 uppercase tracking-wider" />
                    {tiers.map((tier) => (
                      <TableHead key={tier.id?.value} className="text-center">
                        <div className="text-sm font-semibold">{tier.id?.value}</div>
                      </TableHead>
                    ))}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  <TableRow className="border-white/[0.04] hover:bg-white/[0.02]">
                    <TableCell className="text-sm font-medium">Price</TableCell>
                    {tiers.map((tier) => {
                      const isTierT1 = tier.id?.value === 'T1'
                      return (
                        <TableCell key={tier.id?.value} className="text-center">
                          {isTierT1 ? (
                            <div>
                              <div className={`text-base font-bold ${sale.priceColor}`}>${sale.salePrice}/mo</div>
                              <div className="text-[11px] text-muted-foreground/40 line-through">
                                ${getPriceValue(tier)}/mo
                              </div>
                            </div>
                          ) : (
                            <div className="text-base font-bold">
                              ${getPriceValue(tier)}/mo
                            </div>
                          )}
                        </TableCell>
                      )
                    })}
                  </TableRow>
                  <TableRow className="border-white/[0.04] hover:bg-white/[0.02]">
                    <TableCell className="text-sm font-medium">CPU</TableCell>
                    {tiers.map((tier) => (
                      <TableCell key={tier.id?.value} className="text-center">
                        <span className="text-blue-400 text-sm font-medium">{tier.cores} vCPU</span>
                      </TableCell>
                    ))}
                  </TableRow>
                  <TableRow className="border-white/[0.04] hover:bg-white/[0.02]">
                    <TableCell className="text-sm font-medium">RAM</TableCell>
                    {tiers.map((tier) => (
                      <TableCell key={tier.id?.value} className="text-center">
                        <span className="text-violet-400 text-sm font-medium">{tier.ramGb}GB</span>
                      </TableCell>
                    ))}
                  </TableRow>
                  <TableRow className="border-white/[0.04] hover:bg-white/[0.02]">
                    <TableCell className="text-sm font-medium">Storage</TableCell>
                    {tiers.map((tier) => (
                      <TableCell key={tier.id?.value} className="text-center">
                        <span className="text-emerald-400 text-sm font-medium">{tier.diskGb}GB</span>
                      </TableCell>
                    ))}
                  </TableRow>
                  <TableRow className="border-white/[0.04] hover:bg-white/[0.02]">
                    <TableCell className="text-sm font-medium">n8n Hosting</TableCell>
                    {tiers.map((tier) => (
                      <TableCell key={tier.id?.value} className="text-center">
                        {tier.id?.value === "T0" ? (
                          <span className="text-muted-foreground/30">—</span>
                        ) : (
                          <Check className="h-4 w-4 text-emerald-400 mx-auto" />
                        )}
                      </TableCell>
                    ))}
                  </TableRow>
                  <TableRow className="border-white/[0.04] hover:bg-white/[0.02]">
                    <TableCell className="text-sm font-medium">Custom Domain</TableCell>
                    {tiers.map((tier) => (
                      <TableCell key={tier.id?.value} className="text-center">
                        <Check className="h-4 w-4 text-emerald-400 mx-auto" />
                      </TableCell>
                    ))}
                  </TableRow>
                  <TableRow className="border-white/[0.04] hover:bg-white/[0.02]">
                    <TableCell className="text-sm font-medium">SSL & Backups</TableCell>
                    {tiers.map((tier) => (
                      <TableCell key={tier.id?.value} className="text-center">
                        <Check className="h-4 w-4 text-emerald-400 mx-auto" />
                      </TableCell>
                    ))}
                  </TableRow>
                </TableBody>
              </Table>
            </div>
          </div>

          {/* FAQ */}
          <div className="mb-16">
            <FAQSection />
          </div>

          {/* CTA */}
          <div className="relative overflow-hidden rounded-2xl border border-white/[0.06] bg-[hsl(var(--surface))] p-8 sm:p-12 text-center">
            <div className="pointer-events-none absolute inset-0 bg-gradient-to-br from-primary/[0.05] via-transparent to-transparent" />
            <div className="relative">
              <h3 className="text-2xl font-bold tracking-tight">Ready to get started?</h3>
              <p className="mt-3 text-muted-foreground max-w-md mx-auto">
                Deploy your n8n automation instance or add a custom service today.
              </p>
              <div className="mt-8 flex flex-col sm:flex-row gap-3 justify-center">
                <Button
                  size="lg"
                  className="bg-primary hover:bg-primary/90 text-primary-foreground font-medium h-11 px-6"
                  onClick={() => window.location.href = '/n8n'}
                >
                  <Workflow className="mr-2 h-4 w-4" />
                  Create n8n Project
                </Button>
                <Button
                  size="lg"
                  variant="outline"
                  className="border-white/[0.1] hover:bg-white/[0.04] font-medium h-11 px-6"
                  onClick={() => open(getLink('addNewService'), '_blank')}
                >
                  <Plus className="mr-2 h-4 w-4" />
                  Add Custom Service
                </Button>
              </div>
              <p className="mt-6 text-xs text-muted-foreground/50">
                No credit card required &middot; Setup in seconds &middot; Cancel anytime
              </p>
            </div>
          </div>
        </div>
      </main>

      <Footer />
    </div>
  )
}
