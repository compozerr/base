import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { ServicesTable } from '@/components/services-table';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Search, Plus } from 'lucide-react';
import { getLink } from '@/links';

export const Route = createFileRoute('/_auth/dashboard/services')({
    component: RouteComponent,
})

function RouteComponent() {
    return (
        <div className="mx-auto">
            <header className="mb-8">
                <h1 className="text-3xl font-bold mb-2">Services Dashboard</h1>
                <p className="text-muted-foreground">Manage and monitor your running services</p>
            </header>

            <div className="grid gap-6 md:grid-cols-3 mb-8">
                <DashboardCard title="Total Services" value="3" />
                <DashboardCard title="Running Services" value="1" />
                <DashboardCard title="Total vCPU Hours" value="246.4" />
            </div>

            <div className="bg-card rounded-lg shadow-sm p-6">
                <div className="flex flex-col md:flex-row justify-between items-center mb-6 gap-4">
                    <div className="flex items-center w-full md:w-auto">
                        <div className="relative max-w-sm mr-2">
                            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                            <Input
                                placeholder="Search services..."
                                className="pl-9"
                            />
                        </div>
                        <Select defaultValue="all">
                            <SelectTrigger className="w-[180px]">
                                <SelectValue placeholder="Filter by status" />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="all">All Services</SelectItem>
                                <SelectItem value="running">Running</SelectItem>
                                <SelectItem value="stopped">Stopped</SelectItem>
                                <SelectItem value="starting">Starting</SelectItem>
                            </SelectContent>
                        </Select>
                    </div>
                    <Button onClick={()=>open(getLink("addNewService"), "_blank")}>
                        <Plus className="mr-2 h-4 w-4" /> Add New Service
                    </Button>
                </div>
                <ServicesTable />
            </div>
        </div>
    )
}

function DashboardCard({ title, value }: { title: string; value: string }) {
    return (
        <div className="bg-card rounded-lg shadow-sm p-6">
            <h3 className="text-lg font-medium mb-2">{title}</h3>
            <p className="text-3xl font-bold">{value}</p>
        </div>
    )
}