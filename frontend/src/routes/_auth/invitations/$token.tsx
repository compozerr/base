import * as React from 'react'
import { createFileRoute, useNavigate, Link } from '@tanstack/react-router'
import { Check, X, AlertCircle, Mail } from 'lucide-react'
import { api } from '@/api-client'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { useToast } from '@/hooks/use-toast'

export const Route = createFileRoute('/_auth/invitations/$token')({
  component: InvitationAcceptPage,
})

function InvitationAcceptPage() {
  const { token } = Route.useParams()
  const navigate = useNavigate()
  const { toast } = useToast()

  const { data, isLoading, isError, error } = api.v1.getInvitationsToken.useQuery({
    path: { token },
  })

  const acceptMutation = api.v1.postInvitationsTokenAccept.useMutation(
    { path: { token } },
    {
      onSuccess: () => {
        toast({ title: 'Invitation accepted', variant: 'success' })
        if (data?.organizationId) {
          void navigate({ to: '/organizations/$orgId', params: { orgId: data.organizationId } })
        } else {
          void navigate({ to: '/' })
        }
      },
      onError: (err: unknown) => {
        const message = err instanceof Error ? err.message : 'Please try again.'
        toast({
          title: 'Could not accept invitation',
          description: message,
          variant: 'destructive',
        })
      },
    },
  )

  const declineMutation = api.v1.postInvitationsTokenDecline.useMutation(
    { path: { token } },
    {
      onSuccess: () => {
        toast({ title: 'Invitation declined' })
        void navigate({ to: '/' })
      },
      onError: (err: unknown) => {
        const message = err instanceof Error ? err.message : 'Please try again.'
        toast({
          title: 'Could not decline invitation',
          description: message,
          variant: 'destructive',
        })
      },
    },
  )

  const errorMessage = error instanceof Error ? error.message : 'Invitation not found.'

  return (
    <div className="min-h-screen flex items-center justify-center p-6">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center space-y-2">
          <div className="mx-auto h-10 w-10 rounded-full bg-muted flex items-center justify-center">
            <Mail className="h-5 w-5" />
          </div>
          <CardTitle>Organization invitation</CardTitle>
          {isLoading ? (
            <CardDescription>Loading...</CardDescription>
          ) : isError || !data ? (
            <CardDescription className="text-destructive flex items-center justify-center gap-2">
              <AlertCircle className="h-4 w-4" />
              {errorMessage}
            </CardDescription>
          ) : (
            <CardDescription>
              {data.inviterName ?? 'A team member'} invited you to join{' '}
              <span className="font-medium text-foreground">{data.organizationName}</span> as{' '}
              <span className="font-medium text-foreground">{data.role}</span>.
            </CardDescription>
          )}
        </CardHeader>
        <CardContent className="space-y-3">
          {isLoading ? (
            <Skeleton className="h-10 w-full" />
          ) : !data ? (
            <Button asChild variant="outline" className="w-full">
              <Link to="/">Go home</Link>
            </Button>
          ) : data.status !== 'Pending' || data.isExpired ? (
            <>
              <p className="text-sm text-muted-foreground text-center">
                This invitation is no longer pending (status:{' '}
                <span className="font-mono">{data.status}</span>).
              </p>
              <Button asChild variant="outline" className="w-full">
                <Link to="/">Go home</Link>
              </Button>
            </>
          ) : (
            <>
              <Button
                className="w-full"
                disabled={acceptMutation.isPending || declineMutation.isPending}
                onClick={() => acceptMutation.mutate(undefined)}
              >
                <Check className="h-4 w-4 mr-1" />
                {acceptMutation.isPending ? 'Accepting...' : 'Accept invitation'}
              </Button>
              <Button
                variant="outline"
                className="w-full"
                disabled={acceptMutation.isPending || declineMutation.isPending}
                onClick={() => declineMutation.mutate(undefined)}
              >
                <X className="h-4 w-4 mr-1" />
                {declineMutation.isPending ? 'Declining...' : 'Decline'}
              </Button>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
