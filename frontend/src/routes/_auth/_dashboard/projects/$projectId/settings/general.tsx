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
import { useMemo, useState } from 'react'

import { api } from '@/api-client'
import LoadingButton from '@/components/loading-button'
import { Input } from '@/components/ui/input'
import { useToast } from '@/hooks/use-toast'
import { cn } from '@/lib/utils'
import { Github, AlertTriangle, Building2, Trash2, ArrowRightLeft, User, ExternalLink, GitBranch, Pencil, Search, Lock } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { useRegisterActions } from '@/lib/command-palette/action-registry'

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

  useRegisterActions([
    {
      id: "delete-project",
      name: "Delete project",
      icon: Trash2,
      group: "action" as const,
      keywords: ["remove", "destroy", "danger"],
      priority: 90,
      perform: () => setWantsDeletion(true),
    },
    {
      id: "transfer-project",
      name: "Transfer project",
      icon: ArrowRightLeft,
      group: "action" as const,
      keywords: ["move", "organization"],
      perform: () => {
        document.getElementById("transfer-project")?.scrollIntoView({ behavior: "smooth" });
      },
    },
  ], []);

  return (
    <div className="space-y-6">
      {/* Linked Repository */}
      <LinkedRepositorySection projectId={projectId} repoName={project?.repoName} reconnectUrl={reconnectUrl} />

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

function TransferProjectSection({ projectId, projectName }: { projectId: string; projectName?: string | null }) {
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
        api.v1.getProjects.invalidateQueries()
        setTransferDialogOpen(false)
        setSelectedOrgId(null)
      },
      // Let the global ApiErrorHandler render the destructive toast — the backend
      // returns ProblemDetails (e.g. "no payment method on target") that already
      // carry a user-readable detail, and the global handler unpacks it correctly.
    },
  )

  return (
    <>
      <Card id="transfer-project" className="rounded-xl border-zinc-800 bg-zinc-900/50">
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
              You must be an admin of the current organization and the target
              organization. If the project has an active subscription, billing
              is migrated to the target organization — it must already have a
              payment method on file.
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
  projectId,
  repoName,
  reconnectUrl,
}: {
  projectId: string
  repoName?: string | null
  reconnectUrl: string
}) {
  const [changeRepoOpen, setChangeRepoOpen] = useState(false)

  return (
    <>
      <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
        <CardContent className="p-6">
          <div className="flex items-start gap-4">
            <div className="flex-shrink-0 w-10 h-10 rounded-lg bg-zinc-800 flex items-center justify-center">
              <Github className="h-5 w-5 text-zinc-400" />
            </div>
            <div className="flex-1 min-w-0 space-y-4">
              <div>
                <h3 className="font-medium mb-1">Linked Repository</h3>
                <div className="flex items-center gap-1.5 mt-1">
                  {repoName && (
                    <a
                      className="inline-flex items-center gap-1.5 text-sm font-mono hover:text-foreground text-foreground/80 transition-colors"
                      href={`https://github.com/${repoName}`}
                      target="_blank"
                      rel="noreferrer"
                    >
                      <GitBranch className="h-3.5 w-3.5 text-muted-foreground" />
                      {repoName}
                      <ExternalLink className="h-3 w-3 text-muted-foreground" />
                    </a>
                  )}
                  <button
                    type="button"
                    onClick={() => setChangeRepoOpen(true)}
                    title="Change linked repository"
                    aria-label="Change linked repository"
                    className="ml-1 inline-flex h-6 w-6 items-center justify-center rounded text-muted-foreground hover:text-foreground hover:bg-zinc-800 transition-colors"
                  >
                    <Pencil className="h-3.5 w-3.5" />
                  </button>
                </div>
                <p className="text-xs text-muted-foreground mt-2">
                  Compozerr automatically detects if this repository is renamed or transferred on GitHub and updates the link for you. If the auto-detect didn't catch it, use the pencil to pick the new repo manually.
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

      <ChangeRepositoryDialog
        open={changeRepoOpen}
        onOpenChange={setChangeRepoOpen}
        projectId={projectId}
        currentRepoName={repoName}
      />
    </>
  )
}

function ChangeRepositoryDialog({
  open,
  onOpenChange,
  projectId,
  currentRepoName,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  projectId: string
  currentRepoName?: string | null
}) {
  const [search, setSearch] = useState('')
  const [selectedFullName, setSelectedFullName] = useState<string | null>(null)
  const { toast } = useToast()

  const { data: repos, isLoading: reposLoading } = api.v1.getGithubUserRepositories.useQuery(
    undefined,
    { enabled: open },
  )

  const filteredRepos = useMemo(() => {
    if (!repos) return []
    const q = search.trim().toLowerCase()
    const sorted = [...repos].sort((a, b) => (a.fullName ?? '').localeCompare(b.fullName ?? ''))
    if (!q) return sorted
    return sorted.filter((r) => (r.fullName ?? '').toLowerCase().includes(q))
  }, [repos, search])

  const changeRepoMutation = api.v1.postProjectsProjectIdChangeRepository.useMutation(
    { path: { projectId } },
    {
      onSuccess: async () => {
        toast({
          title: 'Repository updated',
          description: `Project is now linked to ${selectedFullName}. The next deploy will use the new repo.`,
          variant: 'success',
        })
        await api.v1.getProjectsProjectId.invalidateQueries({ parameters: { path: { projectId } } })
        onOpenChange(false)
        setSelectedFullName(null)
        setSearch('')
      },
      onError: (error: unknown) => {
        toast({
          title: 'Could not change repository',
          description: error instanceof Error ? error.message : String(error),
          variant: 'destructive',
        })
      },
    },
  )

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>Change linked repository</DialogTitle>
          <DialogDescription>
            Re-link this project to a different GitHub repository — useful if the
            repo was transferred to another organization or renamed and Compozerr's
            auto-detect didn't catch it. The next deploy will clone from the new repo.
          </DialogDescription>
        </DialogHeader>

        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            autoFocus
            placeholder="Search repositories…"
            className="pl-9 font-mono text-sm"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <div className="max-h-72 overflow-y-auto space-y-1.5 pr-1">
          {reposLoading && (
            <p className="text-sm text-muted-foreground py-4 text-center">Loading repositories…</p>
          )}
          {!reposLoading && filteredRepos.length === 0 && (
            <p className="text-sm text-muted-foreground py-4 text-center">
              No repositories found. Make sure the Compozerr GitHub App is installed on the new owner.
            </p>
          )}
          {filteredRepos.map((repo) => {
            const isCurrent = repo.fullName === currentRepoName
            const isSelected = selectedFullName === repo.fullName
            return (
              <button
                key={repo.fullName}
                type="button"
                onClick={() => setSelectedFullName(repo.fullName)}
                disabled={isCurrent}
                className={cn(
                  'flex w-full items-center gap-3 rounded-lg border p-3 text-left transition-all',
                  isSelected
                    ? 'border-primary bg-primary/5 ring-1 ring-primary'
                    : 'border-zinc-800 bg-zinc-900/30 hover:border-zinc-700',
                  isCurrent && 'opacity-50 cursor-not-allowed',
                )}
              >
                <div className="flex h-8 w-8 items-center justify-center rounded-md bg-muted shrink-0">
                  <Github className="h-4 w-4 text-muted-foreground" />
                </div>
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-mono truncate flex items-center gap-1.5">
                    {repo.fullName}
                    {repo.private && <Lock className="h-3 w-3 text-muted-foreground" />}
                  </p>
                  <p className="text-xs text-muted-foreground truncate">
                    {isCurrent ? 'Currently linked' : `Installation: ${repo.installationName}`}
                  </p>
                </div>
              </button>
            )
          })}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <LoadingButton
            isLoading={changeRepoMutation.isPending}
            disabled={!selectedFullName || selectedFullName === currentRepoName}
            onClick={() =>
              selectedFullName &&
              changeRepoMutation.mutate({ repoFullName: selectedFullName })
            }
          >
            Link repository
          </LoadingButton>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

export default GeneralSettingsTab
