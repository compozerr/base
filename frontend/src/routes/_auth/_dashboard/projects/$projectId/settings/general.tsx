import AreYouSureDialogConfirmWithText from '@/components/are-you-sure-dialog-confirm-with-text'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
} from '@/components/ui/card'
import { createFileRoute, getRouteApi, useNavigate, } from '@tanstack/react-router'
import { useState } from 'react'

import { api } from '@/api-client'
import LoadingButton from '@/components/loading-button'
import { useToast } from '@/hooks/use-toast'
import { cn } from '@/lib/utils'
import { Github, AlertTriangle, Building2, Trash2, ArrowRightLeft, User, ExternalLink, GitBranch } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'

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

  const { mutateAsync: deleteAsync, isPending: deleteIsLoading } = api.v1.deleteProjectsProjectId.useMutation({ path: { projectId: params.projectId } })
  const deleteProjectAsync = async () => {
    if (!wantsDeletion) return;
    await deleteAsync();
    await api.v1.getProjects.invalidateQueries();
  }

  const navigate = useNavigate();

  const backendUrl = import.meta.env.VITE_BACKEND_URL || '';
  const currentPath = window.location.pathname;
  const reconnectUrl = `${backendUrl}/v1/auth/login?returnUrl=${encodeURIComponent(currentPath)}`;

  return (
    <div className="space-y-6">
      {/* Linked Repository */}
      <LinkedRepositorySection repoName={project?.repoName} reconnectUrl={reconnectUrl} />

      {/* Transfer Project */}
      <TransferProjectSection projectId={projectId} projectName={project?.name} />

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

function TransferProjectSection({ projectId, projectName }: { projectId: string; projectName?: string }) {
  const [transferDialogOpen, setTransferDialogOpen] = useState(false)
  const [selectedOrgId, setSelectedOrgId] = useState<string | null>(null)
  const { toast } = useToast()

  const { data: organizations } = api.v1.getOrganizations.useQuery(undefined, {
    enabled: transferDialogOpen,
  })

  const transferMutation = api.v1.postOrganizationsOrganizationIdTransferProject.useMutation(
    undefined as void,
    {
      onSuccess: () => {
        toast({ title: 'Project transferred', description: `${projectName} has been moved.`, variant: 'success' })
        api.v1.getOrganizations.invalidateQueries()
        setTransferDialogOpen(false)
        setSelectedOrgId(null)
      },
      onError: (error) => {
        toast({ title: 'Error', description: error instanceof Error ? error.message : String(error), variant: 'destructive' })
      },
    },
  )

  return (
    <>
      <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
        <CardHeader className="pb-4">
          <CardTitle className="text-lg flex items-center gap-2">
            <ArrowRightLeft className="h-5 w-5" />
            Transfer Project
          </CardTitle>
          <CardDescription>Move this project to a different organization.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-muted-foreground pr-3">
                Transfer this project to another organization you are an admin of.
                All settings, deployments, and domains will be preserved.
              </p>
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setTransferDialogOpen(true)}
            >
              <ArrowRightLeft className="h-4 w-4 mr-2" />
              Transfer
            </Button>
          </div>
        </CardContent>
      </Card>

      <Dialog open={transferDialogOpen} onOpenChange={setTransferDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Transfer Project</DialogTitle>
            <DialogDescription>
              Select the organization to transfer <strong>{projectName}</strong> to.
              You must be an admin of the target organization.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-2">
            {organizations?.map((org) => (
              <button
                key={org.id}
                onClick={() => setSelectedOrgId(org.id)}
                className={cn(
                  'flex w-full items-center gap-3 rounded-lg border p-3 text-left transition-all',
                  selectedOrgId === org.id
                    ? 'border-primary bg-primary/5 ring-1 ring-primary'
                    : 'border-zinc-800 bg-zinc-900/30 hover:border-zinc-700'
                )}
              >
                <div className="flex h-8 w-8 items-center justify-center rounded-md bg-muted">
                  {org.isPersonal ? (
                    <User className="h-4 w-4 text-muted-foreground" />
                  ) : (
                    <Building2 className="h-4 w-4 text-muted-foreground" />
                  )}
                </div>
                <div>
                  <p className="text-sm font-medium">
                    {org.isPersonal ? 'Personal Account' : org.name}
                  </p>
                  <p className="text-xs text-muted-foreground font-mono">{org.slug}</p>
                </div>
              </button>
            ))}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setTransferDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={() => selectedOrgId && transferMutation.mutate({ path: { organizationId: selectedOrgId }, body: { projectId } })}
              disabled={!selectedOrgId || transferMutation.isPending}
            >
              {transferMutation.isPending ? 'Transferring...' : 'Transfer Project'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}

// --- Linked Repository Section ---

function LinkedRepositorySection({
  repoName,
  reconnectUrl,
}: {
  repoName?: string
  reconnectUrl: string
}) {
  return (
    <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
      <CardContent className="p-6">
        <div className="flex items-start gap-4">
          <div className="flex-shrink-0 w-10 h-10 rounded-lg bg-zinc-800 flex items-center justify-center">
            <Github className="h-5 w-5 text-zinc-400" />
          </div>
          <div className="flex-1 min-w-0 space-y-4">
            <div>
              <h3 className="font-medium mb-1">Linked Repository</h3>
              {repoName && (
                <a
                  className="inline-flex items-center gap-1.5 text-sm font-mono hover:text-foreground text-foreground/80 transition-colors mt-1"
                  href={`https://github.com/${repoName}`}
                  target="_blank"
                  rel="noreferrer"
                >
                  <GitBranch className="h-3.5 w-3.5 text-muted-foreground" />
                  {repoName}
                  <ExternalLink className="h-3 w-3 text-muted-foreground" />
                </a>
              )}
              <p className="text-xs text-muted-foreground mt-2">
                Compozerr automatically detects if this repository is renamed or transferred on GitHub and updates the link for you.
              </p>
            </div>

            <div className="flex flex-wrap gap-2">
              <Button variant="outline" size="sm" asChild>
                <a href={reconnectUrl}>
                  <Github className="h-3.5 w-3.5 mr-1.5" />
                  Reconnect Account
                </a>
              </Button>
            </div>

            <p className="text-xs text-muted-foreground">
              If deployments fail due to authentication errors, reconnect your GitHub account to refresh your access token.
            </p>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

export default GeneralSettingsTab
