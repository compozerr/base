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
import React, { useState } from 'react'

import { api } from '@/api-client'
import LoadingButton from '@/components/loading-button'
export const Route = createFileRoute(
  '/_auth/_dashboard/projects/$projectId/settings/general',
)({
  component: GeneralSettingsTab,
})

function GeneralSettingsTab() {

  const [wantsDeletion, setWantsDeletion] = useState(false);

  const params = Route.useParams();

  const { projectId } = getRouteApi("/_auth/_dashboard/projects/$projectId").useLoaderData();

  const { data: project } = api.v1.getProjectsProjectId.useQuery({ path: { projectId } })

  const { mutateAsync: deleteAsync } = api.v1.deleteProjectsProjectId.useMutation({ path: { projectId: params.projectId } })
  const deleteProjectAsync = async () => {
    if (!wantsDeletion) return;
    await deleteAsync();
    await api.v1.getProjects.invalidateQueries();
  }

  const navigate = useNavigate();

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
            <div className="space-y-2">
              <Label htmlFor="project-name">Deployment branch</Label>
              <Select
                value={'main'}
                onValueChange={(value) => console.log(value)}
              >
                <SelectTrigger className="w-1/2" id="modules-org">
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
            <div className="flex items-center justify-between">
              <div className="space-y-0.5">
                <Label htmlFor="auto-deploy">Auto Deploy</Label>
                <p className="text-sm text-muted-foreground">
                  Automatically deploy when you push to the main branch.
                </p>
              </div>

              <Switch id="auto-deploy" defaultChecked disabled={true} />
            </div>
          </div>
        </CardContent>
        <CardFooter>
          <Button disabled={true}>Save Changes</Button>
        </CardFooter>
      </Card>

      <Card className='border-destructive border-dashed'>
        <CardHeader>
          <CardTitle>Danger Zone</CardTitle>
          <CardDescription>Irreversible and destructive actions.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="font-medium text-destructive">Delete Project</h3>
              <p className="text-sm text-muted-foreground">
                Permanently delete this project and all its resources.
              </p>
            </div>
            <LoadingButton isLoading={wantsDeletion} variant="destructive" onClick={() => {
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
