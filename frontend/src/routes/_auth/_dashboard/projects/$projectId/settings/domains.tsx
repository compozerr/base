import React, { useState } from 'react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Separator } from '@/components/ui/separator'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { TabsContent } from '@/components/ui/tabs'
import { createFileRoute, useRouter } from '@tanstack/react-router'
import { api } from '@/api-client'
import { useForm } from "@tanstack/react-form"
import { SystemType, SystemTypes } from '../../../../../../lib/system-type'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { FieldInfo } from '@/components/form/field-info'

export const Route = createFileRoute(
  '/_auth/_dashboard/projects/$projectId/settings/domains',
)({
  component: DomainsSettingsTab,
  loader: ({ params: { projectId } }) => {
    return api.v1.getProjectsProjectIdDomains({ parameters: { path: { projectId } } });
  }
})

function DomainsSettingsTab() {
  const [showDnsGuide, setShowDnsGuide] = useState(false)
  const { projectId } = Route.useParams();

  const { invalidate } = useRouter();

  const { mutate } = api.v1.postProjectsProjectIdDomains.useMutation({
    path: {
      projectId
    }
  }, {
    onSuccess: () => {
      invalidate();
    }
  });

  const { data, error } = Route.useLoaderData();

  const addDomainForm = useForm({
    defaultValues: {
      domain: '',
      systemType: "Frontend" as SystemType
    },
    onSubmit: async ({ value }) => {
      mutate(value);
    }
  });

  return (
    <TabsContent value="domains" className="space-y-4 mt-6">
      <Card>
        <CardHeader>
          <CardTitle>Domains</CardTitle>
          <CardDescription>Manage domains for your project.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            {data?.domains?.map(d => {
              return (
                <>
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="font-medium">{d.value}</h3>
                      <p className="text-sm text-muted-foreground">
                        {d.serviceName}
                      </p>
                    </div>

                    {d.isVerified
                      ? <Badge>Verified</Badge>
                      : <Badge className='text-destructive'>Not verified</Badge>}
                  </div>
                  <Separator />
                </>
              )
            })}
          </div>

          <Separator className="my-4" />

          <div className="space-y-2">
            <h3 className="text-lg font-medium">Add Custom Domain</h3>

            <form onSubmit={(e) => {
              e.preventDefault();
              e.stopPropagation();
              addDomainForm.handleSubmit();
            }}>
              <addDomainForm.Field name='domain' children={(field) => (
                <>
                  <Input
                    id={field.name}
                    name={field.name}
                    value={field.state.value}
                    onBlur={field.handleBlur}
                    onChange={(e) => field.handleChange(e.target.value)}
                    placeholder="example.com"
                    className="flex-1"
                  />
                  <FieldInfo field={field} />
                </>
              )} />
              <addDomainForm.Field name="systemType" children={(field) => (
                <>
                  <Select value={field.state.value} onValueChange={(value) => field.handleChange(value as SystemType)}>
                    <SelectTrigger className="w-1/2" id={field.name}>
                      <SelectValue placeholder="Select system type" />
                    </SelectTrigger>
                    <SelectContent>
                      {
                        SystemTypes.map(x => (
                          <SelectItem key={x} value={x}>
                            {x}
                          </SelectItem>
                        ))
                      }
                    </SelectContent>
                  </Select>
                  <FieldInfo field={field} />
                </>
              )} />
              <addDomainForm.Subscribe
                selector={(state) => [state.canSubmit, state.isSubmitting]} children={([canSubmit, isSubmitting]) => (
                  <Button type='submit' onClick={() => setShowDnsGuide(true)}>Add</Button>
                )} />

            </form>
          </div>

          {showDnsGuide && (
            <Card className="mt-6 border-blue-200 bg-blue-50 dark:bg-blue-950 dark:border-blue-800">
              <CardHeader className="pb-2">
                <CardTitle className="text-lg">Set up DNS Records</CardTitle>
                <CardDescription>
                  Configure your domain's DNS settings to point to your project.
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div>
                    <h4 className="font-medium mb-2">1. Add a CNAME record</h4>
                    <div className="rounded-md border bg-card">
                      <Table>
                        <TableHeader>
                          <TableRow>
                            <TableHead>Type</TableHead>
                            <TableHead>Name</TableHead>
                            <TableHead>Value</TableHead>
                            <TableHead>TTL</TableHead>
                          </TableRow>
                        </TableHeader>
                        <TableBody>
                          <TableRow>
                            <TableCell className="font-medium">CNAME</TableCell>
                            <TableCell>www</TableCell>
                            <TableCell>cname.vercel-dns.com</TableCell>
                            <TableCell>3600</TableCell>
                          </TableRow>
                        </TableBody>
                      </Table>
                    </div>
                  </div>

                  <div>
                    <h4 className="font-medium mb-2">
                      2. Add an A record for the apex domain (optional)
                    </h4>
                    <div className="rounded-md border bg-card">
                      <Table>
                        <TableHeader>
                          <TableRow>
                            <TableHead>Type</TableHead>
                            <TableHead>Name</TableHead>
                            <TableHead>Value</TableHead>
                            <TableHead>TTL</TableHead>
                          </TableRow>
                        </TableHeader>
                        <TableBody>
                          <TableRow>
                            <TableCell className="font-medium">A</TableCell>
                            <TableCell>@</TableCell>
                            <TableCell>76.76.21.21</TableCell>
                            <TableCell>3600</TableCell>
                          </TableRow>
                        </TableBody>
                      </Table>
                    </div>
                  </div>

                  <div className="rounded-md bg-muted p-4">
                    <h4 className="font-medium mb-2">DNS Propagation</h4>
                    <p className="text-sm text-muted-foreground">
                      DNS changes can take up to 48 hours to propagate
                      worldwide, though they often take effect much sooner.
                      We'll automatically check your DNS configuration and
                      notify you when your domain is properly configured.
                    </p>
                  </div>

                  <div className="flex justify-end space-x-2">
                    <Button
                      variant="outline"
                      onClick={() => setShowDnsGuide(false)}
                    >
                      Close
                    </Button>
                    <Button>Verify DNS Configuration</Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}
        </CardContent>
      </Card>
    </TabsContent>
  )
}

export default DomainsSettingsTab
