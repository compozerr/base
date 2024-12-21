import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { apiClient } from '../../api-client'
import { GetInstalledOrganizationsResponse } from '../../generated/models'

export const Route = createFileRoute('/_auth/manage')({
    component: RouteComponent,
})

function RouteComponent() {
    const [isLoading, setIsLoading] = React.useState(true)
    const [error, setError] = React.useState<string | null>(null)
    const [installAppUrl, setInstallAppUrl] = React.useState<string | null>(null)
    const [organizations, setOrganizations] = React.useState<GetInstalledOrganizationsResponse | null>(null)

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

    return (
        <div>
            {isLoading && <p>Loading...</p>}
            {error && <p>Error: {error}</p>}
            
            {installAppUrl && <a href={installAppUrl}>Install the app</a>}
        {organizations && (
            <select className="form-select mt-2">
                {organizations.installations!.map(org => (
                    <option key={org.id} value={org.id!}>{org.name}</option>
                ))}
                <option value="install">
                    <a href={installAppUrl!}>Install the app</a>
                </option>
            </select>
        )}
        </div>
    );
}
