import { createFileRoute } from '@tanstack/react-router'
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
} from '@/components/ui/card'
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { FileText, Download, ChevronDown, ChevronUp } from 'lucide-react'
import { api, apiBaseUrl } from '@/api-client'
import { useState } from 'react'

export const Route = createFileRoute('/invoices/')({
  component: RouteComponent,
})

// Type definitions matching the backend DTOs
interface Money {
  amount: number
  currency: string
}

interface InvoiceLineDto {
  id: string
  amount: Money
  description: string
}

interface InvoiceDto {
  id: string
  total: Money
  lines: InvoiceLineDto[]
}

interface MonthlyInvoiceGroup {
  yearMonth: string          // "2024-11"
  monthLabel: string         // "November 2024"
  isOngoing: boolean         // true for current month
  monthTotal: Money          // Total for the month
  invoices: InvoiceDto[]     // All invoices in this month
}

interface GetInvoicesResponse {
  monthlyGroups: MonthlyInvoiceGroup[]
}

function MonthlyInvoiceCard({ group }: { group: MonthlyInvoiceGroup }) {
  const [isOpen, setIsOpen] = useState(false)

  const formatCurrency = (amount: number, currency: string) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency.toUpperCase(),
    }).format(amount / 100)
  }

  const handleDownloadMonthly = async () => {
    try {
      const response = await fetch(
        `${apiBaseUrl}/v1/stripe/invoices/monthly/${group.yearMonth}/download`,
        {
          method: 'POST',
          credentials: 'include',
        }
      )

      if (!response.ok) {
        throw new Error('Failed to download invoice')
      }

      const blob = await response.blob()
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `invoice-${group.yearMonth}.pdf`
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      window.URL.revokeObjectURL(url)
    } catch (error) {
      console.error('Error downloading invoice:', error)
      alert('Failed to download invoice. Please try again.')
    }
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <CardTitle className="text-xl">{group.monthLabel}</CardTitle>
            {group.isOngoing && (
              <Badge variant="secondary">Ongoing</Badge>
            )}
          </div>
          <div className="flex items-center gap-4">
            <span className="text-lg font-semibold">
              {formatCurrency(group.monthTotal.amount || 0, group.monthTotal.currency || 'usd')}
            </span>
            {!group.isOngoing && (
              <Button onClick={handleDownloadMonthly} size="sm">
                <Download className="h-4 w-4 mr-2" />
                Download Invoice
              </Button>
            )}
          </div>
        </div>
        {group.isOngoing && (
          <CardDescription>
            This month is ongoing. Invoice will be available for download next month.
          </CardDescription>
        )}
      </CardHeader>
      <CardContent>
        <Collapsible open={isOpen} onOpenChange={setIsOpen}>
          <CollapsibleTrigger asChild>
            <Button variant="ghost" size="sm" className="w-full justify-between">
              <span className="text-sm text-muted-foreground">
                {isOpen ? 'Hide' : 'Show'} invoice details ({group.invoices.length} invoice{group.invoices.length !== 1 ? 's' : ''})
              </span>
              {isOpen ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
            </Button>
          </CollapsibleTrigger>
          <CollapsibleContent className="mt-4">
            <div className="space-y-4">
              {group.invoices.map((invoice) => (
                <div key={invoice.id} className="border rounded-lg p-4">
                  <div className="flex justify-between items-start mb-3">
                    <span className="font-mono text-xs text-muted-foreground">
                      {invoice.id}
                    </span>
                    <span className="font-semibold">
                      {formatCurrency(invoice.total?.amount || 0, invoice.total?.currency || 'usd')}
                    </span>
                  </div>
                  <div className="space-y-2">
                    {invoice.lines?.map((line) => (
                      <div key={line.id} className="flex justify-between text-sm">
                        <span className="text-muted-foreground">
                          {line.description || 'Service'}
                        </span>
                        <span className="font-medium">
                          {formatCurrency(line?.amount?.amount || 0, line?.amount?.currency || 'usd')}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </CollapsibleContent>
        </Collapsible>
      </CardContent>
    </Card>
  )
}

function RouteComponent() {
  const { data: invoicesData, isLoading, error } = api.v1.getStripeInvoices.useQuery();

  return (
    <main className="container mx-auto p-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold mb-2">Monthly Invoices</h1>
        <p className="text-muted-foreground">
          View and download your monthly consolidated invoices
        </p>
      </div>

      {isLoading ? (
        <div className="space-y-4">
          <Skeleton className="h-32 w-full" />
          <Skeleton className="h-32 w-full" />
          <Skeleton className="h-32 w-full" />
        </div>
      ) : error ? (
        <Card>
          <CardContent className="py-8">
            <p className="text-destructive text-center">
              Error loading invoices: {(error as Error)?.message}
            </p>
          </CardContent>
        </Card>
      ) : invoicesData?.monthlyGroups && invoicesData.monthlyGroups.length > 0 ? (
        <div className="space-y-6">
          {invoicesData.monthlyGroups.map((group) => (
            <MonthlyInvoiceCard key={group.yearMonth} group={group} />
          ))}
        </div>
      ) : (
        <Card>
          <CardContent className="py-12">
            <div className="text-center space-y-3">
              <FileText className="h-12 w-12 mx-auto text-muted-foreground opacity-50" />
              <p className="text-muted-foreground">
                No invoices found. Once you have invoices in Stripe, they will appear here.
              </p>
            </div>
          </CardContent>
        </Card>
      )}
    </main>
  )
}
