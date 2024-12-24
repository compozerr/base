import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { apiClient } from '../../api-client'
import { GetInstallationsResponse } from '../../generated/models'

export const Route = createFileRoute('/_auth/manage')({
    component: RouteComponent,
})

function RouteComponent() {
    const [isLoading, setIsLoading] = React.useState(true)
    const [error, setError] = React.useState<string | null>(null)
    const [installAppUrl, setInstallAppUrl] = React.useState<string | null>(null)
    const [installations, setInstallations] = React.useState<GetInstallationsResponse["installations"] | null>(null)
    const [selectedProjectsInstallationId, setSelectedProjectsInstallationId] = React.useState<string>('')
    const [selectedModulesInstallationId, setSelectedModulesInstallationId] = React.useState<string>('')
    // const [avatarUrl, setAvatarUrl] = React.useState<string | null>(null);

    React.useEffect(() => {
        Promise.allSettled([
            apiClient.v1.github.getInstallAppUrl.get(),
            apiClient.v1.github.getInstalledOrganizations.get(),
            // apiClient.v1.auth.me.get()
        ]).then(([urlResult, installationsResult]) => {
            if (urlResult.status === 'fulfilled') {
                setInstallAppUrl(urlResult.value?.installUrl!)
            } else {
                setError(JSON.stringify(urlResult.reason))
            }

            if (installationsResult.status === 'fulfilled') {
                setInstallations(installationsResult.value?.installations!)
                setSelectedProjectsInstallationId(installationsResult.value?.selectedProjectsInstallationId!)
                setSelectedModulesInstallationId(installationsResult.value?.selectedModulesInstallationId!)
            } else {
                setError('Error getting organizations')
            }

            // if (meResult.status === "fulfilled") {
            //     setAvatarUrl(meResult.value?.avatarUrl!);
            // }
        }).finally(() => {
            setIsLoading(false)
        });
    }, []);

    const onChangeProjects = (e: React.ChangeEvent<HTMLSelectElement>) => {
        const installationId = e.target.value;
        setSelectedProjectsInstallationId(installationId);

        if (!installationId) return;
        apiClient.v1.github.setDeafultOrganization.post({ installationId, type: 1 });
    }

    const onChangeModules = (e: React.ChangeEvent<HTMLSelectElement>) => {
        const installationId = e.target.value;
        setSelectedModulesInstallationId(installationId);

        if (!installationId) return;
        apiClient.v1.github.setDeafultOrganization.post({ installationId, type: 2 });
    }

    return (
        <div>
            {isLoading && <p>Loading...</p>}
            {error && <p>Error: {error}</p>}

            {/* {avatarUrl && <img style={{ height: "20px", width: "20px", borderRadius: "100px", position: "absolute", right: "20px", top: "20px" }} src={avatarUrl} alt="avatar" />} */}

            {installAppUrl && <a href={installAppUrl} target="_blank">Install the app</a>}
            <br />
            <h3>Default project organization</h3>
            {installations && (
                <select className="form-select mt-2" onChange={onChangeProjects} value={selectedProjectsInstallationId}>
                    {installations!.map(i => (
                        <option key={i.installationId} value={i.installationId!}>{i.name}</option>
                    ))}
                </select>
            )}
            <br />

            <h3>Default modules organization</h3>
            {installations && (
                <select className="form-select mt-2" onChange={onChangeModules} value={selectedModulesInstallationId}>
                    {installations!.map(i => (
                        <option key={i.installationId} value={i.installationId!}>{i.name}</option>
                    ))}
                </select>
            )}
        </div>
    );
}
