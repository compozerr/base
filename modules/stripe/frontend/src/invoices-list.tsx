import React from 'react';
import { api } from '@/api-client';
import {
  Card,
  CardContent,
} from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table';
import { ExternalLink, FileText } from 'lucide-react';

const statusBadge = (status: string | null) => {
  switch (status) {
    case 'paid':
      return <Badge className="bg-emerald-500/15 text-emerald-400 border-0 text-[10px]">Paid</Badge>;
    case 'open':
      return <Badge className="bg-amber-500/15 text-amber-400 border-0 text-[10px]">Open</Badge>;
    case 'void':
      return <Badge variant="outline" className="text-[10px]">Void</Badge>;
    case 'uncollectible':
      return <Badge className="bg-red-500/15 text-red-400 border-0 text-[10px]">Uncollectible</Badge>;
    default:
      return <Badge variant="outline" className="text-[10px]">{status}</Badge>;
  }
};

const formatDate = (unixTimestamp: number) => {
  return new Date(unixTimestamp * 1000).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
};

const formatPeriod = (start: number, end: number) => {
  const startDate = new Date(start * 1000);
  const endDate = new Date(end * 1000);
  return `${startDate.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} - ${endDate.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`;
};

const formatCurrency = (amount: number, currency: string) => {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: currency.toUpperCase(),
  }).format(amount / 100);
};

export const InvoicesList: React.FC = () => {
  const { data: invoicesData, isLoading, error } = api.v1.getStripeInvoices.useQuery();

  if (isLoading) {
    return (
      <div className="space-y-4">
        <h3 className="text-lg font-medium">Invoices</h3>
        <Card>
          <CardContent className="p-0">
            <div className="space-y-2 p-4">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (error) {
    return (
      <div className="space-y-4">
        <h3 className="text-lg font-medium">Invoices</h3>
        <div className="text-destructive">
          Error loading invoices: {(error as Error).message}
        </div>
      </div>
    );
  }

  const invoices = invoicesData?.invoices ?? [];

  return (
    <div className="space-y-4">
      <h3 className="text-lg font-medium">Invoices</h3>

      {invoices.length > 0 ? (
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
                    <TableCell className="text-sm">
                      {formatDate(invoice.created)}
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {formatPeriod(invoice.periodStart, invoice.periodEnd)}
                    </TableCell>
                    <TableCell>
                      {statusBadge(invoice.status)}
                    </TableCell>
                    <TableCell className="text-right font-medium">
                      {formatCurrency(invoice.total.amount || 0, invoice.total.currency || 'usd')}
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        {invoice.hostedInvoiceUrl && (
                          <Button
                            variant="outline"
                            size="sm"
                            className="h-7 text-xs"
                            asChild
                          >
                            <a href={invoice.hostedInvoiceUrl} target="_blank" rel="noopener noreferrer">
                              <ExternalLink className="h-3 w-3 mr-1" />
                              View
                            </a>
                          </Button>
                        )}
                        {invoice.invoicePdf && (
                          <Button
                            variant="ghost"
                            size="sm"
                            className="h-7 text-xs"
                            asChild
                          >
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
          <CardContent className="py-6">
            <div className="text-center space-y-3">
              <FileText className="h-10 w-10 mx-auto text-muted-foreground opacity-50" />
              <p className="text-muted-foreground">No invoices yet</p>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

export default InvoicesList;
