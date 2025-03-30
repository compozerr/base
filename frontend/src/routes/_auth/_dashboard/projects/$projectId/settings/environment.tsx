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
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Separator } from '@/components/ui/separator'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { cn } from '@/lib/utils'
import { createFileRoute, Outlet, useRouter } from '@tanstack/react-router'
import { Info, PlusCircle, Trash2, Undo } from 'lucide-react'
import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react'

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

const SystemTypes = ["Frontend", "Backend"] as const;

type SystemType = (typeof SystemTypes)[number]

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

    const [selectedSystemType, setSelectedSystemType] = useState<SystemType>("Frontend");

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

    return (
        <TabsContent value="environment" className="space-y-4 mt-6">
            <Card>
                <CardHeader className='flex flex-row justify-between'>
                    <div className='flex flex-col'>

                        <CardTitle>Environment Variables</CardTitle>
                        <CardDescription>
                            Manage environment variables for your project.
                        </CardDescription>
                    </div>
                    <div className='w-1/4 justify-end flex pointer-events-auto'>
                        <Select
                            value={selectedBranch}
                            onValueChange={(value) =>
                                setSelectedBranch(value)
                            }
                        >
                            <SelectTrigger className="w-1/2" id="select-branch">
                                <SelectValue placeholder="Select branch" />
                            </SelectTrigger>
                            <SelectContent>
                                {env.map((i) => (
                                    <SelectItem
                                        key={i.branch}
                                        value={i.branch}
                                    >
                                        {i.branch}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>

                </CardHeader>
                <CardContent className="space-y-4">


                    <Tabs defaultValue={selectedSystemType} className="w-full">
                        <TabsList className="grid w-full grid-cols-2">
                            {
                                SystemTypes.map(x => (
                                    <TabsTrigger value={x} onClick={() => {
                                        setSelectedSystemType(x);
                                    }} className={systemTypeHasChanges(x) ? "italic" : ""} key={x}>
                                        {x}
                                    </TabsTrigger>
                                ))
                            }
                        </TabsList>

                        <Outlet />
                    </Tabs>

                    {/* Drag and drop area for .env file */}
                    <div
                        className={cn(
                            'border-2 border-dashed border-muted-foreground/25 rounded-lg p-6 text-center cursor-pointer hover:bg-muted/50 transition-colors',
                            isDragging && 'bg-muted/50',
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
                                <div className="rounded-full bg-primary/10 p-2">
                                    <PlusCircle className="h-6 w-6 text-primary" />
                                </div>
                                <h3 className="text-lg font-medium">Upload .env file</h3>
                                <p className="text-sm text-muted-foreground max-w-md mx-auto">
                                    Drag and drop your .env file here, or click to browse. We'll
                                    automatically parse and add the variables.
                                </p>
                            </div>
                        </label>
                    </div>

                    <Separator className="my-4" />

                    <div className="space-y-4">
                        {variablesToShow?.length ? variablesToShow.map((env, index) => (
                            <div key={index} className="flex items-center gap-4">
                                <div className="grid flex-1 gap-2">
                                    <div className="grid grid-cols-2 gap-4">
                                        <div>
                                            <Input
                                                id={`env-key-${index}`}
                                                className={
                                                    env.isDeleting ? 'line-through text-red-600' : ''
                                                }
                                                value={env.key}
                                                readOnly
                                                disabled={!env.isNew}
                                            />
                                        </div>
                                        <div>
                                            <Input
                                                id={`env-value-${index}`}
                                                className={
                                                    env.isDeleting ? 'line-through text-red-600' : ''
                                                }
                                                value={env.value}
                                                readOnly
                                                disabled={!env.isNew}
                                            />
                                        </div>
                                    </div>
                                </div>
                                {env.isGenerated ? (
                                    <div className="relative group p-2 mx-1">
                                        <Info className="h-4 w-4 text-muted-foreground" />
                                        <div className="absolute bottom-full right-full mb-2 hidden group-hover:block bg-secondary text-popover-foreground p-2 rounded shadow-lg text-sm w-[200px]">
                                            This is an auto-generated variable, it can be overriden by adding the same name with a different value.
                                        </div>
                                    </div>
                                ) : <Button
                                    variant="ghost"
                                    size="icon"
                                    className=""
                                    onClick={() => toggleRemoveEnvVar(index)}
                                >
                                    {env.isDeleting ? (
                                        <Undo className="h-4 w-4 " />
                                    ) : (
                                        <Trash2 className="h-4 w-4 text-destructive" />
                                    )}
                                </Button>}
                            </div>
                        )) : <div className='flex flex-row justify-center'>
                            <span className="italic">No variables here...</span>
                        </div>}
                    </div>

                    <Separator className="my-4" />

                    <form onSubmit={(e) => {
                        e.preventDefault();
                        addEnvVar();
                    }} className="space-y-4">
                        <h3 className="text-lg font-medium">Add New Variable</h3>
                        <div className="flex flex-row items-center gap-4">
                            <div className="grid flex-1 gap-2">
                                <div className="grid grid-cols-2 gap-4">
                                    <div className="">
                                        <Input
                                            id="new-env-key"
                                            ref={inputVariableRef}
                                            placeholder="NEW_VARIABLE"
                                            maxLength={255} 
                                            value={newEnvKey}
                                            onChange={(e) => setNewEnvKey(e.target.value)}
                                        />
                                    </div>
                                    <div className="">
                                        <Input
                                            id="new-env-value"
                                            placeholder="value"
                                            value={newEnvValue}
                                            maxLength={20 * 1024} // 20KB
                                            onChange={(e) => {
                                                // Additional check to ensure value doesn't exceed 1MB
                                                if (e.target.value.length <= 20 * 1024) {
                                                    setNewEnvValue(e.target.value)
                                                }
                                            }}
                                        />
                                    </div>
                                </div>
                            </div>
                            <Button type="submit" variant="ghost">
                                <PlusCircle className="h-4 w-4" />
                            </Button>
                        </div>
                    </form>

                    <Separator />

                    <LoadingButton isLoading={isSavingEnvChanges} onClick={
                        saveChanges
                    } disabled={!SystemTypes.some(x => systemTypeHasChanges(x))}
                    >Save Changes</LoadingButton>

                </CardContent>
            </Card>
        </TabsContent>
    )
}

export default EnvironmentSettingsTab
