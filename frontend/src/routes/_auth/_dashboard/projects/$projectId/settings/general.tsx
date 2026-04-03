import AreYouSureDialogConfirmWithText from '@/components/are-you-sure-dialog-confirm-with-text'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
} from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import { createFileRoute, getRouteApi, useNavigate, } from '@tanstack/react-router'
import { useState } from 'react'

import { api } from '@/api-client'
import LoadingButton from '@/components/loading-button'
import { orgFetch } from '@/lib/org-storage'
import { useToast } from '@/hooks/use-toast'
import { cn } from '@/lib/utils'
import { Github, AlertTriangle, Building2, CheckCircle, Trash2, ArrowRightLeft, User, ExternalLink, RefreshCw, Archive, XCircle, Lock, Globe, ArrowRight, GitBranch, Pencil } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { apiBaseUrl } from '@/api-client'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'

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

  const { toast } = useToast();

  const backendUrl = import.meta.env.VITE_BACKEND_URL || '';
  const currentPath = window.location.pathname;
  const reconnectUrl = `${backendUrl}/v1/auth/login?returnUrl=${encodeURIComponent(currentPath)}`;

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

interface Org {
  id: string
  name: string
  slug: string
  isPersonal: boolean
}

function TransferProjectSection({ projectId, projectName }: { projectId: string; projectName?: string }) {
  const [transferDialogOpen, setTransferDialogOpen] = useState(false)
  const [selectedOrgId, setSelectedOrgId] = useState<string | null>(null)
  const { toast } = useToast()
  const queryClient = useQueryClient()

  const { data: organizations } = useQuery<Org[]>({
    queryKey: ['organizations'],
    queryFn: async () => {
      const res = await orgFetch(`${apiBaseUrl}/v1/organizations`)
      if (!res.ok) return []
      return res.json()
    },
    enabled: transferDialogOpen,
  })

  const transferMutation = useMutation({
    mutationFn: async (targetOrgId: string) => {
      const res = await orgFetch(`${apiBaseUrl}/v1/organizations/${targetOrgId}/transfer-project`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ projectId }),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => null)
        throw new Error(err?.detail || 'Failed to transfer project')
      }
      return res.json()
    },
    onSuccess: () => {
      toast({ title: 'Project transferred', description: `${projectName} has been moved.`, variant: 'success' })
      queryClient.invalidateQueries({ queryKey: ['organizations'] })
      setTransferDialogOpen(false)
      setSelectedOrgId(null)
    },
    onError: (error: Error) => {
      toast({ title: 'Error', description: error.message, variant: 'destructive' })
    },
  })

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
              onClick={() => selectedOrgId && transferMutation.mutate(selectedOrgId)}
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

const GITHUB_URL_REGEX = /^https?:\/\/github\.com\/([^/]+\/[^/.]+)\/?$/

// --- Repo status types ---

type RepoStatusState = 'idle' | 'checking' | 'ok' | 'renamed' | 'transferred' | 'archived' | 'notFound' | 'error'

interface RepoStatusResult {
  status: RepoStatusState
  currentName?: string
  newName?: string
  newUrl?: string
  isPrivate?: boolean
  isArchived?: boolean
  defaultBranch?: string
  message?: string
}

// --- Linked Repository Section ---

function LinkedRepositorySection({
  projectId,
  repoName,
  reconnectUrl,
}: {
  projectId: string
  repoName?: string
  reconnectUrl: string
}) {
  const [repoStatus, setRepoStatus] = useState<RepoStatusResult>({ status: 'idle' })
  const [changeRepoDialogOpen, setChangeRepoDialogOpen] = useState(false)
  const [newRepoUrl, setNewRepoUrl] = useState('')
  const [updateConfirmOpen, setUpdateConfirmOpen] = useState(false)
  const { toast } = useToast()

  const checkRepoMutation = useMutation({
    mutationFn: async () => {
      const res = await orgFetch(`${apiBaseUrl}/v1/projects/${projectId}/repo-status`)
      if (!res.ok) {
        const err = await res.json().catch(() => null)
        throw new Error(err?.detail || 'Failed to check repository status')
      }
      return res.json() as Promise<RepoStatusResult>
    },
    onSuccess: (data: RepoStatusResult) => {
      setRepoStatus(data)
      if (data.status === 'ok') {
        toast({ title: 'Repository OK', description: `${repoName} is accessible and up to date.` })
      }
    },
    onError: (error: Error) => {
      setRepoStatus({ status: 'error', message: error.message })
      toast({ title: 'Check failed', description: error.message, variant: 'destructive' })
    },
  })

  const updateRepoUrlMutation = useMutation({
    mutationFn: async (url: string) => {
      const res = await orgFetch(`${apiBaseUrl}/v1/projects/${projectId}/repo-url`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ repoUrl: url }),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => null)
        throw new Error(err?.detail || 'Failed to update repository URL')
      }
      return res.json()
    },
    onSuccess: () => {
      toast({ title: 'Repository updated', description: 'The linked repository has been updated.' })
      api.v1.getProjectsProjectId.invalidateQueries({ parameters: { path: { projectId } } })
      api.v1.getProjects.invalidateQueries()
      setRepoStatus({ status: 'idle' })
      setChangeRepoDialogOpen(false)
      setUpdateConfirmOpen(false)
      setNewRepoUrl('')
    },
    onError: (error: Error) => {
      toast({ title: 'Error', description: error.message, variant: 'destructive' })
    },
  })

  const isValidGithubUrl = GITHUB_URL_REGEX.test(newRepoUrl)
  const parsedNewRepo = newRepoUrl.match(GITHUB_URL_REGEX)?.[1]

  const statusBadge = () => {
    switch (repoStatus.status) {
      case 'ok':
        return (
          <Badge variant="outline" className="text-xs border-green-500/30 text-green-500 bg-green-500/10">
            <CheckCircle className="h-3 w-3 mr-1" />
            Accessible
          </Badge>
        )
      case 'renamed':
        return (
          <Badge variant="outline" className="text-xs border-amber-500/30 text-amber-500 bg-amber-500/10">
            <ArrowRight className="h-3 w-3 mr-1" />
            Renamed
          </Badge>
        )
      case 'transferred':
        return (
          <Badge variant="outline" className="text-xs border-amber-500/30 text-amber-500 bg-amber-500/10">
            <ArrowRightLeft className="h-3 w-3 mr-1" />
            Transferred
          </Badge>
        )
      case 'archived':
        return (
          <Badge variant="outline" className="text-xs border-amber-500/30 text-amber-500 bg-amber-500/10">
            <Archive className="h-3 w-3 mr-1" />
            Archived
          </Badge>
        )
      case 'notFound':
        return (
          <Badge variant="outline" className="text-xs border-red-500/30 text-red-500 bg-red-500/10">
            <XCircle className="h-3 w-3 mr-1" />
            Not Found
          </Badge>
        )
      case 'error':
        return (
          <Badge variant="outline" className="text-xs border-red-500/30 text-red-500 bg-red-500/10">
            <AlertTriangle className="h-3 w-3 mr-1" />
            Error
          </Badge>
        )
      default:
        return null
    }
  }

  const visibilityBadge = () => {
    if (repoStatus.status === 'idle' || repoStatus.status === 'checking' || repoStatus.status === 'notFound') return null
    if (repoStatus.isPrivate === undefined) return null
    return repoStatus.isPrivate ? (
      <Badge variant="outline" className="text-xs border-blue-500/30 text-blue-500 bg-blue-500/10">
        <Lock className="h-3 w-3 mr-1" />
        Private
      </Badge>
    ) : (
      <Badge variant="outline" className="text-xs border-zinc-500/30 text-zinc-400 bg-zinc-500/10">
        <Globe className="h-3 w-3 mr-1" />
        Public
      </Badge>
    )
  }

  return (
    <>
      <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
        <CardContent className="p-6">
          <div className="flex items-start gap-4">
            <div className="flex-shrink-0 w-10 h-10 rounded-lg bg-zinc-800 flex items-center justify-center">
              <Github className="h-5 w-5 text-zinc-400" />
            </div>
            <div className="flex-1 min-w-0 space-y-4">
              {/* Header row */}
              <div>
                <div className="flex items-center gap-2 mb-1 flex-wrap">
                  <h3 className="font-medium">Linked Repository</h3>
                  {statusBadge()}
                  {visibilityBadge()}
                </div>
                {/* Repo name display */}
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
              </div>

              {/* Rename/transfer detected banner */}
              {(repoStatus.status === 'renamed' || repoStatus.status === 'transferred') && repoStatus.newName && (
                <div className="rounded-lg border border-amber-500/20 bg-amber-500/5 p-3 space-y-2">
                  <p className="text-sm text-amber-200">
                    This repository has been <strong>{repoStatus.status}</strong> on GitHub.
                  </p>
                  <div className="flex items-center gap-2 text-xs font-mono">
                    <span className="text-muted-foreground line-through">{repoName}</span>
                    <ArrowRight className="h-3 w-3 text-amber-500" />
                    <span className="text-amber-400">{repoStatus.newName}</span>
                  </div>
                  <LoadingButton
                    isLoading={updateRepoUrlMutation.isPending}
                    size="sm"
                    className="mt-1"
                    onClick={() => setUpdateConfirmOpen(true)}
                  >
                    <RefreshCw className="h-3.5 w-3.5 mr-1.5" />
                    Update to new URL
                  </LoadingButton>
                </div>
              )}

              {/* Archived warning */}
              {repoStatus.status === 'archived' && (
                <div className="rounded-lg border border-amber-500/20 bg-amber-500/5 p-3">
                  <p className="text-sm text-amber-200">
                    This repository is <strong>archived</strong> on GitHub. Deployments will still work, but the repository is read-only.
                  </p>
                </div>
              )}

              {/* Not found warning */}
              {repoStatus.status === 'notFound' && (
                <div className="rounded-lg border border-red-500/20 bg-red-500/5 p-3 space-y-2">
                  <p className="text-sm text-red-300">
                    This repository could not be found on GitHub. It may have been deleted, made private, or you may have lost access.
                  </p>
                  <p className="text-xs text-muted-foreground">
                    Link a different repository below, or reconnect your GitHub account if this is a permissions issue.
                  </p>
                </div>
              )}

              {/* Action buttons */}
              <div className="flex flex-wrap gap-2">
                <LoadingButton
                  isLoading={checkRepoMutation.isPending}
                  variant="outline"
                  size="sm"
                  onClick={() => checkRepoMutation.mutate()}
                >
                  <RefreshCw className={cn("h-3.5 w-3.5 mr-1.5", checkRepoMutation.isPending && "animate-spin")} />
                  Check Repository
                </LoadingButton>
                <Button variant="outline" size="sm" onClick={() => setChangeRepoDialogOpen(true)}>
                  <Pencil className="h-3.5 w-3.5 mr-1.5" />
                  Change Repository
                </Button>
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

      {/* Update URL confirmation dialog */}
      <Dialog open={updateConfirmOpen} onOpenChange={setUpdateConfirmOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Update Repository URL</DialogTitle>
            <DialogDescription>
              The repository has been {repoStatus.status} on GitHub. Update Compozerr to use the new URL?
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-3">
            <div className="rounded-lg border border-zinc-800 bg-zinc-900/30 p-4 space-y-3">
              <div>
                <p className="text-xs text-muted-foreground mb-1">Current</p>
                <p className="text-sm font-mono text-foreground/60 line-through">{repoName}</p>
              </div>
              <div className="flex justify-center">
                <ArrowRight className="h-4 w-4 text-muted-foreground" />
              </div>
              <div>
                <p className="text-xs text-muted-foreground mb-1">New</p>
                <p className="text-sm font-mono text-foreground">{repoStatus.newName}</p>
              </div>
            </div>
            <p className="text-xs text-muted-foreground">
              All future deployments will clone from the new repository URL. Existing deployments are not affected.
            </p>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setUpdateConfirmOpen(false)}>
              Cancel
            </Button>
            <LoadingButton
              isLoading={updateRepoUrlMutation.isPending}
              onClick={() => repoStatus.newUrl && updateRepoUrlMutation.mutate(repoStatus.newUrl)}
            >
              Update URL
            </LoadingButton>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Change repository dialog */}
      <Dialog open={changeRepoDialogOpen} onOpenChange={setChangeRepoDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Change Linked Repository</DialogTitle>
            <DialogDescription>
              Link this project to a different GitHub repository. The new repository must be accessible via your GitHub App installation.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            {repoName && (
              <div className="rounded-lg border border-zinc-800 bg-zinc-900/30 p-3">
                <p className="text-xs text-muted-foreground mb-1">Currently linked</p>
                <p className="text-sm font-mono">{repoName}</p>
              </div>
            )}
            <div className="space-y-2">
              <Label className="text-sm">New Repository URL</Label>
              <Input
                placeholder="https://github.com/owner/repo"
                value={newRepoUrl}
                onChange={(e) => setNewRepoUrl(e.target.value)}
                className="font-mono text-sm"
              />
              {newRepoUrl && !isValidGithubUrl && (
                <p className="text-xs text-red-400">
                  Enter a valid GitHub URL (e.g. https://github.com/owner/repo)
                </p>
              )}
              {parsedNewRepo && (
                <p className="text-xs text-muted-foreground">
                  Will link to: <span className="font-mono text-foreground/80">{parsedNewRepo}</span>
                </p>
              )}
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => {
              setChangeRepoDialogOpen(false)
              setNewRepoUrl('')
            }}>
              Cancel
            </Button>
            <LoadingButton
              isLoading={updateRepoUrlMutation.isPending}
              disabled={!isValidGithubUrl}
              onClick={() => updateRepoUrlMutation.mutate(newRepoUrl)}
            >
              Change Repository
            </LoadingButton>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}

export default GeneralSettingsTab
