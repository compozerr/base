import { createFileRoute } from '@tanstack/react-router'
import {
  Card,
  CardContent,
} from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table'
import { FileText, ExternalLink } from 'lucide-react'
import { api } from '@/api-client'

export const Route = createFileRoute('/invoices/')({
  component: RouteComponent,
})

const statusBadge = (status: string | null) => {
  switch (status) {
    case 'paid':
      return <Badge className="bg-emerald-500/15 text-emerald-400 border-0 text-[10px]">Paid</Badge>
    case 'open':
      return <Badge className="bg-amber-500/15 text-amber-400 border-0 text-[10px]">Open</Badge>
    case 'void':
      return <Badge variant="outline" className="text-[10px]">Void</Badge>
    case 'uncollectible':
      return <Badge className="bg-red-500/15 text-red-400 border-0 text-[10px]">Uncollectible</Badge>
    default:
      return <Badge variant="outline" className="text-[10px]">{status}</Badge>
  }
}

const formatDate = (unixTimestamp: number) =>
  new Date(unixTimestamp * 1000).toLocaleDateString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
  })

const formatPeriod = (start: number, end: number) => {
  const s = new Date(start * 1000)
  const e = new Date(end * 1000)
  return `${s.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} - ${e.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`
}

const formatCurrency = (amount: number, currency: string) =>
  new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: currency.toUpperCase(),
  }).format(amount / 100)

function RouteComponent() {
  const { data: invoicesData, isLoading, error } = api.v1.getStripeInvoices.useQuery()
  const invoices = invoicesData?.invoices ?? []

  return (
    <main className="container mx-auto p-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold mb-2">Invoices</h1>
        <p className="text-muted-foreground">
          View your invoices and payment history
        </p>
      </div>

      {isLoading ? (
        <div className="space-y-4">
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
      ) : invoices.length > 0 ? (
        <Card>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Date</TableHead>
                  <TableHead>Period</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Amount</TableHead>
                  <TableHead></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {invoices.map((invoice) => (
                  <TableRow key={invoice.id}>
                    <TableCell className="text-sm">{formatDate(invoice.created)}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {formatPeriod(invoice.periodStart, invoice.periodEnd)}
                    </TableCell>
                    <TableCell>{statusBadge(invoice.status)}</TableCell>
                    <TableCell className="text-right font-medium">
                      {formatCurrency(invoice.total.amount || 0, invoice.total.currency || 'usd')}
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        {invoice.hostedInvoiceUrl && (
                          <Button variant="outline" size="sm" className="h-7 text-xs" asChild>
                            <a href={invoice.hostedInvoiceUrl} target="_blank" rel="noopener noreferrer">
                              <ExternalLink className="h-3 w-3 mr-1" />
                              View
                            </a>
                          </Button>
                        )}
                        {invoice.invoicePdf && (
                          <Button variant="ghost" size="sm" className="h-7 text-xs" asChild>
                            <a href={invoice.invoicePdf} target="_blank" rel="noopener noreferrer">
                              <FileText className="h-3 w-3 mr-1" />
                              PDF
                            </a>
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
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
