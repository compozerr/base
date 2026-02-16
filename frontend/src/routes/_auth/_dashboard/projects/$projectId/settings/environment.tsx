import { api } from '@/api-client'
import LoadingButton from '@/components/loading-button'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
    Card,
    CardContent,
    CardDescription,
    CardFooter,
    CardHeader,
    CardTitle,
} from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { cn } from '@/lib/utils'
import { createFileRoute, useRouter } from '@tanstack/react-router'
import { FileText, Info, PlusCircle, Trash2, Undo, Upload, GitBranch } from 'lucide-react'
import React, { Fragment, useCallback, useEffect, useRef, useState } from 'react'
import { SystemType } from '../../../../../../lib/system-type'

export const Route = createFileRoute(
    '/_auth/_dashboard/projects/$projectId/settings/environment',
)({
    component: EnvironmentSettingsTab,
    loader: ({ params: { projectId } }) => {
        return api.v1.getProjectsProjectIdEnvironment({
            parameters: {
                path: {
                    projectId
                },
                query: {
                    branch: "main"
                }
            }
        })
    },
})

type EnvironmentVar = {
    systemType: SystemType
    key: string
    value: string
    isNew: boolean
    isDeleting: boolean
    isGenerated: boolean
}

type Environment = {
    branch: string,
    variables: EnvironmentVar[]
}

function EnvironmentSettingsTab() {
    const { data, error } = Route.useLoaderData();
    const { projectId } = Route.useParams();

    const { invalidate } = useRouter();

    const [selectedBranch, setSelectedBranch] = useState("main");

    const { mutate } = api.v1.putProjectsProjectIdEnvironment.useMutation({
        path: {
            projectId
        },
        query: {
            branch: selectedBranch
        }
    })

    const [isSavingEnvChanges, setIsSavingEnvChanges] = useState(false)

    const [env, setEnv] = useState<Environment[]>([])

    // Get configuration from API response
    const config = data?.configuration
    const availableSystemTypes = (config?.allowedSystemTypes ?? ["Frontend", "Backend"]) as SystemType[]
    const defaultSystemType = (config?.defaultSystemType ?? "Frontend") as SystemType
    const environmentFiles = config?.environmentFiles ?? []

    useEffect(() => {
        const env: Environment[] = [{
            branch: selectedBranch,
            variables: data?.variables?.map((item) => ({
                systemType: item.systemType as SystemType,
                key: item.key!,
                value: item.value!,
                isNew: false,
                isDeleting: false,
                isGenerated: !!item.isGenerated
            })).sort(x => x.isGenerated ? -1 : 1) || []
        }]

        setEnv(env)
    }, [data, selectedBranch]);

    const saveChanges = () => {
        setIsSavingEnvChanges(true);
        mutate({
            variables: env.find(x => x.branch === selectedBranch)?.variables.filter(x => !x.isGenerated && !x.isDeleting) || []
        }, {
            onSuccess: () => {
                invalidate();
            },
            onSettled: () => {
                setIsSavingEnvChanges(false);
            }
        });
    }

    const [newEnvKey, setNewEnvKey] = useState('')
    const [newEnvValue, setNewEnvValue] = useState('')
    const [isDragging, setIsDragging] = useState(false)
    const fileInputRef = useRef<HTMLInputElement>(null)

    // Initialize with the default system type from configuration
    const [selectedSystemType, setSelectedSystemType] = useState<SystemType>(defaultSystemType);

    const inputVariableRef = useRef<HTMLInputElement>(null);

    const addEnvVar = () => {
        const environmentVariables = env.find((env) => env.branch === selectedBranch)?.variables

        if (!environmentVariables) return

        // Check if the environment variable already exists
        const existingVar = environmentVariables.find((env) => env.key === newEnvKey)
        if (existingVar) {
            existingVar.value = newEnvValue
            existingVar.isGenerated = false
            existingVar.isNew = true
        } else {
            environmentVariables.push({
                systemType: selectedSystemType,
                key: newEnvKey,
                value: newEnvValue,
                isNew: true,
                isDeleting: false,
                isGenerated: false,
            })
        }
        setNewEnvKey('')
        setNewEnvValue('')
        setEnv([...env])

        inputVariableRef.current?.focus();
    }

    const systemTypeHasChanges = useCallback((systemType: SystemType) => {
        const environment = env.find(e => e.branch === selectedBranch)
        if (!environment) return false

        const variables = environment.variables.filter(x => x.systemType === systemType)
        return variables.some(x => x.isNew || x.isDeleting)
    }, [env]);

    const toggleRemoveEnvVar = (varIndex: number) => {
        const newEnvList = [...env]
        const environment = newEnvList.find(e => e.branch === selectedBranch)
        if (!environment) return

        const variables = environment.variables.filter(x => x.systemType === selectedSystemType)
        if (variables[varIndex]?.isNew) {
            variables.splice(varIndex, 1)
        } else {
            variables[varIndex] = {
                ...variables[varIndex]!,
                isDeleting: !variables[varIndex]!.isDeleting,
            }
        }

        environment.variables = [
            ...environment.variables.filter(x => x.systemType !== selectedSystemType),
            ...variables
        ]

        setEnv(newEnvList)
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
            if (file?.name.endsWith('.env')) {
                handleEnvFile(file)
            }
        }
    }

    const handleEnvFile = (file: File) => {
        const reader = new FileReader()
        reader.onload = (e) => {
            const content = e.target?.result as string
            if (content) {
                const lines = content.split('\n')
                const newVars: EnvironmentVar[] = []

                lines.forEach((line) => {
                    // Skip comments and empty lines
                    if (line.trim() && !line.startsWith('#')) {
                        const parts = line.split('=')
                        if (parts.length >= 2) {
                            const key = parts[0]?.trim()
                            const value = parts.slice(1).join('=').trim()
                            if (!key) return
                            newVars.push({ systemType: selectedSystemType, key, value, isNew: true, isDeleting: false, isGenerated: false })
                        }
                    }
                })

                if (newVars.length > 0) {
                    const environment = env.find((env) => env.branch === selectedBranch)
                    if (!environment) return;

                    environment.variables.push(...newVars)
                    setEnv([...env])
                }
            }
        }
        reader.readAsText(file)
    }

    const variablesToShow = env.find(x => x.branch === selectedBranch)?.variables.filter(x => x.systemType === selectedSystemType);
    const hasAnyChanges = availableSystemTypes.some(x => systemTypeHasChanges(x));

    return (
        <div className="space-y-6">
            <Card className="rounded-xl border-zinc-800 bg-zinc-900/50">
                <CardHeader className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
                    <div>
                        <CardTitle className="text-lg">Environment Variables</CardTitle>
                        <CardDescription>
                            Manage environment variables for your project.
                        </CardDescription>
                    </div>
                    <Select
                        value={selectedBranch}
                        onValueChange={(value) => setSelectedBranch(value)}
                    >
                        <SelectTrigger className="w-full sm:w-40" id="select-branch">
                            <GitBranch className="h-4 w-4 mr-2 text-muted-foreground" />
                            <SelectValue placeholder="Select branch" />
                        </SelectTrigger>
                        <SelectContent>
                            {env.map((i) => (
                                <SelectItem key={i.branch} value={i.branch}>
                                    <span className="font-mono text-sm">{i.branch}</span>
                                </SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </CardHeader>
                <CardContent className="space-y-6">
                    {/* Info alert */}
                    <Alert className="border-zinc-800 bg-zinc-900/30">
                        <FileText className="h-4 w-4" />
                        <AlertDescription className="text-sm">
                            Environment variables are stored in{' '}
                            {environmentFiles.map((file, index) => (
                                <Fragment key={file.filePath}>
                                    <code className="font-mono text-xs bg-zinc-800 px-1.5 py-0.5 rounded">{file.filePath}</code>
                                    {index < environmentFiles.length - 1 && ' and '}
                                </Fragment>
                            ))}
                            {environmentFiles.length === 0 && <code className="font-mono text-xs bg-zinc-800 px-1.5 py-0.5 rounded">.env</code>}
                        </AlertDescription>
                    </Alert>

                    {/* System type tabs */}
                    {availableSystemTypes.length > 1 && (
                        <div className="flex items-center gap-1 p-1 rounded-lg bg-zinc-900/50 border border-zinc-800 w-fit">
                            {availableSystemTypes.map(x => (
                                <button
                                    key={x}
                                    type="button"
                                    onClick={() => setSelectedSystemType(x)}
                                    className={cn(
                                        'relative px-4 py-1.5 text-sm font-medium rounded-md transition-all',
                                        selectedSystemType === x
                                            ? 'bg-zinc-800 text-foreground shadow-sm'
                                            : 'text-muted-foreground hover:text-foreground',
                                    )}
                                >
                                    {x}
                                    {systemTypeHasChanges(x) && (
                                        <span className="absolute top-1 right-1 w-1.5 h-1.5 rounded-full bg-amber-500" />
                                    )}
                                </button>
                            ))}
                        </div>
                    )}

                    {/* Drag and drop area */}
                    <div
                        className={cn(
                            'rounded-lg border-2 border-dashed p-8 text-center cursor-pointer transition-all',
                            isDragging
                                ? 'border-primary/50 bg-primary/5'
                                : 'border-zinc-800 hover:border-zinc-700 hover:bg-zinc-900/30',
                        )}
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
                                    if (file?.name.endsWith('.env')) {
                                        handleEnvFile(file)
                                    }
                                }
                            }}
                            ref={fileInputRef}
                        />
                        <label htmlFor="env-file-upload" className="cursor-pointer">
                            <div className="flex flex-col items-center gap-2">
                                <div className="rounded-full bg-zinc-800 p-3">
                                    <Upload className="h-5 w-5 text-muted-foreground" />
                                </div>
                                <p className="text-sm font-medium">Upload .env file</p>
                                <p className="text-xs text-muted-foreground max-w-sm">
                                    Drag and drop your .env file here, or click to browse.
                                    Variables will be automatically parsed and added.
                                </p>
                            </div>
                        </label>
                    </div>

                    {/* Variable list */}
                    <div className="space-y-2">
                        {variablesToShow?.length ? (
                            <div className="rounded-lg border border-zinc-800 overflow-hidden divide-y divide-zinc-800/50">
                                {variablesToShow.map((envVar, index) => (
                                    <div
                                        key={index}
                                        className={cn(
                                            'flex items-center gap-3 px-4 py-3 transition-colors',
                                            envVar.isDeleting && 'bg-red-500/5',
                                            envVar.isNew && !envVar.isDeleting && 'bg-green-500/5',
                                        )}
                                    >
                                        <div className="flex-1 min-w-0 grid grid-cols-1 sm:grid-cols-2 gap-2">
                                            <span className={cn(
                                                'font-mono text-sm truncate',
                                                envVar.isDeleting ? 'line-through text-red-500/70' : 'text-muted-foreground',
                                            )}>
                                                {envVar.key}
                                            </span>
                                            <span className={cn(
                                                'font-mono text-sm truncate',
                                                envVar.isDeleting ? 'line-through text-red-500/70' : 'text-foreground',
                                            )}>
                                                {envVar.value}
                                            </span>
                                        </div>
                                        <div className="flex items-center gap-2 flex-shrink-0">
                                            {envVar.isGenerated ? (
                                                <Badge variant="outline" className="text-xs border-zinc-700 text-muted-foreground">
                                                    <Info className="h-3 w-3 mr-1" />
                                                    auto
                                                </Badge>
                                            ) : (
                                                <Button
                                                    variant="ghost"
                                                    size="icon"
                                                    className="h-8 w-8"
                                                    onClick={() => toggleRemoveEnvVar(index)}
                                                >
                                                    {envVar.isDeleting ? (
                                                        <Undo className="h-4 w-4 text-muted-foreground" />
                                                    ) : (
                                                        <Trash2 className="h-4 w-4 text-destructive" />
                                                    )}
                                                </Button>
                                            )}
                                        </div>
                                    </div>
                                ))}
                            </div>
                        ) : (
                            <div className="flex items-center justify-center py-8 text-sm text-muted-foreground">
                                No variables configured for this system type.
                            </div>
                        )}
                    </div>

                    {/* Add new variable */}
                    <form onSubmit={(e) => {
                        e.preventDefault();
                        addEnvVar();
                    }}>
                        <div className="rounded-lg border border-dashed border-zinc-800 p-4">
                            <h4 className="text-sm font-medium mb-3">Add New Variable</h4>
                            <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3">
                                <Input
                                    id="new-env-key"
                                    ref={inputVariableRef}
                                    placeholder="VARIABLE_NAME"
                                    maxLength={255}
                                    value={newEnvKey}
                                    onChange={(e) => setNewEnvKey(e.target.value)}
                                    className="font-mono text-sm flex-1"
                                />
                                <Input
                                    id="new-env-value"
                                    placeholder="value"
                                    value={newEnvValue}
                                    maxLength={20 * 1024}
                                    onChange={(e) => {
                                        if (e.target.value.length <= 20 * 1024) {
                                            setNewEnvValue(e.target.value)
                                        }
                                    }}
                                    className="font-mono text-sm flex-1"
                                />
                                <Button type="submit" variant="outline" size="icon" className="h-10 w-10 flex-shrink-0">
                                    <PlusCircle className="h-4 w-4" />
                                </Button>
                            </div>
                        </div>
                    </form>
                </CardContent>
                <CardFooter className="border-t border-zinc-800 pt-6">
                    <LoadingButton
                        isLoading={isSavingEnvChanges}
                        onClick={saveChanges}
                        disabled={!hasAnyChanges}
                        className="w-full sm:w-auto"
                    >
                        Save Changes
                    </LoadingButton>
                </CardFooter>
            </Card>
        </div>
    )
}

export default EnvironmentSettingsTab
