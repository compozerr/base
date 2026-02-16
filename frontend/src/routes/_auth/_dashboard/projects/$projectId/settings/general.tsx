import AreYouSureDialogConfirmWithText from '@/components/are-you-sure-dialog-confirm-with-text'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  CardFooter,
} from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Switch } from '@/components/ui/switch'
import { createFileRoute, getRouteApi, useNavigate, } from '@tanstack/react-router'
import React, { useEffect, useMemo, useState } from 'react'

import { api } from '@/api-client'
import LoadingButton from '@/components/loading-button'
import { Price } from '@/lib/price'
import { useToast } from '@/hooks/use-toast'
import { cn } from '@/lib/utils'
import { Github, AlertTriangle, CheckCircle, Cpu, HardDrive, MemoryStick, Trash2 } from 'lucide-react'
import { Badge } from '@/components/ui/badge'

export const Route = createFileRoute(
  '/_auth/_dashboard/projects/$projectId/settings/general',
)({
  component: GeneralSettingsTab,
  loader: ({ params: { projectId } }) => {
    return api.v1.getProjectsProjectIdEnvironment({
      parameters: {
        path: {
          projectId
        },
        query: {
          branch: "main"
        }
      }
    })
  },
})

function GeneralSettingsTab() {
  const { data: projectEnvironmentData, error } = Route.useLoaderData();

  const [autoDeploy, setAutoDeploy] = useState(projectEnvironmentData?.autoDeploy ?? false);

  const [wantsDeletion, setWantsDeletion] = useState(false);

  const params = Route.useParams();

  const { projectId } = getRouteApi("/_auth/_dashboard/projects/$projectId").useLoaderData();

  const { data: project } = api.v1.getProjectsProjectId.useQuery({ path: { projectId } })

  const { mutateAsync: deleteAsync, isPending: deleteIsLoading } = api.v1.deleteProjectsProjectId.useMutation({ path: { projectId: params.projectId } })
  const deleteProjectAsync = async () => {
    if (!wantsDeletion) return;
    await deleteAsync();
    await api.v1.getProjects.invalidateQueries();
  }

  const { data: tiers } = api.v1.getServersTiers.useQuery();

  const { mutateAsync: changeTierAsync } = api.v1.postStripeSubscriptionsUpsert.useMutation();
  const { mutateAsync: changeAutoDeployAsync } = api.v1.putProjectsProjectIdEnvironmentChangeAutoDeploy.useMutation({ path: { projectId } });

  const [tier, setTier] = useState<string | null>(null);

  const [savingChanges, setSavingChanges] = useState(false);

  useEffect(() => {
    if (project?.serverTier) {
      setTier(project.serverTier);
    }
  }, [project, tiers]);

  const navigate = useNavigate();

  const { toast } = useToast();

  const handleTierChangeAsync = async () => {
    await changeTierAsync({
      body: {

        projectId,
        tier,
      }
    })

    await api.v1.getProjectsProjectId.invalidateQueries({ parameters: { path: { projectId } } });
  }

  const handleOnSaveChangesAsync = async () => {
    if (!project) return;
    if (!tier) {
      alert('Please select a server tier.');
      return;
    }

    setSavingChanges(true);

    await handleTierChangeAsync();

    toast({
      title: 'Changes saved',
      description: `Server tier changed to ${tier}. Therefor started a new deployment.`,
    });

    setSavingChanges(false);
  }

  const isN8nProject = useMemo(() => project?.type === "N8n", [project]);

  const backendUrl = import.meta.env.VITE_BACKEND_URL || '';
  const currentPath = window.location.pathname;
  const reconnectUrl = `${backendUrl}/v1/auth/login?returnUrl=${encodeURIComponent(currentPath)}`;

  const hasChanges = project && tier && project.serverTier !== tier;

  return (
    <div className="space-y-6">
      {/* GitHub Connection */}
      <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
        <CardContent className="p-6">
          <div className="flex items-start gap-4">
            <div className="flex-shrink-0 w-10 h-10 rounded-lg bg-zinc-800 flex items-center justify-center">
              <Github className="h-5 w-5 text-zinc-400" />
            </div>
            <div className="flex-1 min-w-0 space-y-3">
              <div>
                <div className="flex items-center gap-2 mb-1">
                  <h3 className="font-medium">GitHub Connection</h3>
                  <Badge variant="outline" className="text-xs border-green-500/30 text-green-500 bg-green-500/10">
                    <CheckCircle className="h-3 w-3 mr-1" />
                    Connected
                  </Badge>
                </div>
                <p className="text-sm text-muted-foreground">
                  If deployments fail due to authentication errors, reconnect your GitHub account to refresh your access token.
                </p>
              </div>
              <Button variant="outline" size="sm" className="w-full sm:w-auto" asChild>
                <a href={reconnectUrl}>
                  <Github className="mr-2 h-4 w-4" />
                  Reconnect
                </a>
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* General Settings */}
      <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
        <CardHeader className="pb-4">
          <CardTitle className="text-lg">General Settings</CardTitle>
          <CardDescription>
            Configure your project's deployment and server settings.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {!isN8nProject && (
            <div className="space-y-2">
              <Label htmlFor="deployment-branch" className="text-sm font-medium">Deployment Branch</Label>
              <Select
                value={'main'}
                onValueChange={(value) => console.log(value)}
              >
                <SelectTrigger className="w-full sm:w-64" id="deployment-branch">
                  <SelectValue placeholder="Select branch" />
                </SelectTrigger>
                <SelectContent>
                  {['main'].map((i) => (
                    <SelectItem key={i} value={i!}>
                      <span className="font-mono text-sm">{i}</span>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

          {!isN8nProject && (
            <div className="flex items-center justify-between rounded-lg border border-zinc-800 bg-zinc-900/30 p-4">
              <div className="space-y-0.5">
                <Label htmlFor="auto-deploy" className="text-sm font-medium">Auto Deploy</Label>
                <p className="text-sm text-muted-foreground">
                  Automatically deploy when you push to the main branch.
                </p>
              </div>
              <Switch
                id="auto-deploy"
                checked={autoDeploy}
                disabled={!!error || !projectEnvironmentData}
                onCheckedChange={async (checked) => {
                  setAutoDeploy(checked);
                  await changeAutoDeployAsync({ autoDeploy: checked }).then(() => {
                    toast({
                      title: 'Auto Deploy changed',
                      description: `Auto Deploy is now ${checked ? 'enabled' : 'disabled'}.`
                    });
                  }).catch((err) => {
                    toast({
                      title: 'Error changing Auto Deploy',
                      description: err.message,
                      variant: 'destructive'
                    });
                  })
                }}
              />
            </div>
          )}

          <div className="space-y-3">
            <Label className="text-sm font-medium">Server Tier</Label>
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {tiers && tiers.tiers!.map((t, idx) => {
                const isCurrentTier = project?.serverTier === t.id.value;
                const isSelected = tier === t.id.value;
                const currentPriceValue = isCurrentTier ? project?.serverTierPrice ?? t.price : t.price;
                const isDiscounted = isCurrentTier && project?.serverTierPrice?.isDiscounted || false;

                return (
                  <button
                    key={idx}
                    type="button"
                    onClick={() => setTier(t.id!.value!)}
                    className={cn(
                      'relative flex flex-col gap-3 rounded-lg border p-4 text-left transition-all',
                      isSelected
                        ? 'border-primary bg-primary/5 ring-1 ring-primary'
                        : 'border-zinc-800 bg-zinc-900/30 hover:border-zinc-700 hover:bg-zinc-900/50',
                    )}
                  >
                    <div className="flex items-center justify-between">
                      <span className="font-mono text-sm font-medium">{t.id!.value}</span>
                      {isCurrentTier && (
                        <Badge variant="secondary" className="text-xs">Current</Badge>
                      )}
                    </div>
                    <div className="flex items-center gap-3 text-xs text-muted-foreground">
                      <span className="flex items-center gap-1">
                        <MemoryStick className="h-3 w-3" />
                        {t.ramGb}GB
                      </span>
                      <span className="flex items-center gap-1">
                        <Cpu className="h-3 w-3" />
                        {t.cores} Core{t.cores !== 1 ? 's' : ''}
                      </span>
                      <span className="flex items-center gap-1">
                        <HardDrive className="h-3 w-3" />
                        {t.diskGb}GB
                      </span>
                    </div>
                    <div className="flex items-baseline gap-1">
                      <span className={cn('text-lg font-semibold', { 'text-red-400': isDiscounted })}>
                        {Price.formatPrice(currentPriceValue)}
                      </span>
                      <span className="text-xs text-muted-foreground">/month</span>
                    </div>
                    {t.promotionalText && (
                      <Badge variant="outline" className="w-fit text-xs border-amber-500/30 text-amber-500 bg-amber-500/10">
                        {t.promotionalText}
                      </Badge>
                    )}
                  </button>
                )
              })}
            </div>
          </div>
        </CardContent>
        <CardFooter className="border-t border-zinc-800 pt-6">
          <LoadingButton
            isLoading={savingChanges}
            disabled={!hasChanges}
            onClick={handleOnSaveChangesAsync}
            className="w-full sm:w-auto"
          >
            Save Changes
          </LoadingButton>
        </CardFooter>
      </Card>

      {/* Danger Zone */}
      <Card id="danger-zone" className="rounded-xl border-dashed border-red-500/30 bg-red-500/5">
        <CardHeader className="pb-4">
          <CardTitle className="text-lg flex items-center gap-2 text-red-500">
            <AlertTriangle className="h-5 w-5" />
            Danger Zone
          </CardTitle>
          <CardDescription>Irreversible and destructive actions.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div>
              <h3 className="font-medium">Delete Project</h3>
              <p className="text-sm text-muted-foreground pr-3">
                Permanently delete this project and all its resources. This will also cancel your subscription.
              </p>
            </div>
            <LoadingButton
              isLoading={wantsDeletion || deleteIsLoading}
              variant="destructive"
              size="sm"
              onClick={() => setWantsDeletion(true)}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              Delete
            </LoadingButton>
            <AreYouSureDialogConfirmWithText
              title='Are you sure you want to delete this project'
              subtitle="This action cannot be reverted, you'll have to add it again..."
              destructiveButton='Delete'
              cancelButton='Cancel'
              textToAnswer={project?.name ?? "confirm"}
              open={wantsDeletion}
              onAnswer={(ans) => {
                if (ans && wantsDeletion) {
                  deleteProjectAsync().finally(() => {
                    setWantsDeletion(false);

                    navigate({ to: '/projects', params: {}, replace: true });
                  });
                } else {
                  setWantsDeletion(false);
                }

              }} />
          </div>
        </CardContent>
      </Card>
    </div>
  )
}

export default GeneralSettingsTab
