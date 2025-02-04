import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { MetricsCard } from '@/components/metrics-card'
import { StatsChart } from '@/components/stats-chart'
import { ServicesTable } from '@/components/services-table'
import {
    BarChart3,
    ChevronDown,
    Globe,
    Home,
    LayoutDashboard,
    LifeBuoy,
    Settings,
    Server,
    LogOut,
    FileText,
} from 'lucide-react'
import * as React from 'react'
import {
    createFileRoute,
    Outlet,
    useNavigate,
    useRouter,
} from '@tanstack/react-router'
import { useAuth } from '@/hooks/use-dynamic-auth'

export const Route = createFileRoute('/_auth/dashboard')({
    component: RouteComponent,
})

function RouteComponent() {
    const router = useRouter()
    const navigate = useNavigate()
    const { user, logout } = useAuth()

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
                            <img
                                src={user?.avatarUrl}
                                alt={`${user?.name}-avatar`}
                                className="h-8 w-8 rounded-full"
                            />
                            <span className="font-bold">Hi, {user?.name}</span>
                        </div>
                        <div className="px-4 py-2">
                            {/* <Input placeholder="Search" className="bg-background/50" /> */}
                        </div>
                        <nav className="space-y-2 px-2">
                            <Button variant="ghost" onClick={() => navigate({ viewTransition: true, to: "/dashboard" })} className="w-full justify-start gap-2">
                                <LayoutDashboard className="h-4 w-4" />
                                Dashboard
                            </Button>
                            <Button variant="ghost" onClick={() => navigate({ viewTransition: true, to: "/dashboard/services" })} className="w-full justify-start gap-2">
                                <Home className="h-4 w-4" />
                                Services
                            </Button>
                            <Button
                                variant="ghost"
                                onClick={() => navigate({ viewTransition: true, to: '/dashboard/settings' })}
                                className="w-full justify-start gap-2"
                            >
                                <Settings className="h-4 w-4" />
                                Settings
                            </Button>
                        </nav>
                    </div>

                    <nav className="px-2 py-2">
                        <Button
                            onClick={() => open("https://docs.compozerr.com", "_blank")}
                            variant="ghost"
                            className="w-full justify-start gap-2"
                        >
                            <FileText className="h-4 w-4" />
                            Documentation
                        </Button>
                        <Button
                            onClick={handleLogout}
                            variant="ghost"
                            className="w-full justify-start gap-2"
                        >
                            <LogOut className="h-4 w-4" />
                            Logout
                        </Button>
                    </nav>
                </aside>
                <main className="p-6 pl-[300px] overflow-y-auto w-screen">
                    <Outlet />
                </main>
            </div >
        </div >
    )
}
