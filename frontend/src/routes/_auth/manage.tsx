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
    const [selectedInstallation, setSelectedInstallation] = React.useState<string>('')

    React.useEffect(() => {
        Promise.allSettled([
            apiClient.v1.github.getInstallAppUrl.get(),
            apiClient.v1.github.getInstalledOrganizations.get()
        ]).then(([urlResult, installationsResult]) => {
            if (urlResult.status === 'fulfilled') {
                setInstallAppUrl(urlResult.value?.installUrl!)
            } else {
                setError(JSON.stringify(urlResult.reason))
            }

            if (installationsResult.status === 'fulfilled') {
                setInstallations(installationsResult.value?.installations!)
                setSelectedInstallation(installationsResult.value?.selectedInstallationId!)
            } else {
                setError('Error getting organizations')
            }
        }).finally(() => {
            setIsLoading(false)
        });
    }, []);

    const onChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
        const installationId = e.target.value;
        setSelectedInstallation(installationId);

        if (!installationId) return;
        apiClient.v1.github.setDeafultOrganization.post({ installationId });
    }

    return (
        <div>
            {isLoading && <p>Loading...</p>}
            {error && <p>Error: {error}</p>}

            {installAppUrl && <a href={installAppUrl} target="_blank">Install the app</a>}
            <br />
            {installations && (
                <select className="form-select mt-2" onChange={onChange} value={selectedInstallation}>
                    {installations!.map(i => (
                        <option key={i.installationId} value={i.installationId!}>{i.name}</option>
                    ))}
                </select>
            )}
        </div>
    );
}
