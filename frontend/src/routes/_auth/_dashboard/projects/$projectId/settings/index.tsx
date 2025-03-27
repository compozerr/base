import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { createFileRoute } from "@tanstack/react-router"
import DomainsSettingsTab from "./!components/domains-settings-tab"
import EnvironmentSettingsTab from "./!components/environment-settings-tab"
import GeneralSettingsTab from "./!components/general-settings-tab"

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/settings/',
)({
    component: RouteComponent,
})

function RouteComponent() {
    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-3xl font-bold tracking-tight">Project Settings</h2>
                <p className="text-muted-foreground">Manage your project settings and configuration.</p>
            </div>

            <Tabs defaultValue="general" className="w-full">
                <TabsList className="grid w-full grid-cols-3 lg:w-auto">
                    <TabsTrigger value="general">General</TabsTrigger>
                    <TabsTrigger value="environment">Environment Variables</TabsTrigger>
                    <TabsTrigger value="domains">Domains</TabsTrigger>
                </TabsList>

                <GeneralSettingsTab />
                <EnvironmentSettingsTab />
                <DomainsSettingsTab />
            </Tabs>
        </div>
    )
}
