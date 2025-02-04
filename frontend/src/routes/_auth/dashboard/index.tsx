import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { MetricsCard } from '@/components/metrics-card'
import { ServicesTable } from '@/components/services-table'
import { StatsChart } from '@/components/stats-chart'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { ChevronDown } from 'lucide-react'

export const Route = createFileRoute('/_auth/dashboard/')({
  component: RouteComponent,
})

function RouteComponent() {
  return (<main>
    <div className="mb-6 flex items-center justify-between">
      <div className="space-y-1">
        <h1 className="text-3xl font-bold">Microservices Overview</h1>
        <div className="text-sm text-muted-foreground">
          Aug 13, 2023 - Aug 18, 2023
        </div>
      </div>
      <Button variant="outline" className="gap-2">
        All Services
        <ChevronDown className="h-4 w-4" />
      </Button>
    </div>
    <div className="grid gap-4 md:grid-cols-3">
      <MetricsCard
        title="Remaining Tokens (vCPU Hours)"
        value="1,892"
        change={{ value: '-108', percentage: '-5.4%', isPositive: false }}
      />
      <MetricsCard
        title="Active Services"
        value="7"
        change={{ value: '+2', percentage: '+40%', isPositive: true }}
      />
      <MetricsCard
        title="Total vCPU Hours Used"
        value="246.4"
        change={{ value: '+18.7', percentage: '+8.2%', isPositive: true }}
      />
    </div>
    <Card className="mt-6 p-6">
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-lg font-semibold">Overall Traffic</h2>
        <div className="flex gap-2">
          <Button size="sm" variant="ghost">
            Today
          </Button>
          <Button size="sm" variant="ghost">
            Last week
          </Button>
          <Button size="sm" variant="ghost">
            Last month
          </Button>
          <Button size="sm" variant="ghost">
            Last 6 month
          </Button>
          <Button size="sm" variant="ghost">
            Year
          </Button>
        </div>
      </div>
      <StatsChart />
    </Card>
    <div className="mt-6">
      <ServicesTable />
    </div>
  </main>)
}
