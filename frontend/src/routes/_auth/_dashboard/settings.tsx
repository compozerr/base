import * as React from 'react'
import { createFileRoute, useSearch } from '@tanstack/react-router'
import { api } from '@/api-client'
import { Github, CircleHelp } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
} from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { useToast } from '@/hooks/use-toast'
import { Separator } from '@/components/ui/separator'
import StripeProvider from '@repo/stripe/stripe-provider'
import SubscriptionList from '@repo/stripe/subscription-list'
import PaymentMethods from '@repo/stripe/payment-methods'
import { z } from 'zod'

const optionalSearchParamsSchema = z.object({
  addPaymentMethod: z.boolean().optional(),
})

export const Route = createFileRoute('/_auth/_dashboard/settings')({
  component: RouteComponent,
  validateSearch: optionalSearchParamsSchema
})

function RouteComponent() {
  const { toast } = useToast();
  
  // Add state to track the active user
  const {
    data: appUrlData,
    isLoading: appUrlLoading,
    error: appUrlError,
  } = api.v1.getGithubGetInstallAppUrl.useQuery()
  const {
    data: installationsData,
    isLoading: installationsLoading,
    error: installationsError,
  } = api.v1.getGithubGetInstalledOrganizations.useQuery()

  const isLoading = appUrlLoading || installationsLoading
  const error =
    (appUrlError as Error)?.message ||
    (installationsError as Error)?.message ||
    null

  const installAppUrl = appUrlData?.installUrl || null

  const [selectedProjectsInstallationId, setSelectedProjectsInstallationId] =
    React.useState<string>('')
  const [selectedModulesInstallationId, setSelectedModulesInstallationId] =
    React.useState<string>('')

  const defaultOrganizationMutation =
    api.v1.postGithubSetDeafultOrganization.useMutation()

  const installations = React.useMemo(
    () => installationsData?.installations || [],
    [installationsData],
  )

  React.useEffect(() => {
    if (!installationsData) return

    setSelectedProjectsInstallationId(
      installationsData.selectedProjectsInstallationId!,
    )
    setSelectedModulesInstallationId(
      installationsData.selectedModulesInstallationId!,
    )
  }, [installationsData])

  const handleDefaultOrganizationChange = React.useCallback(
    (installationId: string, type: 1 | 2) => {
      if (type === 1) {
        setSelectedProjectsInstallationId(installationId)
      } else {
        setSelectedModulesInstallationId(installationId)
      }
      if (installationId) {
        defaultOrganizationMutation.mutateAsync({
          body: { installationId, type: type },
        }).then(() => {
          toast({
            title: "Success",
            description: "Saved successfully",
            variant: "success"
          })
        }).catch((error) => {
          toast({
            title: "Error",
            description: error.message,
            variant: "destructive"
          })
        })
      }
    },
    [defaultOrganizationMutation],
  )

  const { addPaymentMethod } = Route.useSearch();
  const navigate = Route.useNavigate();

  React.useEffect(() => {
    if(!addPaymentMethod) return;
    
    navigate({ to: Route.to })
  }, [addPaymentMethod])

  return (
    <>
      <main>
        <h1 className="text-3xl font-bold mb-6">Settings</h1>
      <div className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>GitHub Integration</CardTitle>
            <CardDescription>
              Manage your GitHub app installation and organization settings.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            {error ? (
              <p className="text-destructive">Error: {error}</p>
            ) : (
              <>
                {isLoading ? (
                  <Skeleton className="h-[38px] w-[170px] pb-2" />
                ) : (
                  installAppUrl && (
                    <Button
                      asChild
                      variant="outline"
                      className="w-full sm:w-auto"
                    >
                      <a
                        href={installAppUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                      >
                        <Github className="h-4 w-4" />
                        Install GitHub App
                      </a>
                    </Button>
                  )
                )}
                <div className="space-y-4">
                  <div className="space-y-2">
                    <div className="flex items-center gap-1">
                      <label
                        htmlFor="projects-org"
                        className="text-sm font-medium"
                      >
                        Default Project Organization
                      </label>
                      <TooltipProvider>
                        <Tooltip delayDuration={0}>
                          <TooltipTrigger asChild>
                            <CircleHelp className="h-4 w-4" />
                          </TooltipTrigger>
                          <TooltipContent className="w-64">
                            Select the default organization for managing and
                            storing your projects.
                          </TooltipContent>
                        </Tooltip>
                      </TooltipProvider>
                    </div>
                    {isLoading ? (
                      <Skeleton className="h-10 w-full sm:w-1/2" />
                    ) : (
                      <Select
                        value={selectedProjectsInstallationId}
                        onValueChange={(value) =>
                          handleDefaultOrganizationChange(value, 1)
                        }
                      >
                        <SelectTrigger id="projects-org" className="w-full sm:w-1/2">
                          <SelectValue placeholder="Select organization" />
                        </SelectTrigger>
                        <SelectContent>
                          {installations.map((i) => (
                            <SelectItem
                              key={i.installationId}
                              value={i.installationId!}
                            >
                              {i.name}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
                  </div>
                  <div className="space-y-2">
                    <div className="flex items-center gap-1">
                      <label
                        htmlFor="modules-org"
                        className="text-sm font-medium"
                      >
                        Default Modules Organization
                      </label>
                      <TooltipProvider>
                        <Tooltip delayDuration={0}>
                          <TooltipTrigger asChild>
                            <CircleHelp className="h-4 w-4" />
                          </TooltipTrigger>
                          <TooltipContent className="w-64">
                            Select the default organization for managing and
                            storing your modules.
                          </TooltipContent>
                        </Tooltip>
                      </TooltipProvider>
                    </div>
                    {isLoading ? (
                      <Skeleton className="h-10 w-full sm:w-1/2" />
                    ) : (
                      <Select
                        value={selectedModulesInstallationId}
                        onValueChange={(value) =>
                          handleDefaultOrganizationChange(value, 2)
                        }
                      >
                        <SelectTrigger className="w-full sm:w-1/2" id="modules-org">
                          <SelectValue placeholder="Select organization" />
                        </SelectTrigger>
                        <SelectContent>
                          {installations.map((i) => (
                            <SelectItem
                              key={i.installationId}
                              value={i.installationId!}
                            >
                              {i.name}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
                  </div>
                </div>
              </>
            )}
          </CardContent>
        </Card>

        {/* <Card>
          <CardHeader>
            <CardTitle>Account Settings</CardTitle>
            <CardDescription>
              Manage your account preferences and personal information.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-muted-foreground">
              Account settings coming soon...
            </p>
          </CardContent>
        </Card> */}

        <Card>
          <CardHeader>
            <CardTitle>Billing & Subscriptions</CardTitle>
            <CardDescription>
              Manage your subscriptions and billing information.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <StripeProvider>
              {/* Using the current user ID from state */}
              <PaymentMethods openAddPaymentMethodDialogOnInit={addPaymentMethod} />

              <Separator className="my-6" />

              <SubscriptionList />

            </StripeProvider>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Notification Preferences</CardTitle>
            <CardDescription>
              Control how and when you receive notifications.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-muted-foreground">
              Notification settings coming soon...
            </p>
          </CardContent>
        </Card>
      </div>
    </main>
    </>
  )
}
