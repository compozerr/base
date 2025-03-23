import * as React from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { useState, useEffect } from "react"
import { Card, CardContent } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import { Globe } from "lucide-react"

interface ProjectData {
  id: string
  name: string
  repoName: string
  state: "Active" | "Inactive" | "Paused" | "Archived"
  vCpuHours: number
  startDate: string
  primaryDomain: string
}

export const Route = createFileRoute('/_auth/_dashboard/projects/$projectId/')({
    component: RouteComponent,
    beforeLoad: async ({ params }) => {
        console.log({ params });
    }
})

function RouteComponent() {
    const [project, setProject] = useState<ProjectData | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    // Simulate API fetch
    setTimeout(() => {
      setProject({
        id: "550e8400-e29b-41d4-a716-446655440000",
        name: "My Awesome Project",
        repoName: "awesome-project",
        state: "Active",
        vCpuHours: 124.5,
        startDate: "2023-09-15T00:00:00Z",
        primaryDomain: "myawesomeproject.com",
      })
      setLoading(false)
    }, 1000)
  }, [])

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("en-US", {
      year: "numeric",
      month: "long",
      day: "numeric",
    })
  }

  const getStateColor = (state: string) => {
    switch (state) {
      case "Active":
        return "bg-green-500"
      case "Inactive":
        return "bg-gray-500"
      case "Paused":
        return "bg-yellow-500"
      case "Archived":
        return "bg-red-500"
      default:
        return "bg-gray-500"
    }
  }

  if (loading) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Skeleton className="h-[200px] w-full" />
        <Skeleton className="h-[200px] w-full" />
        <Skeleton className="h-[200px] w-full" />
        <Skeleton className="h-[200px] w-full" />
      </div>
    )
  }

  if (!project) {
    return (
      <div className="text-center py-10">
        <p>No project data available</p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <h1 className="text-3xl font-bold">{project.name}</h1>
          <Badge className={getStateColor(project.state)}>{project.state}</Badge>
        </div>
        <div className="flex items-center gap-2 text-sm">
          <Globe className="h-4 w-4" />
          <a
            href={`https://${project.primaryDomain}`}
            target="_blank"
            rel="noopener noreferrer"
            className="hover:underline"
          >
            {project.primaryDomain}
          </a>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Card>
          <CardContent className="pt-6">
            <div className="space-y-2">
              <h2 className="text-xl font-semibold">Project Details</h2>
              <div className="grid grid-cols-1 gap-4 mt-4">
                <div>
                  <h3 className="text-sm font-medium text-muted-foreground">Project ID</h3>
                  <p className="text-sm font-mono">{project.id}</p>
                </div>
                <div>
                  <h3 className="text-sm font-medium text-muted-foreground">Repository</h3>
                  <p className="text-sm">{project.repoName}</p>
                </div>
                <div>
                  <h3 className="text-sm font-medium text-muted-foreground">Start Date</h3>
                  <p className="text-sm">{formatDate(project.startDate)}</p>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="space-y-2">
              <h2 className="text-xl font-semibold">Resource Usage</h2>
              <div className="grid grid-cols-1 gap-4 mt-4">
                <div>
                  <h3 className="text-sm font-medium text-muted-foreground">vCPU Hours</h3>
                  <p className="text-2xl font-bold">{project.vCpuHours.toFixed(2)}</p>
                  <p className="text-xs text-muted-foreground">Total usage since project creation</p>
                </div>
                <div>
                  <h3 className="text-sm font-medium text-muted-foreground">Status</h3>
                  <p className="text-sm flex items-center gap-2">
                    <span className={`h-2 w-2 rounded-full ${getStateColor(project.state)}`}></span>
                    {project.state}
                  </p>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="space-y-2">
              <h2 className="text-xl font-semibold">Recent Activity</h2>
              <div className="space-y-4 mt-4">
                <div className="flex items-center justify-between">
                  <p className="text-sm">Last deployment</p>
                  <p className="text-sm text-muted-foreground">2 hours ago</p>
                </div>
                <div className="flex items-center justify-between">
                  <p className="text-sm">Last commit</p>
                  <p className="text-sm text-muted-foreground">Yesterday</p>
                </div>
                <div className="flex items-center justify-between">
                  <p className="text-sm">Last settings change</p>
                  <p className="text-sm text-muted-foreground">3 days ago</p>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="pt-6">
            <div className="space-y-2">
              <h2 className="text-xl font-semibold">Domains</h2>
              <div className="space-y-4 mt-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium">{project.primaryDomain}</p>
                    <p className="text-xs text-muted-foreground">Primary Domain</p>
                  </div>
                  <Badge variant="outline" className="text-green-500 border-green-500">
                    Active
                  </Badge>
                </div>
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium">{project.name}.vercel.app</p>
                    <p className="text-xs text-muted-foreground">Vercel Domain</p>
                  </div>
                  <Badge variant="outline" className="text-green-500 border-green-500">
                    Active
                  </Badge>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
