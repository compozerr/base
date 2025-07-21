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
import { useEffect, useState } from 'react'
import { z } from "zod"
import { SystemType, SystemTypes } from '../../../../../../lib/system-type'

import VerifyDnsDialog from './!components/verify-dns-dialog'
import AreYouSureDialog from '@/components/are-you-sure-dialog'
import { Button } from '@/components/ui/button'
import { Trash2, MoreVertical, Check } from 'lucide-react'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { CopyButton } from '@/components/copy-button'

export const Route = createFileRoute(
  '/_auth/_dashboard/projects/$projectId/settings/domains',
)({
  component: DomainsSettingsTab,
  loader: ({ params: { projectId } }) => {
    return api.v1.getProjectsProjectIdDomains.prefetchQuery({ parameters: { path: { projectId } } });
  }
})

const addDomainSchema = z.object({
  domain: z.string().min(4).max(255).regex(/^[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,}$/, 'Invalid domain name').transform((val) => val.trim()),
  serviceName: z.enum(SystemTypes)
});

function DomainsSettingsTab() {
  const { projectId } = Route.useParams();
  const { data } = api.v1.getProjectsProjectIdDomains.useQuery({
    path: {
      projectId
    }
  });

  const [deleteDomainId, setDeleteDomainId] = useState<string | null>(null);

  const [selectedDomainId, setSelectedDomainId] = useState<string | null>(null);

  const invalidate = () => {
    api.v1.getProjectsProjectIdDomains.invalidateQueries({
      parameters: {
        path: {
          projectId
        }
      }
    });
  }


  const { mutateAsync } = api.v1.postProjectsProjectIdDomains.useMutation({
    path: {
      projectId
    }
  }, {
    onSuccess: ({ domainId }) => {
      invalidate();
      setSelectedDomainId(domainId || null);
    },
  });

  const { mutateAsync: deleteDomainMutateAsync } = api.v1.deleteProjectsProjectIdDomainsDomainId.useMutation({
    path: {
      projectId,
      domainId: deleteDomainId!
    }
  }, {
    onSuccess: () => {
      invalidate();
    }
  })

  const [selectedSetPrimaryDomainId, setSelectedSetPrimaryDomainId] = useState<string | null>(null);

  const { mutateAsync: setPrimaryDomainMutateAsync } = api.v1.postProjectsProjectIdDomainsDomainIdSetPrimary.useMutation({
    path: {
      projectId,
      domainId: selectedSetPrimaryDomainId!
    }
  }, {
    onSuccess: () => {
      invalidate();
      api.v1.getProjectsProjectId.invalidateQueries({
        parameters: {
          path: {
            projectId: projectId
          }
        }
      });
    }
  })

  const deleteDomainAsync = async () => {
    if (!deleteDomainId) return;

    await deleteDomainMutateAsync();
  }

  useEffect(() => {
    if (!selectedSetPrimaryDomainId) return;

    setPrimaryDomainAsync(selectedSetPrimaryDomainId).finally(() => {
      setSelectedSetPrimaryDomainId(null);
    });
  }, [selectedSetPrimaryDomainId])

  const setPrimaryDomainAsync = async (domainId: string) => {
    if (!domainId) return;

    await setPrimaryDomainMutateAsync();
  }

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
                    <div className="min-w-0 flex-1">
                      <h3 className="font-medium truncate">{d.value}</h3>
                      <p className="text-sm text-muted-foreground">
                        {d.serviceName}
                      </p>
                    </div>
                    <section className='flex items-center gap-2'>
                      {selectedSetPrimaryDomainId === d.domainId && <Badge variant="secondary">Setting as primary...</Badge>}
                      {d.isPrimary && <Badge variant="outline">Primary</Badge>}
                      {d.isVerified
                        ? <Badge>Verified</Badge>
                        : <Badge variant="destructive">Not verified</Badge>}
                      {
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" size="icon">
                              <MoreVertical className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            {!d.isVerified && (
                              <DropdownMenuItem onClick={() => setSelectedDomainId(d.domainId ?? null)} disabled={d.isInternal}>
                                <Check className="mr-2 h-4 w-4" />
                                Verify
                              </DropdownMenuItem>
                            )}
                            {
                              d.value && (
                                  <CopyButton
                                    value={d.value}
                                    className="gap-4"
                                    iconClassName='text-primary'
                                  >
                                    <span className="text-sm">Copy Domain</span>
                                  </CopyButton>
                              )
                            }
                            {
                              d.isVerified && !d.isPrimary && (
                                <DropdownMenuItem onClick={() => {
                                  setSelectedSetPrimaryDomainId(d.domainId || null);
                                }}>
                                  <Check className="mr-2 h-4 w-4" />
                                  Set as primary
                                </DropdownMenuItem>
                              )
                            }
                            <DropdownMenuItem
                              className="text-destructive"
                              onClick={() => setDeleteDomainId(d.domainId || null)} disabled={d.isInternal}>
                              <Trash2 className="mr-2 h-4 w-4" />
                              Delete
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      }
                    </section>
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

          <VerifyDnsDialog
            selectedDomainId={selectedDomainId}
            onClose={() => { invalidate(); setSelectedDomainId(null); }}
            projectId={projectId}
            domains={data?.domains} />

          <AreYouSureDialog
            title='Are you sure you want to delete domain'
            subtitle="This action cannot be reverted, you'll have to add it again..."
            open={!!deleteDomainId}
            onAnswer={(ans) => {
              if (ans && deleteDomainId) {
                deleteDomainAsync().finally(() => {
                  setDeleteDomainId(null);
                });
              } else {
                setDeleteDomainId(null);
              }

            }} />
        </CardContent>
      </Card>
    </TabsContent>
  )
}

export default DomainsSettingsTab
