

import type React from "react"

import { Badge } from "@/components/ui/badge"

import { useState, useRef } from "react"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Button } from "@/components/ui/button"
import { Switch } from "@/components/ui/switch"
import { Separator } from "@/components/ui/separator"
import { PlusCircle, Trash2 } from "lucide-react"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { createFileRoute } from "@tanstack/react-router"

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/settings/',
)({
    component: RouteComponent,
})

function RouteComponent() {
    const [envVars, setEnvVars] = useState([
        { key: "DATABASE_URL", value: "postgres://user:password@localhost:5432/mydb", isSecret: true },
        { key: "API_KEY", value: "sk_test_123456789", isSecret: true },
        { key: "NODE_ENV", value: "production", isSecret: false },
    ])

    const [newEnvKey, setNewEnvKey] = useState("")
    const [newEnvValue, setNewEnvValue] = useState("")
    const [newEnvIsSecret, setNewEnvIsSecret] = useState(false)
    const [isDragging, setIsDragging] = useState(false)
    const fileInputRef = useRef<HTMLInputElement>(null)
    const [showDnsGuide, setShowDnsGuide] = useState(false)

    const addEnvVar = () => {
        if (newEnvKey.trim() && newEnvValue.trim()) {
            setEnvVars([...envVars, { key: newEnvKey, value: newEnvValue, isSecret: newEnvIsSecret }])
            setNewEnvKey("")
            setNewEnvValue("")
            setNewEnvIsSecret(false)
        }
    }

    const removeEnvVar = (index: number) => {
        const newEnvVars = [...envVars]
        newEnvVars.splice(index, 1)
        setEnvVars(newEnvVars)
    }

    const handleDragOver = (e: React.DragEvent) => {
        e.preventDefault()
        setIsDragging(true)
    }

    const handleDragLeave = () => {
        setIsDragging(false)
    }

    const handleDrop = (e: React.DragEvent) => {
        e.preventDefault()
        setIsDragging(false)

        if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
            const file = e.dataTransfer.files[0]
            if (file?.name.endsWith(".env")) {
                handleEnvFile(file)
            }
        }
    }

    const handleEnvFile = (file: File) => {
        const reader = new FileReader()
        reader.onload = (e) => {
            const content = e.target?.result as string
            if (content) {
                const lines = content.split("\n")
                const newVars: { key: string; value: string; isSecret: boolean }[] = []

                lines.forEach((line) => {
                    // Skip comments and empty lines
                    if (line.trim() && !line.startsWith("#")) {
                        const parts = line.split("=")
                        if (parts.length >= 2) {
                            const key = parts[0]?.trim()
                            const value = parts.slice(1).join("=").trim()
                            // Assume variables with "KEY", "SECRET", "PASSWORD", or "TOKEN" are secrets
                            if(!key) return;
                            const isSecret = /key|secret|password|token/i.test(key)
                            newVars.push({ key, value, isSecret })
                        }
                    }
                })

                if (newVars.length > 0) {
                    setEnvVars([...envVars, ...newVars])
                }
            }
        }
        reader.readAsText(file)
    }

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

                <TabsContent value="general" className="space-y-4 mt-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>General Settings</CardTitle>
                            <CardDescription>Manage your project's basic settings.</CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="space-y-2">
                                <Label htmlFor="project-name">Project Name</Label>
                                <Input id="project-name" defaultValue="My Awesome Project" />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="repo-name">Repository Name</Label>
                                <Input id="repo-name" defaultValue="awesome-project" />
                            </div>

                            <Separator className="my-4" />

                            <div className="space-y-4">
                                <h3 className="text-lg font-medium">Project Status</h3>
                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label htmlFor="auto-deploy">Auto Deploy</Label>
                                        <p className="text-sm text-muted-foreground">
                                            Automatically deploy when you push to the main branch.
                                        </p>
                                    </div>
                                    <Switch id="auto-deploy" defaultChecked />
                                </div>
                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label htmlFor="preview-deployments">Preview Deployments</Label>
                                        <p className="text-sm text-muted-foreground">Create preview deployments for pull requests.</p>
                                    </div>
                                    <Switch id="preview-deployments" defaultChecked />
                                </div>
                            </div>
                        </CardContent>
                        <CardFooter>
                            <Button>Save Changes</Button>
                        </CardFooter>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle>Danger Zone</CardTitle>
                            <CardDescription>Irreversible and destructive actions.</CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="flex items-center justify-between">
                                <div>
                                    <h3 className="font-medium">Archive Project</h3>
                                    <p className="text-sm text-muted-foreground">Archive this project and make it read-only.</p>
                                </div>
                                <Button variant="outline">Archive</Button>
                            </div>
                            <Separator />
                            <div className="flex items-center justify-between">
                                <div>
                                    <h3 className="font-medium text-destructive">Delete Project</h3>
                                    <p className="text-sm text-muted-foreground">
                                        Permanently delete this project and all its resources.
                                    </p>
                                </div>
                                <Button variant="destructive">Delete</Button>
                            </div>
                        </CardContent>
                    </Card>
                </TabsContent>

                <TabsContent value="environment" className="space-y-4 mt-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>Environment Variables</CardTitle>
                            <CardDescription>Manage environment variables for your project.</CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            {/* Drag and drop area for .env file */}
                            <div
                                className="border-2 border-dashed border-muted-foreground/25 rounded-lg p-6 text-center cursor-pointer hover:bg-muted/50 transition-colors"
                                onDragOver={handleDragOver}
                                onDragLeave={handleDragLeave}
                                onDrop={handleDrop}
                            >
                                <input
                                    type="file"
                                    id="env-file-upload"
                                    className="hidden"
                                    accept=".env"
                                    onChange={(e) => {
                                        if (e.target.files && e.target.files.length > 0) {
                                            const file = e.target.files[0]
                                            if (file?.name.endsWith(".env")) {
                                                handleEnvFile(file)
                                            }
                                        }
                                    }}
                                    ref={fileInputRef}
                                />
                                <label htmlFor="env-file-upload" className="cursor-pointer">
                                    <div className="flex flex-col items-center gap-2">
                                        <div className="rounded-full bg-primary/10 p-2">
                                            <PlusCircle className="h-6 w-6 text-primary" />
                                        </div>
                                        <h3 className="text-lg font-medium">Upload .env file</h3>
                                        <p className="text-sm text-muted-foreground max-w-md mx-auto">
                                            Drag and drop your .env file here, or click to browse. We'll automatically parse and add the
                                            variables.
                                        </p>
                                    </div>
                                </label>
                            </div>

                            <Separator className="my-4" />

                            <div className="space-y-4">
                                {envVars.map((env, index) => (
                                    <div key={index} className="flex items-center gap-4">
                                        <div className="grid flex-1 gap-2">
                                            <div className="grid grid-cols-2 gap-4">
                                                <div>
                                                    <Label htmlFor={`env-key-${index}`}>Key</Label>
                                                    <Input id={`env-key-${index}`} value={env.key} readOnly />
                                                </div>
                                                <div>
                                                    <Label htmlFor={`env-value-${index}`}>Value</Label>
                                                    <Input
                                                        id={`env-value-${index}`}
                                                        value={env.isSecret ? "••••••••••••••••" : env.value}
                                                        readOnly
                                                    />
                                                </div>
                                            </div>
                                            <div className="flex items-center space-x-2">
                                                <Switch id={`env-secret-${index}`} checked={env.isSecret} />
                                                <Label htmlFor={`env-secret-${index}`}>Secret</Label>
                                            </div>
                                        </div>
                                        <Button
                                            variant="ghost"
                                            size="icon"
                                            className="text-destructive"
                                            onClick={() => removeEnvVar(index)}
                                        >
                                            <Trash2 className="h-4 w-4" />
                                        </Button>
                                    </div>
                                ))}
                            </div>

                            <Separator className="my-4" />

                            <div className="space-y-4">
                                <h3 className="text-lg font-medium">Add New Variable</h3>
                                <div className="grid grid-cols-2 gap-4">
                                    <div className="space-y-2">
                                        <Label htmlFor="new-env-key">Key</Label>
                                        <Input
                                            id="new-env-key"
                                            placeholder="NEW_VARIABLE"
                                            value={newEnvKey}
                                            onChange={(e) => setNewEnvKey(e.target.value)}
                                        />
                                    </div>
                                    <div className="space-y-2">
                                        <Label htmlFor="new-env-value">Value</Label>
                                        <Input
                                            id="new-env-value"
                                            placeholder="value"
                                            value={newEnvValue}
                                            onChange={(e) => setNewEnvValue(e.target.value)}
                                        />
                                    </div>
                                </div>
                                <div className="flex items-center space-x-2">
                                    <Switch id="new-env-secret" checked={newEnvIsSecret} onCheckedChange={setNewEnvIsSecret} />
                                    <Label htmlFor="new-env-secret">Secret</Label>
                                </div>
                                <Button onClick={addEnvVar}>
                                    <PlusCircle className="mr-2 h-4 w-4" />
                                    Add Variable
                                </Button>
                            </div>
                        </CardContent>
                    </Card>
                </TabsContent>

                <TabsContent value="domains" className="space-y-4 mt-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>Domains</CardTitle>
                            <CardDescription>Manage domains for your project.</CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="space-y-2">
                                <div className="flex items-center justify-between">
                                    <div>
                                        <h3 className="font-medium">project-abc123.vercel.app</h3>
                                        <p className="text-sm text-muted-foreground">Default Vercel domain</p>
                                    </div>
                                    <Badge>Default</Badge>
                                </div>
                                <Separator />
                                <div className="flex items-center justify-between">
                                    <div>
                                        <h3 className="font-medium">myawesomeproject.com</h3>
                                        <p className="text-sm text-muted-foreground">Custom domain</p>
                                    </div>
                                    <Badge variant="outline">Valid</Badge>
                                </div>
                            </div>

                            <Separator className="my-4" />

                            <div className="space-y-2">
                                <h3 className="text-lg font-medium">Add Custom Domain</h3>
                                <div className="flex gap-2">
                                    <Input placeholder="example.com" className="flex-1" />
                                    <Button onClick={() => setShowDnsGuide(true)}>Add</Button>
                                </div>
                            </div>

                            {showDnsGuide && (
                                <Card className="mt-6 border-blue-200 bg-blue-50 dark:bg-blue-950 dark:border-blue-800">
                                    <CardHeader className="pb-2">
                                        <CardTitle className="text-lg">Set up DNS Records</CardTitle>
                                        <CardDescription>Configure your domain's DNS settings to point to your project.</CardDescription>
                                    </CardHeader>
                                    <CardContent>
                                        <div className="space-y-4">
                                            <div>
                                                <h4 className="font-medium mb-2">1. Add a CNAME record</h4>
                                                <div className="rounded-md border bg-card">
                                                    <Table>
                                                        <TableHeader>
                                                            <TableRow>
                                                                <TableHead>Type</TableHead>
                                                                <TableHead>Name</TableHead>
                                                                <TableHead>Value</TableHead>
                                                                <TableHead>TTL</TableHead>
                                                            </TableRow>
                                                        </TableHeader>
                                                        <TableBody>
                                                            <TableRow>
                                                                <TableCell className="font-medium">CNAME</TableCell>
                                                                <TableCell>www</TableCell>
                                                                <TableCell>cname.vercel-dns.com</TableCell>
                                                                <TableCell>3600</TableCell>
                                                            </TableRow>
                                                        </TableBody>
                                                    </Table>
                                                </div>
                                            </div>

                                            <div>
                                                <h4 className="font-medium mb-2">2. Add an A record for the apex domain (optional)</h4>
                                                <div className="rounded-md border bg-card">
                                                    <Table>
                                                        <TableHeader>
                                                            <TableRow>
                                                                <TableHead>Type</TableHead>
                                                                <TableHead>Name</TableHead>
                                                                <TableHead>Value</TableHead>
                                                                <TableHead>TTL</TableHead>
                                                            </TableRow>
                                                        </TableHeader>
                                                        <TableBody>
                                                            <TableRow>
                                                                <TableCell className="font-medium">A</TableCell>
                                                                <TableCell>@</TableCell>
                                                                <TableCell>76.76.21.21</TableCell>
                                                                <TableCell>3600</TableCell>
                                                            </TableRow>
                                                        </TableBody>
                                                    </Table>
                                                </div>
                                            </div>

                                            <div className="rounded-md bg-muted p-4">
                                                <h4 className="font-medium mb-2">DNS Propagation</h4>
                                                <p className="text-sm text-muted-foreground">
                                                    DNS changes can take up to 48 hours to propagate worldwide, though they often take effect much
                                                    sooner. We'll automatically check your DNS configuration and notify you when your domain is
                                                    properly configured.
                                                </p>
                                            </div>

                                            <div className="flex justify-end space-x-2">
                                                <Button variant="outline" onClick={() => setShowDnsGuide(false)}>
                                                    Close
                                                </Button>
                                                <Button>Verify DNS Configuration</Button>
                                            </div>
                                        </div>
                                    </CardContent>
                                </Card>
                            )}
                        </CardContent>
                    </Card>
                </TabsContent>
            </Tabs>
        </div>
    )
}
