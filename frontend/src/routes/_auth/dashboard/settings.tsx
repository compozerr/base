import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { api } from '@/api-client'
import StyledLink from '@/components/styled-link'
import { Download } from 'lucide-react'

export const Route = createFileRoute('/_auth/dashboard/settings')({
  component: RouteComponent,
})

function RouteComponent() {
  const {
    data: appUrlData,
    isLoading: appUrlLoading,
    error: appUrlError,
  } = api.v1.getGithubGetInstallAppUrl.useQuery()
  const {
    data: installationsData,
    isLoading: installationsLoading,
    error: installationsError,
  } = api.v1.getGithubGetInstalledOrganizations.useQuery()

  const isLoading = appUrlLoading || installationsLoading
  const error =
    (appUrlError as Error)?.message ||
    (installationsError as Error)?.message ||
    null

  const installAppUrl = appUrlData?.installUrl || null

  const [selectedProjectsInstallationId, setSelectedProjectsInstallationId] =
    React.useState<string>('')
  const [selectedModulesInstallationId, setSelectedModulesInstallationId] =
    React.useState<string>('')

  const defaultOrganizationMutation =
    api.v1.postGithubSetDeafultOrganization.useMutation()

  const installations = React.useMemo(
    () => installationsData?.installations || [],
    [installationsData],
  )

  React.useEffect(() => {
    if (!installationsData) return

    setSelectedProjectsInstallationId(
      installationsData.selectedProjectsInstallationId!,
    )
    setSelectedModulesInstallationId(
      installationsData.selectedModulesInstallationId!,
    )
  }, [installationsData])

  const handleDefaultOrganizationChange = React.useCallback(
    (installationId: string, type: 1 | 2) => {
      setSelectedProjectsInstallationId(installationId)
      if (installationId) {
        defaultOrganizationMutation.mutate({
          body: { installationId, type: type },
        })
      }
    },
    [defaultOrganizationMutation],
  )

  return (
    <div>
      {isLoading && <p>Loading...</p>}
      {error && <p>Error: {error}</p>}

      {installAppUrl && (
        <StyledLink type="button" href={installAppUrl} target="_blank">
          Install the app <Download />
        </StyledLink>
      )}
      
      <h3>Default project organization</h3>
      {installations && (
        <select
          className="form-select mt-2"
          onChange={(e) => handleDefaultOrganizationChange(e.target.value, 1)}
          value={selectedProjectsInstallationId}
        >
          {installations!.map((i) => (
            <option key={i.installationId} value={i.installationId!}>
              {i.name}
            </option>
          ))}
        </select>
      )}
      <br />

      <h3>Default modules organization</h3>
      {installations && (
        <select
          className="form-select mt-2"
          onChange={(e) => handleDefaultOrganizationChange(e.target.value, 2)}
          value={selectedModulesInstallationId}
        >
          {installations!.map((i) => (
            <option key={i.installationId} value={i.installationId!}>
              {i.name}
            </option>
          ))}
        </select>
      )}
    </div>
  )
}
