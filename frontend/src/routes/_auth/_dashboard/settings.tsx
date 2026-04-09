import * as React from 'react'
import { createFileRoute, useSearch } from '@tanstack/react-router'
import { api } from '@/api-client'
import { Github, CircleHelp, Copy, Check, Key, Trash2, AlertTriangle } from 'lucide-react'
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
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { useToast } from '@/hooks/use-toast'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table'
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Separator } from '@/components/ui/separator'
import StripeProvider from '@repo/stripe/stripe-provider'
import SubscriptionList from '@repo/stripe/subscription-list'
import PaymentMethods from '@repo/stripe/payment-methods'
import InvoicesList from '@repo/stripe/invoices-list'
import { useEphemeralQueryParams } from '@/hooks/use-ephemeral-storage.hook'
import { z } from 'zod'
import { useRegisterActions } from '@/lib/command-palette/action-registry'

type InstallGuideParams = Record<string, string | boolean | undefined> & {
  'install-guide-show'?: boolean;
  reason?: string;
}

const INSTALL_GUIDE_REASONS: Record<string, { title: string; description: string }> = {
  migrate: {
    title: 'GitHub Access Required',
    description: 'To migrate your project, Compozerr needs access to your repository. After installing the GitHub app for your organization, return to the CLI and run `compozerr migrate` again.',
  },
  default: {
    title: 'Install GitHub App',
    description: 'You can add more organizations by installing the GitHub app.',
  },
}

const optionalSearchParamsSchema = z.object({
  addPaymentMethod: z.boolean().optional(),
})

export const Route = createFileRoute('/_auth/_dashboard/settings')({
  component: RouteComponent,
  validateSearch: optionalSearchParamsSchema
})

function ApiTokensSection() {
    const { toast } = useToast();
    const [createDialogOpen, setCreateDialogOpen] = React.useState(false);
    const [tokenName, setTokenName] = React.useState('');
    const [tokenExpiry, setTokenExpiry] = React.useState('30');
    const [createdToken, setCreatedToken] = React.useState<string | null>(null);
    const [copied, setCopied] = React.useState(false);
    const [revokeTokenId, setRevokeTokenId] = React.useState<string | null>(null);

    const { data: tokensData, refetch: refetchTokens } = api.v1.getApiTokens.useQuery();
    const createTokenMutation = api.v1.postApiTokens.useMutation();
    const revokeTokenMutation = api.v1.deleteApiTokensId.useMutation();

    const handleCreateToken = async () => {
        if (!tokenName.trim()) return;

        let expiresAtUtc: string | undefined;
        if (tokenExpiry === '7') {
            expiresAtUtc = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString();
        } else if (tokenExpiry === '30') {
            expiresAtUtc = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString();
        }
        // 'never' = undefined expiresAtUtc

        try {
            const result = await createTokenMutation.mutateAsync({
                body: { name: tokenName.trim(), expiresAtUtc: expiresAtUtc ?? null },
            });
            setCreatedToken(result.rawToken);
            refetchTokens();
        } catch (error) {
            toast({
                title: 'Error',
                description: (error as Error).message,
                variant: 'destructive',
            });
        }
    };

    const handleRevokeToken = async () => {
        if (!revokeTokenId) return;

        try {
            await revokeTokenMutation.mutateAsync({
                path: { id: revokeTokenId },
            });
            toast({
                title: 'Token revoked',
                description: 'The API token has been revoked successfully.',
                variant: 'success',
            });
            refetchTokens();
        } catch (error) {
            toast({
                title: 'Error',
                description: (error as Error).message,
                variant: 'destructive',
            });
        } finally {
            setRevokeTokenId(null);
        }
    };

    const handleCopy = (value: string) => {
        navigator.clipboard.writeText(value);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
    };

    const handleCloseCreateDialog = () => {
        setCreateDialogOpen(false);
        setTokenName('');
        setTokenExpiry('30');
        setCreatedToken(null);
        setCopied(false);
    };

    const tokens = tokensData?.tokens ?? [];

    const formatDate = (dateStr: string | null | undefined) => {
        if (!dateStr) return 'Never';
        return new Date(dateStr).toLocaleDateString();
    };

    return (
        <>
            <Card id="api-tokens">
                <CardHeader>
                    <div className="flex items-center justify-between">
                        <div>
                            <CardTitle>API Tokens</CardTitle>
                            <CardDescription>
                                Generate tokens for CLI and programmatic access.
                            </CardDescription>
                        </div>
                        <Button onClick={() => setCreateDialogOpen(true)}>
                            <Key className="h-4 w-4 mr-2" />
                            Create Token
                        </Button>
                    </div>
                </CardHeader>
                <CardContent>
                    {tokens.length === 0 ? (
                        <p className="text-muted-foreground text-sm">
                            No API tokens yet. Create one to use with the CLI or for programmatic access.
                        </p>
                    ) : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Name</TableHead>
                                    <TableHead>Token</TableHead>
                                    <TableHead>Created</TableHead>
                                    <TableHead>Expires</TableHead>
                                    <TableHead>Last Used</TableHead>
                                    <TableHead className="w-[50px]"></TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {tokens.map((token) => (
                                    <TableRow key={token.id}>
                                        <TableCell className="font-medium">{token.name}</TableCell>
                                        <TableCell>
                                            <code className="text-sm bg-muted px-2 py-0.5 rounded">
                                                {token.tokenPrefix}...
                                            </code>
                                        </TableCell>
                                        <TableCell>{formatDate(token.createdAtUtc)}</TableCell>
                                        <TableCell>
                                            {token.expiresAtUtc ? (
                                                formatDate(token.expiresAtUtc)
                                            ) : (
                                                <Badge variant="secondary">Never</Badge>
                                            )}
                                        </TableCell>
                                        <TableCell>
                                            {token.lastUsedAtUtc ? formatDate(token.lastUsedAtUtc) : (
                                                <span className="text-muted-foreground">Never used</span>
                                            )}
                                        </TableCell>
                                        <TableCell>
                                            <Button
                                                variant="ghost"
                                                size="sm"
                                                onClick={() => setRevokeTokenId(token.id)}
                                            >
                                                <Trash2 className="h-4 w-4 text-destructive" />
                                            </Button>
                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    )}
                </CardContent>
            </Card>

            {/* Create Token Dialog */}
            <Dialog open={createDialogOpen} onOpenChange={(open) => {
                if (!open) handleCloseCreateDialog();
            }}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Create API Token</DialogTitle>
                        <DialogDescription>
                            Generate a new token for CLI or programmatic access.
                        </DialogDescription>
                    </DialogHeader>

                    {createdToken ? (
                        <div className="space-y-4">
                            <div className="flex items-center gap-2 p-3 bg-yellow-500/10 border border-yellow-500/20 rounded-lg">
                                <AlertTriangle className="h-4 w-4 text-yellow-500 flex-shrink-0" />
                                <p className="text-sm text-yellow-600 dark:text-yellow-400">
                                    This token will only be shown once. Copy it now.
                                </p>
                            </div>
                            <div className="flex items-center gap-2">
                                <code className="flex-1 p-3 bg-muted rounded-lg text-sm break-all font-mono">
                                    {createdToken}
                                </code>
                                <Button
                                    variant="outline"
                                    size="sm"
                                    onClick={() => handleCopy(createdToken)}
                                >
                                    {copied ? (
                                        <Check className="h-4 w-4 text-green-500" />
                                    ) : (
                                        <Copy className="h-4 w-4" />
                                    )}
                                </Button>
                            </div>
                            <DialogFooter>
                                <Button onClick={handleCloseCreateDialog}>Done</Button>
                            </DialogFooter>
                        </div>
                    ) : (
                        <div className="space-y-4">
                            <div className="space-y-2">
                                <label htmlFor="token-name" className="text-sm font-medium">
                                    Token name
                                </label>
                                <Input
                                    id="token-name"
                                    placeholder="e.g. CI/CD Token"
                                    value={tokenName}
                                    onChange={(e) => setTokenName(e.target.value)}
                                />
                            </div>
                            <div className="space-y-2">
                                <label htmlFor="token-expiry" className="text-sm font-medium">
                                    Expiration
                                </label>
                                <Select value={tokenExpiry} onValueChange={setTokenExpiry}>
                                    <SelectTrigger>
                                        <SelectValue />
                                    </SelectTrigger>
                                    <SelectContent>
                                        <SelectItem value="7">7 days</SelectItem>
                                        <SelectItem value="30">30 days</SelectItem>
                                        <SelectItem value="never">Never</SelectItem>
                                    </SelectContent>
                                </Select>
                                {tokenExpiry === 'never' && (
                                    <div className="flex items-center gap-2 text-sm text-yellow-600 dark:text-yellow-400">
                                        <TooltipProvider>
                                            <Tooltip delayDuration={0}>
                                                <TooltipTrigger asChild>
                                                    <AlertTriangle className="h-4 w-4 cursor-help" />
                                                </TooltipTrigger>
                                                <TooltipContent className="w-64">
                                                    This token will never expire. Use with caution — consider rotating tokens periodically.
                                                </TooltipContent>
                                            </Tooltip>
                                        </TooltipProvider>
                                        <span>This token will never expire</span>
                                    </div>
                                )}
                            </div>
                            <DialogFooter>
                                <Button variant="outline" onClick={handleCloseCreateDialog}>
                                    Cancel
                                </Button>
                                <Button
                                    onClick={handleCreateToken}
                                    disabled={!tokenName.trim() || createTokenMutation.isPending}
                                >
                                    {createTokenMutation.isPending ? 'Creating...' : 'Create Token'}
                                </Button>
                            </DialogFooter>
                        </div>
                    )}
                </DialogContent>
            </Dialog>

            {/* Revoke Token Confirmation */}
            <AlertDialog open={!!revokeTokenId} onOpenChange={(open) => {
                if (!open) setRevokeTokenId(null);
            }}>
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>Revoke API Token</AlertDialogTitle>
                        <AlertDialogDescription>
                            Are you sure you want to revoke this token? Any applications or CI/CD pipelines using this token will lose access immediately.
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel>Cancel</AlertDialogCancel>
                        <AlertDialogAction onClick={handleRevokeToken} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
                            Revoke Token
                        </AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </>
    );
}

function RouteComponent() {
  const { toast } = useToast();
  const [installGuideOpen, setInstallGuideOpen] = React.useState(false);
  const [installGuideReason, setInstallGuideReason] = React.useState<string>('default');

  // Handle ephemeral query params for install guide
  const { collect: collectInstallGuide } = useEphemeralQueryParams<InstallGuideParams>(
    ['install-guide-show', 'reason'],
    'install-guide-params'
  );

  React.useEffect(() => {
    const params = collectInstallGuide();
    if (params?.['install-guide-show']) {
      setInstallGuideReason(params.reason || 'default');
      setInstallGuideOpen(true);
    }
  }, []);

  const installGuideContent = INSTALL_GUIDE_REASONS[installGuideReason] ?? INSTALL_GUIDE_REASONS.default!;

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

  useRegisterActions([
    {
      id: "create-api-token",
      name: "Create API token",
      icon: Key,
      group: "action" as const,
      keywords: ["token", "api", "key", "auth"],
      perform: () => {
        document.getElementById("api-tokens")?.scrollIntoView({ behavior: "smooth" });
      },
    },
  ], []);

  return (
    <>
      <Dialog open={installGuideOpen} onOpenChange={setInstallGuideOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{installGuideContent.title}</DialogTitle>
            <DialogDescription>
              {installGuideContent.description}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            {!isLoading && installAppUrl && (
              <Button asChild>
                <a
                  href={installAppUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  onClick={() => setInstallGuideOpen(false)}
                >
                  <Github className="h-4 w-4" />
                  Install GitHub App
                </a>
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>

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

        <ApiTokensSection />

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

              <Separator className="my-6" />

              <InvoicesList />

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
