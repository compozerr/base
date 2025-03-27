
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { Switch } from '@/components/ui/switch';
import { TabsContent } from '@/components/ui/tabs';
import { cn } from '@/lib/utils';
import { PlusCircle, Trash2, Undo } from 'lucide-react';
import React, { useRef, useState } from 'react';

interface Props {

}

type EnvironmentVar = {
    key: string,
    value: string,
    isNew: boolean,
    isDeleting: boolean,
}

const EnvironmentSettingsTab: React.FC<Props> = (props) => {
    const [envVars, setEnvVars] = useState<EnvironmentVar[]>([
        { key: "DATABASE_URL", value: "postgres://user:password@localhost:5432/mydb", isNew: true, isDeleting: false },
        { key: "API_KEY", value: "sk_test_123456789", isNew: true, isDeleting: false, },
        { key: "NODE_ENV", value: "production", isNew: false, isDeleting: false },
    ])

    const [newEnvKey, setNewEnvKey] = useState("")
    const [newEnvValue, setNewEnvValue] = useState("")
    const [isDragging, setIsDragging] = useState(false)
    const fileInputRef = useRef<HTMLInputElement>(null)

    const addEnvVar = () => {
        if (newEnvKey.trim() && newEnvValue.trim()) {
            setEnvVars([...envVars, { key: newEnvKey, value: newEnvValue, isNew: true, isDeleting: false }])
            setNewEnvKey("")
            setNewEnvValue("")
        }
    }

    const toggleRemoveEnvVar = (index: number) => {
        const newEnvVars = [...envVars]
        if (newEnvVars[index]?.isNew) {
            newEnvVars.splice(index, 1)
        } else {
            newEnvVars[index] = { ...newEnvVars[index], isDeleting: !newEnvVars[index]!.isDeleting } as EnvironmentVar
        }

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
                const newVars: EnvironmentVar[] = []

                lines.forEach((line) => {
                    // Skip comments and empty lines
                    if (line.trim() && !line.startsWith("#")) {
                        const parts = line.split("=")
                        if (parts.length >= 2) {
                            const key = parts[0]?.trim()
                            const value = parts.slice(1).join("=").trim()
                            if (!key) return;
                            newVars.push({ key, value, isNew: true, isDeleting: false })
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
                        className={cn("border-2 border-dashed border-muted-foreground/25 rounded-lg p-6 text-center cursor-pointer hover:bg-muted/50 transition-colors", isDragging && "bg-muted/50")}
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
                                            <Input id={`env-key-${index}`} className={env.isDeleting ? "line-through text-red-600" : ""} value={env.key} readOnly disabled={!env.isNew} />
                                        </div>
                                        <div>
                                            <Label htmlFor={`env-value-${index}`}>Value</Label>
                                            <Input
                                                id={`env-value-${index}`}
                                                className={env.isDeleting ? "line-through text-red-600" : ""}
                                                value={env.value}
                                                readOnly
                                                disabled={!env.isNew}
                                            />
                                        </div>
                                    </div>
                                </div>
                                <Button
                                    variant="ghost"
                                    size="icon"
                                    className=""
                                    onClick={() => toggleRemoveEnvVar(index)}
                                >
                                    {env.isDeleting ? <Undo className='h-4 w-4 ' /> : <Trash2 className="h-4 w-4 text-destructive" />}
                                </Button>
                            </div>
                        ))}
                    </div>

                    <Separator className="my-4" />

                    <div className="space-y-4">
                        <h3 className="text-lg font-medium">Add New Variable</h3>
                        <div className="flex flex-row items-center gap-4">
                            <div className="grid flex-1 gap-2">

                                <div className="grid grid-cols-2 gap-4">

                                    <div className="">
                                        <Label htmlFor="new-env-key">Key</Label>
                                        <Input
                                            id="new-env-key"
                                            placeholder="NEW_VARIABLE"
                                            value={newEnvKey}
                                            onChange={(e) => setNewEnvKey(e.target.value)}
                                        />
                                    </div>
                                    <div className="">
                                        <Label htmlFor="new-env-value">Value</Label>
                                        <Input
                                            id="new-env-value"
                                            placeholder="value"
                                            value={newEnvValue}
                                            onChange={(e) => setNewEnvValue(e.target.value)}
                                        />
                                    </div>
                                </div>
                            </div>
                            <Button onClick={addEnvVar} variant="ghost">
                                <PlusCircle className="h-4 w-4" />
                            </Button>
                        </div>
                    </div>

                    <Separator />

                    <Button >
                        Save Changes
                    </Button>
                </CardContent>
            </Card>
        </TabsContent>
    );
}

export default EnvironmentSettingsTab;