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
    const [organizations, setOrganizations] = React.useState<GetInstallationsResponse | null>(null)

    React.useEffect(() => {
        Promise.allSettled([
            apiClient.v1.github.getInstallAppUrl.get(),
            apiClient.v1.github.getInstalledOrganizations.get()
        ]).then(([urlResult, orgsResult]) => {
            if (urlResult.status === 'fulfilled') {
                setInstallAppUrl(urlResult.value!)
            } else {
                setError(JSON.stringify(urlResult.reason))
            }

            if (orgsResult.status === 'fulfilled') {
                setOrganizations(orgsResult.value!)
            } else {
                setError('Error getting organizations')
            }
        }).finally(() => {
            setIsLoading(false)
        });
    }, []);

    const onSelect = (e: React.ChangeEvent<HTMLSelectElement>) => {
        const installationId = e.target.value;

        if (!installationId) return;

        apiClient.v1.github.setDeafultOrganization.post({ installationId });
    }

    return (
        <div>
            {isLoading && <p>Loading...</p>}
            {error && <p>Error: {error}</p>}

            {installAppUrl && <a href={installAppUrl} target="_blank">Install the app</a>}
            <br />
            {organizations && (
                <select className="form-select mt-2" onSelect={onSelect}>
                    {organizations.installations!.map(org => (
                        <option key={org.installationId} value={org.installationId!}>{org.name}</option>
                    ))}
                </select>
            )}
        </div>
    );
}
