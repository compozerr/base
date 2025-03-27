
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { Switch } from '@/components/ui/switch';
import { TabsContent } from '@/components/ui/tabs';
import { PlusCircle, Trash2 } from 'lucide-react';
import React, { useRef, useState } from 'react';

interface Props {

}

const EnvironmentSettingsTab: React.FC<Props> = (props) => {
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
                            if (!key) return;
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
    );
}

export default EnvironmentSettingsTab;