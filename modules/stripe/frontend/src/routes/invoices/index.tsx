import { createFileRoute } from '@tanstack/react-router'
import { InvoicesList } from '../../invoices-list'

export const Route = createFileRoute('/invoices/')({
  component: RouteComponent,
})

function RouteComponent() {
  return (
    <main className="container mx-auto p-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold mb-2">Invoices</h1>
        <p className="text-muted-foreground">
          View your invoices and payment history
        </p>
      </div>
      <InvoicesList />
    </main>
  )
}
