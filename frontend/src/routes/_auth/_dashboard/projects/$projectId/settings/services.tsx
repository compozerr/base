import { api } from '@/api-client'
import LoadingButton from '@/components/loading-button'
import { Button } from '@/components/ui/button'
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Separator } from '@/components/ui/separator'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { createFileRoute, useRouter } from '@tanstack/react-router'
import { TabsContent } from '@radix-ui/react-tabs'
import { useState } from 'react'

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/settings/services',
)({
    component: ServicesSettingsTab,
    loader: ({ params: { projectId } }) => {
        return api.v1.getProjectsProjectIdServices({
            parameters: {
                path: {
                    projectId
                }
            }
        })
    },
})

type Service = {
    name: string
    port: string
    protocol: string
    isSystem: boolean
}

function ServicesSettingsTab() {
    const { data } = Route.useLoaderData();
    const { projectId } = Route.useParams();
    const { invalidate } = useRouter();

    const [services, setServices] = useState<Service[]>(
        data?.services?.map(s => ({
            name: s.name!,
            port: s.port!,
            protocol: s.protocol!,
            isSystem: s.isSystem!
        })) || []
    );

    const [newServiceName, setNewServiceName] = useState('');
    const [newServicePort, setNewServicePort] = useState('');
    const [newServiceProtocol, setNewServiceProtocol] = useState<'http' | 'tcp'>('http');
    const [isSaving, setIsSaving] = useState(false);

    const { mutate } = api.v1.putProjectsProjectIdServices.useMutation({
        path: {
            projectId
        }
    });

    const addService = () => {
        if (!newServiceName || !newServicePort) return;

        const existingService = services.find(s => s.name === newServiceName);
        if (existingService) {
            // Update existing service
            existingService.port = newServicePort;
            existingService.protocol = newServiceProtocol;
            setServices([...services]);
        } else {
            // Add new service
            setServices([
                ...services,
                {
                    name: newServiceName,
                    port: newServicePort,
                    protocol: newServiceProtocol,
                    isSystem: false
                }
            ]);
        }

        setNewServiceName('');
        setNewServicePort('');
        setNewServiceProtocol('http');
    };

    const removeService = (serviceName: string) => {
        setServices(services.filter(s => s.name !== serviceName));
    };

    const updateServiceProtocol = (serviceName: string, protocol: string) => {
        const service = services.find(s => s.name === serviceName);
        if (service) {
            service.protocol = protocol;
            setServices([...services]);
        }
    };

    const saveChanges = () => {
        setIsSaving(true);
        mutate({
            services: services.map(s => ({
                name: s.name,
                port: s.port,
                protocol: s.protocol
            }))
        }, {
            onSuccess: () => {
                invalidate();
            },
            onSettled: () => {
                setIsSaving(false);
            }
        });
    };

    return (
        <TabsContent value="services" className="space-y-4 mt-6">
            <Card>
                <CardHeader>
                    <CardTitle>Services</CardTitle>
                    <CardDescription>
                        Manage the services running in your project. System services (Frontend and Backend) are automatically created.
                        You can add custom services and select their protocol (HTTP or TCP).
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Name</TableHead>
                                <TableHead>Port</TableHead>
                                <TableHead>Protocol</TableHead>
                                <TableHead>Type</TableHead>
                                <TableHead className="w-[100px]"></TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {services.map((service) => (
                                <TableRow key={service.name}>
                                    <TableCell className="font-medium">{service.name}</TableCell>
                                    <TableCell>{service.port}</TableCell>
                                    <TableCell>
                                        {service.isSystem ? (
                                            <span className="text-muted-foreground">{service.protocol}</span>
                                        ) : (
                                            <Select
                                                value={service.protocol}
                                                onValueChange={(value) => updateServiceProtocol(service.name, value)}
                                            >
                                                <SelectTrigger className="w-[100px]">
                                                    <SelectValue />
                                                </SelectTrigger>
                                                <SelectContent>
                                                    <SelectItem value="http">HTTP</SelectItem>
                                                    <SelectItem value="tcp">TCP</SelectItem>
                                                </SelectContent>
                                            </Select>
                                        )}
                                    </TableCell>
                                    <TableCell>
                                        {service.isSystem ? (
                                            <span className="text-xs bg-muted px-2 py-1 rounded">System</span>
                                        ) : (
                                            <span className="text-xs bg-primary/10 px-2 py-1 rounded">Custom</span>
                                        )}
                                    </TableCell>
                                    <TableCell>
                                        {!service.isSystem && (
                                            <Button
                                                variant="destructive"
                                                size="sm"
                                                onClick={() => removeService(service.name)}
                                            >
                                                Remove
                                            </Button>
                                        )}
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>

                    <Separator className="my-4" />

                    <div className="space-y-4">
                        <h3 className="text-lg font-medium">Add Custom Service</h3>
                        <div className="grid grid-cols-4 gap-4">
                            <div>
                                <Label htmlFor="service-name">Name</Label>
                                <Input
                                    id="service-name"
                                    placeholder="Postgres"
                                    value={newServiceName}
                                    onChange={(e) => setNewServiceName(e.target.value)}
                                />
                            </div>
                            <div>
                                <Label htmlFor="service-port">Port</Label>
                                <Input
                                    id="service-port"
                                    placeholder="5432"
                                    value={newServicePort}
                                    onChange={(e) => setNewServicePort(e.target.value)}
                                />
                            </div>
                            <div>
                                <Label htmlFor="service-protocol">Protocol</Label>
                                <Select
                                    value={newServiceProtocol}
                                    onValueChange={(value) => setNewServiceProtocol(value as 'http' | 'tcp')}
                                >
                                    <SelectTrigger id="service-protocol">
                                        <SelectValue />
                                    </SelectTrigger>
                                    <SelectContent>
                                        <SelectItem value="http">HTTP</SelectItem>
                                        <SelectItem value="tcp">TCP</SelectItem>
                                    </SelectContent>
                                </Select>
                            </div>
                            <div className="flex items-end">
                                <Button
                                    onClick={addService}
                                    disabled={!newServiceName || !newServicePort}
                                >
                                    Add Service
                                </Button>
                            </div>
                        </div>
                    </div>

                    <Separator />

                    <LoadingButton
                        isLoading={isSaving}
                        onClick={saveChanges}
                    >
                        Save Changes
                    </LoadingButton>
                </CardContent>
            </Card>
        </TabsContent>
    )
}

export default ServicesSettingsTab
