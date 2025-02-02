import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { MetricsCard } from "@/components/metrics-card"
import { StatsChart } from "@/components/stats-chart"
import { ServicesTable } from "@/components/services-table"
import { BarChart3, ChevronDown, Globe, Home, LayoutDashboard, LifeBuoy, Settings, Server, LogOut } from "lucide-react"
import * as React from 'react'
import { createFileRoute, useNavigate, useRouter } from '@tanstack/react-router'
import { useAuth } from "@/hooks/use-dynamic-auth"

export const Route = createFileRoute('/_auth/dashboard')({
    component: RouteComponent,
})

function RouteComponent() {
    const router = useRouter()
    const navigate = useNavigate()
    const { user, logout } = useAuth();

    const handleLogout = () => {
        if (window.confirm('Are you sure you want to logout?')) {
            logout().then(() => {
                router.invalidate().finally(() => {
                    navigate({ to: '/' })
                })
            })
        }
    }

    return (
        <div className="min-h-screen bg-black text-white">
            <div className="grid lg:grid-cols-[280px_1fr]">
                <aside className="fixed top-0 bottom-0 w-[280px] border-r bg-background/50 backdrop-blur flex flex-col justify-between max-h-screen">
                    <div>
                        <div className="flex h-16 items-center gap-4 border-b px-6">
                            <img src={user?.avatarUrl} alt={`${user?.name}-avatar`} className="h-10 w-10 rounded-full" />
                            <span className="font-bold">Hi, {user?.name}</span>
                        </div>
                        <div className="px-4 py-4">
                            <Input placeholder="Search" className="bg-background/50" />
                        </div>
                        <nav className="space-y-2 px-2">
                            <Button variant="ghost" className="w-full justify-start gap-2">
                                <LayoutDashboard className="h-4 w-4" />
                                Dashboard
                            </Button>
                            <Button variant="ghost" className="w-full justify-start gap-2">
                                <BarChart3 className="h-4 w-4" />
                                Statistics & Usage
                            </Button>
                            <Button variant="ghost" className="w-full justify-start gap-2">
                                <Globe className="h-4 w-4" />
                                Network
                            </Button>
                            <Button variant="ghost" className="w-full justify-start gap-2">
                                <Home className="h-4 w-4" />
                                Services
                            </Button>
                            <Button variant="ghost" className="w-full justify-start gap-2">
                                <Server className="h-4 w-4" />
                                Microservices
                                <ChevronDown className="ml-auto h-4 w-4" />
                            </Button>
                            <Button variant="ghost" className="w-full justify-start gap-2">
                                <LifeBuoy className="h-4 w-4" />
                                Support
                            </Button>
                            <Button variant="ghost" className="w-full justify-start gap-2">
                                <Settings className="h-4 w-4" />
                                Settings
                            </Button>
                        </nav>
                    </div>

                    <nav className="px-2 py-2">
                        <Button onClick={handleLogout} variant="ghost" className="w-full justify-start gap-2">
                            <LogOut className="h-4 w-4" />
                            Logout
                        </Button>
                    </nav>
                </aside>
                <main className="p-6 pl-[300px] overflow-y-auto w-screen">
                    <div className="mb-6 flex items-center justify-between">
                        <div className="space-y-1">
                            <h1 className="text-2xl font-bold">Microservices Overview</h1>
                            <div className="text-sm text-muted-foreground">Aug 13, 2023 - Aug 18, 2023</div>
                        </div>
                        <Button variant="outline" className="gap-2">
                            All Services
                            <ChevronDown className="h-4 w-4" />
                        </Button>
                    </div>
                    <div className="grid gap-4 md:grid-cols-3">
                        <MetricsCard
                            title="Remaining Tokens (vCPU Hours)"
                            value="1,892"
                            change={{ value: "-108", percentage: "-5.4%", isPositive: false }}
                        />
                        <MetricsCard
                            title="Active Services"
                            value="7"
                            change={{ value: "+2", percentage: "+40%", isPositive: true }}
                        />
                        <MetricsCard
                            title="Total vCPU Hours Used"
                            value="246.4"
                            change={{ value: "+18.7", percentage: "+8.2%", isPositive: true }}
                        />
                    </div>
                    <Card className="mt-6 p-6">
                        <div className="mb-4 flex items-center justify-between">
                            <h2 className="text-lg font-semibold">Overall Traffic</h2>
                            <div className="flex gap-2">
                                <Button size="sm" variant="ghost">
                                    Today
                                </Button>
                                <Button size="sm" variant="ghost">
                                    Last week
                                </Button>
                                <Button size="sm" variant="ghost">
                                    Last month
                                </Button>
                                <Button size="sm" variant="ghost">
                                    Last 6 month
                                </Button>
                                <Button size="sm" variant="ghost">
                                    Year
                                </Button>
                            </div>
                        </div>
                        <StatsChart />
                    </Card>
                    <div className="mt-6">
                        <ServicesTable />
                    </div>
                </main>
            </div>
        </div>
    )
}
