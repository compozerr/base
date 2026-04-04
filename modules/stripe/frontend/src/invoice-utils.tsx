import { Badge } from '@/components/ui/badge';

export function InvoiceStatusBadge({ status }: { status: string | null }) {
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
}

export const formatDate = (unixTimestamp: number) =>
  new Date(unixTimestamp * 1000).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });

export const formatPeriod = (start: number, end: number) => {
  const startDate = new Date(start * 1000);
  const endDate = new Date(end * 1000);
  return `${startDate.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} - ${endDate.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`;
};

export const formatCurrencyCents = (amount: number, currency: string) =>
  new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: currency.toUpperCase(),
  }).format(amount / 100);

export const formatCurrency = (amount: number, currency: string) =>
  new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: currency.toUpperCase(),
  }).format(amount);
