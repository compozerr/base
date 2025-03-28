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
import { createFileRoute } from '@tanstack/react-router'
import React from 'react'

export const Route = createFileRoute(
  '/_auth/_dashboard/projects/$projectId/settings/general',
)({
  component: GeneralSettingsTab,
})

function GeneralSettingsTab() {
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

      {/* <Card>
                        <CardHeader>
                            <CardTitle>Danger Zone</CardTitle>
                            <CardDescription>Irreversible and destructive actions.</CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="flex items-center justify-between">
                                <div>
                                    <h3 className="font-medium">Archive Project</h3>
                                    <p className="text-sm text-muted-foreground">Archive this project and make it read-only.</p>
                                </div>
                                <Button variant="outline">Archive</Button>
                            </div>
                            <Separator />
                            <div className="flex items-center justify-between">
                                <div>
                                    <h3 className="font-medium text-destructive">Delete Project</h3>
                                    <p className="text-sm text-muted-foreground">
                                        Permanently delete this project and all its resources.
                                    </p>
                                </div>
                                <Button variant="destructive">Delete</Button>
                            </div>
                        </CardContent>
                    </Card> */}
    </TabsContent>
  )
}

export default GeneralSettingsTab
