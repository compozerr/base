import * as React from 'react'
import { createFileRoute, Link } from '@tanstack/react-router'
import { Mail, AlertCircle } from 'lucide-react'
import { api } from '@/api-client'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

export const Route = createFileRoute('/_auth/_dashboard/admin/emails/')({
  component: AdminEmailsListPage,
})

function AdminEmailsListPage() {
  const [cursorStack, setCursorStack] = React.useState<(string | undefined)[]>([undefined])
  const cursor = cursorStack[cursorStack.length - 1]

  const query: { Cursor?: string; Limit?: number } = { Limit: 50 }
  if (cursor) query.Cursor = cursor

  const { data, isLoading, isError } = api.v1.getAdminSentEmails.useQuery({ query })

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <Mail className="h-6 w-6" />
        <div>
          <h1 className="text-2xl font-semibold">Sent emails</h1>
          <p className="text-sm text-muted-foreground">
            Every email the platform has dispatched, newest first.
          </p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>All outbound mail</CardTitle>
          <CardDescription>
            Click a row to view the rendered body and metadata.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 6 }).map((_, i) => (
                <Skeleton key={i} className="h-10 w-full" />
              ))}
            </div>
          ) : isError ? (
            <div className="flex items-center gap-2 text-destructive">
              <AlertCircle className="h-4 w-4" />
              <span>Failed to load sent emails.</span>
            </div>
          ) : !data?.items?.length ? (
            <p className="text-sm text-muted-foreground">No emails have been sent yet.</p>
          ) : (
            <>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Recipient</TableHead>
                    <TableHead>Subject</TableHead>
                    <TableHead>Template</TableHead>
                    <TableHead>Sent at</TableHead>
                    <TableHead>Status</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((email) => (
                    <TableRow key={email.id} className="cursor-pointer">
                      <TableCell className="font-mono text-xs">
                        <Link
                          to="/admin/emails/$id"
                          params={{ id: email.id }}
                          className="hover:underline"
                        >
                          {email.toDisplayName
                            ? `${email.toDisplayName} <${email.toAddress}>`
                            : email.toAddress}
                        </Link>
                      </TableCell>
                      <TableCell className="max-w-[320px] truncate">
                        <Link
                          to="/admin/emails/$id"
                          params={{ id: email.id }}
                          className="hover:underline"
                        >
                          {email.subject}
                        </Link>
                      </TableCell>
                      <TableCell>
                        {email.templateName ? (
                          <code className="text-xs">{email.templateName}</code>
                        ) : (
                          <span className="text-muted-foreground">—</span>
                        )}
                      </TableCell>
                      <TableCell className="whitespace-nowrap text-xs">
                        {new Date(email.sentAtUtc).toLocaleString()}
                      </TableCell>
                      <TableCell>
                        <StatusBadge status={email.status} />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>

              <div className="flex items-center justify-between pt-4">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={cursorStack.length <= 1}
                  onClick={() => setCursorStack((s) => s.slice(0, -1))}
                >
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={!data.nextCursor}
                  onClick={() => {
                    if (data.nextCursor) {
                      setCursorStack((s) => [...s, data.nextCursor ?? undefined])
                    }
                  }}
                >
                  Next
                </Button>
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

function StatusBadge({ status }: { status: 'Sent' | 'Failed' }) {
  if (status === 'Sent') {
    return <Badge variant="default">Sent</Badge>
  }
  return <Badge variant="destructive">Failed</Badge>
}
