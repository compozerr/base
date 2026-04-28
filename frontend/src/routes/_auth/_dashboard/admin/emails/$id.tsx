import { createFileRoute, Link } from '@tanstack/react-router'
import { ArrowLeft, AlertCircle } from 'lucide-react'
import { api } from '@/api-client'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

export const Route = createFileRoute('/_auth/_dashboard/admin/emails/$id')({
  component: AdminEmailDetailPage,
})

function AdminEmailDetailPage() {
  const { id } = Route.useParams()

  const { data, isLoading, isError } = api.v1.getAdminSentEmailsId.useQuery({
    path: { id },
  })

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2">
        <Button asChild size="sm" variant="ghost">
          <Link to="/admin/emails">
            <ArrowLeft className="h-4 w-4 mr-1" />
            Back to sent emails
          </Link>
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-64 w-full" />
        </div>
      ) : isError || !data ? (
        <div className="flex items-center gap-2 text-destructive">
          <AlertCircle className="h-4 w-4" />
          <span>Email not found.</span>
        </div>
      ) : (
        <>
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-3">
                <span className="truncate">{data.subject}</span>
                <StatusBadge status={data.status} />
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <Field label="To" value={formatAddress(data.toAddress ?? '', data.toDisplayName)} />
              <Field label="From" value={formatAddress(data.fromAddress ?? '', data.fromDisplayName)} />
              <Field label="Template" value={data.templateName ?? '—'} />
              <Field label="Sent at" value={new Date(data.sentAtUtc ?? '').toLocaleString()} />
              {data.errorMessage ? (
                <Field label="Error" value={data.errorMessage} />
              ) : null}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Rendered body</CardTitle>
            </CardHeader>
            <CardContent>
              {/* sandbox attribute prevents scripts, forms and top-navigation from the stored HTML */}
              <iframe
                title="Email body"
                className="w-full min-h-[600px] rounded-md border bg-white"
                sandbox=""
                srcDoc={data.htmlBody ?? undefined}
              />
            </CardContent>
          </Card>
        </>
      )}
    </div>
  )
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div className="grid grid-cols-[120px_1fr] gap-4">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-mono text-xs break-all">{value}</span>
    </div>
  )
}

function formatAddress(address: string, displayName: string | null | undefined) {
  return displayName ? `${displayName} <${address}>` : address
}

function StatusBadge({ status }: { status: 'Sent' | 'Failed' }) {
  if (status === 'Sent') {
    return <Badge variant="default">Sent</Badge>
  }
  return <Badge variant="destructive">Failed</Badge>
}
