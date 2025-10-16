import { createFileRoute } from '@tanstack/react-router'
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
} from '@/components/ui/card'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Skeleton } from '@/components/ui/skeleton'
import { FileText } from 'lucide-react'

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

interface GetInvoicesResponse {
  invoices: InvoiceDto[]
}

function RouteComponent() {
  // TODO: Once the OpenAPI schema is regenerated, use:
  // const { data: invoicesData, isLoading, error } = api.v1.postStripeInvoices.useQuery({ body: {} })

  // Mock data for now - replace with actual API call once backend generates OpenAPI spec
  const isLoading = false
  const error = null
  const invoicesData: GetInvoicesResponse | undefined = {
    invoices: []
  }

  const formatCurrency = (amount: number, currency: string) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency.toUpperCase(),
    }).format(amount / 100)
  }

  return (
    <main className="container mx-auto p-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold mb-2">Invoices</h1>
        <p className="text-muted-foreground">
          View and manage your Stripe invoices
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileText className="h-5 w-5" />
            Your Invoices
          </CardTitle>
          <CardDescription>
            A list of all your invoices from Stripe
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-3">
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
            </div>
          ) : error ? (
            <p className="text-destructive">
              Error loading invoices: {(error as Error)?.message}
            </p>
          ) : invoicesData?.invoices && invoicesData.invoices.length > 0 ? (
            <div className="rounded-md border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Invoice ID</TableHead>
                    <TableHead>Total</TableHead>
                    <TableHead>Line Items</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {invoicesData.invoices.map((invoice: InvoiceDto) => (
                    <TableRow key={invoice.id}>
                      <TableCell className="font-mono text-sm">
                        {invoice.id}
                      </TableCell>
                      <TableCell className="font-semibold">
                        {formatCurrency(
                          invoice.total.amount,
                          invoice.total.currency
                        )}
                      </TableCell>
                      <TableCell>
                        <div className="space-y-1">
                          {invoice.lines.map((line: InvoiceLineDto) => (
                            <div
                              key={line.id}
                              className="text-sm flex justify-between gap-4"
                            >
                              <span className="text-muted-foreground">
                                {line.description || 'No description'}
                              </span>
                              <span className="font-medium whitespace-nowrap">
                                {formatCurrency(
                                  line.amount.amount,
                                  line.amount.currency
                                )}
                              </span>
                            </div>
                          ))}
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          ) : (
            <p className="text-muted-foreground text-center py-8">
              No invoices found. Once you have invoices in Stripe, they will appear here.
            </p>
          )}
        </CardContent>
      </Card>
    </main>
  )
}
