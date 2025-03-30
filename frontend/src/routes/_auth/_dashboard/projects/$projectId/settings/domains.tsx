import { api } from '@/api-client'
import { useAppForm } from '@/components/form/use-app-form'
import LoadingButton from '@/components/loading-button'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
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
import { useState } from 'react'
import { z } from "zod"
import { SystemType, SystemTypes } from '../../../../../../lib/system-type'

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"

export const Route = createFileRoute(
  '/_auth/_dashboard/projects/$projectId/settings/domains',
)({
  component: DomainsSettingsTab,
  loader: ({ params: { projectId } }) => {
    return api.v1.getProjectsProjectIdDomains({ parameters: { path: { projectId } } });
  }
})

const addDomainSchema = z.object({
  domain: z.string().min(4).max(255).regex(/^[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,}$/, 'Invalid domain name'),
  serviceName: z.enum(SystemTypes)
});

type DnsGuideData = {
  name: string,
  value: string
}

function DomainsSettingsTab() {
  const [dnsGuide, setDnsGuide] = useState<DnsGuideData | null>(null)
  const { projectId } = Route.useParams();

  const {data: parentDomainData} = api.v1.

  const { invalidate } = useRouter();

  const { mutateAsync } = api.v1.postProjectsProjectIdDomains.useMutation({
    path: {
      projectId
    }
  }, {
    onSuccess: ({ domain, parentDomain }) => {
      invalidate();
      setDnsGuide({
        name: domain!,
        value: parentDomain!
      });
    }
  });

  const { data } = Route.useLoaderData();

  const addDomainForm = useAppForm({
    defaultValues: {
      domain: '',
      serviceName: "Frontend" as SystemType
    },
    validators: {
      onChange: addDomainSchema
    },
    onSubmit: async ({ value }) => {
      await mutateAsync(value);
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
            {data?.domains?.map((d, index) => {
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
                      : <Badge onClick={() => setDnsGuide(true)} variant="destructive">Not verified</Badge>}
                  </div>
                  {(index !== data.domains!.length - 1) && <Separator />}
                </>
              )
            })}
          </div>

          <Separator className="my-4" />

          <div className="space-y-2">
            <h3 className="text-lg font-medium">Add Custom Domain</h3>

            <form onSubmit={(e) => {
              e.preventDefault();
              addDomainForm.handleSubmit();
            }}>
              <section className='flex flex-row gap-2 mb-4'>
                <addDomainForm.AppField name='domain' children={(field) => (
                  <field.TextField className='w-full' placeholder='example.com' />
                )} />
                <addDomainForm.AppField name="serviceName" children={(field) => (
                  <field.SelectField className='w-[250px]' values={SystemTypes} />
                )} />
              </section>
              <addDomainForm.Subscribe
                selector={(state) => [state.canSubmit, state.isSubmitting]} children={([canSubmit, isSubmitting]) => (
                  <LoadingButton isLoading={!!isSubmitting} disabled={!canSubmit} type='submit'>Add</LoadingButton>
                )} />
            </form>
          </div>
          <Dialog open={!!dnsGuide || true}>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Set up DNS Records</DialogTitle>
                <DialogDescription>
                  Configure your domain's DNS settings to point to your project.
                </DialogDescription>
              </DialogHeader>
              <div className="space-y-4">
                <div>
                  <h4 className="font-medium mb-2">Add CNAME record</h4>
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

                <div className="rounded-md bg-muted p-4">
                  <h4 className="font-medium mb-2">DNS Propagation</h4>
                  <p className="text-sm text-muted-foreground">
                    DNS changes can take up to 48 hours to propagate
                    worldwide, though they often take effect much sooner.
                    {/* We'll automatically check your DNS configuration and
                    notify you when your domain is properly configured. */}
                  </p>
                </div>
              </div>
              <DialogFooter>
                <Button
                  variant="outline"
                  onClick={() => setDnsGuide(null)}
                >
                  Close
                </Button>
                <Button>Verify DNS Configuration</Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>

        </CardContent>
      </Card>
    </TabsContent>
  )
}

export default DomainsSettingsTab
