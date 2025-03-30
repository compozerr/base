import { api } from '@/api-client'
import { useAppForm } from '@/components/form/use-app-form'
import LoadingButton from '@/components/loading-button'
import { Badge } from '@/components/ui/badge'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { TabsContent } from '@/components/ui/tabs'
import { createFileRoute, useRouter } from '@tanstack/react-router'
import { useState } from 'react'
import { z } from "zod"
import { SystemType, SystemTypes } from '../../../../../../lib/system-type'

import VerifyDnsDialog from './!components/verify-dns-dialog'

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

function DomainsSettingsTab() {
  const { projectId } = Route.useParams();
  const { data } = Route.useLoaderData();

  const [selectedDomainId, setSelectedDomainId] = useState<string | null>(null);

  const { invalidate } = useRouter();

  const { mutateAsync } = api.v1.postProjectsProjectIdDomains.useMutation({
    path: {
      projectId
    }
  }, {
    onSuccess: ({ domainId }) => {
      invalidate();
      setSelectedDomainId(domainId || null);
    }
  });

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
                <div key={index}>
                  <div className="flex items-center justify-between">
                    <div>
                      <h3 className="font-medium">{d.value}</h3>
                      <p className="text-sm text-muted-foreground">
                        {d.serviceName}
                      </p>
                    </div>

                    {d.isVerified
                      ? <Badge>Verified</Badge>
                      : <Badge onClick={() => setSelectedDomainId(d.domainId ?? null)} variant="destructive">Not verified</Badge>}
                  </div>
                  {(index !== data.domains!.length - 1) && <Separator />}
                </div>
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

          <VerifyDnsDialog selectedDomainId={selectedDomainId} onClose={() => setSelectedDomainId(null)} projectId={projectId} domains={data?.domains} />
        </CardContent>
      </Card>
    </TabsContent>
  )
}

export default DomainsSettingsTab
