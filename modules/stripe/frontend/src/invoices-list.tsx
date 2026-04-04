import React from 'react';
import { api } from '@/api-client';
import {
  Card,
  CardContent,
} from '@/components/ui/card';
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
import { InvoiceStatusBadge, formatDate, formatPeriod, formatCurrencyCents } from './invoice-utils';

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
                      <InvoiceStatusBadge status={invoice.status} />
                    </TableCell>
                    <TableCell className="text-right font-medium">
                      {formatCurrencyCents(invoice.total.amount || 0, invoice.total.currency || 'usd')}
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
