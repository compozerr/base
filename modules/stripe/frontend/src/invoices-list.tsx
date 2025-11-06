import React from 'react';
import { api } from '@/api-client';
import {
  Card,
  CardContent,
} from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table';

interface InvoicesListProps {
}

export const InvoicesList: React.FC<InvoicesListProps> = () => {
  // Use the API endpoint - will work once OpenAPI schema is regenerated
  const { data: invoicesData, isLoading, error } = api.v1.getStripeInvoices.useQuery();

  const invoices = invoicesData?.invoices || [];

  const formatCurrency = (amount: number, currency: string) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency.toUpperCase(),
    }).format(amount / 100);
  };

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-64" />
        <Card>
          <CardContent className="p-0">
            <div className="space-y-2 p-4">
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-destructive">
        Error loading invoices: {(error as Error).message}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {invoices.length > 0 ? (
        <Card>
          <CardContent className="p-0">
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
                  {invoices.map((invoice) => (
                    <TableRow key={invoice.id}>
                      <TableCell className="font-mono text-sm">
                        {invoice.id}
                      </TableCell>
                      <TableCell className="font-semibold">
                        {formatCurrency(
                          invoice!.total!.amount!,
                          invoice!.total!.currency!
                        )}
                      </TableCell>
                      <TableCell>
                        <div className="space-y-1">
                          {invoice!.lines?.map((line) => (
                            <div
                              key={line.id}
                              className="text-sm flex justify-between gap-4"
                            >
                              <span className="text-muted-foreground">
                                {line.description || 'No description'}
                              </span>
                              <span className="font-medium whitespace-nowrap">
                                {formatCurrency(
                                  line!.amount!.amount!,
                                  line!.amount!.currency!
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
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardContent className="py-6">
            <div className="text-center space-y-3">
              <p className="text-muted-foreground">
                No invoices found. Once you have invoices in Stripe, they will appear here.
              </p>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

export default InvoicesList;

