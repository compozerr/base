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
import { TabsContent } from '@/components/ui/tabs'
import { createFileRoute, getRouteApi, useNavigate, } from '@tanstack/react-router'
import React, { useEffect, useMemo, useState } from 'react'

import { api } from '@/api-client'
import LoadingButton from '@/components/loading-button'
import { Price } from '@/lib/price'
import { useToast } from '@/hooks/use-toast'
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

  return (
    <TabsContent value="general" className="space-y-4 mt-6">
      <Card>
        <CardHeader>
          <CardTitle>General Settings</CardTitle>
          <CardDescription>
            Manage your project's basic settings.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-4">
            {
              !isN8nProject && (
                <div className="space-y-2">
                  <Label htmlFor="project-name">Deployment branch</Label>
                  <Select
                    value={'main'}
                    onValueChange={(value) => console.log(value)}
                  >
                    <SelectTrigger className="w-full sm:w-1/2" id="modules-org">
                      <SelectValue placeholder="Select organization" />
                    </SelectTrigger>
                    <SelectContent>
                      {['main'].map((i) => (
                        <SelectItem key={i} value={i!}>
                          {i}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              )
            }
            {
              !isN8nProject && (
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label htmlFor="auto-deploy">Auto Deploy</Label>
                    <p className="text-sm text-muted-foreground pr-3">
                      Automatically deploy when you push to the main branch.
                    </p>
                  </div>

                  <Switch id="auto-deploy" checked={autoDeploy} disabled={!!error || !projectEnvironmentData} onCheckedChange={async (checked) => {
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
                  }} />
                </div>
              )}
            <div className="space-y-2">
              <Label htmlFor="project-name">Server tier</Label>
              <Select
                value={tier ?? 'Loading...'}
                onValueChange={(value) =>
                  setTier(value === '' ? null : value)
                }
              >
                <SelectTrigger className="w-full sm:w-1/2" id="modules-org">
                  <SelectValue placeholder="Select organization" />
                </SelectTrigger>
                <SelectContent>
                  {tiers && tiers.tiers!.map((t, idx) => (
                    <SelectItem key={idx} value={t.id!.value!}>
                      {t.id!.value} - {t.ramGb}GB RAM, {t.cores} Cores, {t.diskGb}GB Disk - <span className='font-bold'>{Price.formatPrice(t.price)}/month</span> {t.promotionalText ? `(${t.promotionalText})` : ""}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
        <CardFooter>
          <LoadingButton isLoading={savingChanges} disabled={!project || !tier || project.serverTier === tier} onClick={handleOnSaveChangesAsync}>Save Changes</LoadingButton>
        </CardFooter>
      </Card>

      <Card id="danger-zone" className='border-destructive border-dashed'>
        <CardHeader>
          <CardTitle>Danger Zone</CardTitle>
          <CardDescription>Irreversible and destructive actions.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="font-medium text-destructive">Delete Project</h3>
              <p className="text-sm text-muted-foreground pr-3">
                Delete this project and all its resources. This action will also cancel your subscription.
              </p>
            </div>
            <LoadingButton isLoading={wantsDeletion || deleteIsLoading} variant="destructive" onClick={() => {
              setWantsDeletion(true);
            }}>Delete</LoadingButton>
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
    </TabsContent>
  )
}

export default GeneralSettingsTab
