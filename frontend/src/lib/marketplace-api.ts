import { api } from '@/api-client'

export interface SearchModuleItem {
  name: string
  description: string
  latestVersion: string
  repoUrl: string
  installCount: number
  organization: string
  tags: string[]
  iconUrl: string | null
}

export interface SearchModulesResponse {
  modules: SearchModuleItem[]
  total: number
}

export interface ModuleVersionDto {
  version: string
  commitHash: string
  releaseNotes: string | null
  createdAt: string
}

export interface ModuleInfoResponse {
  found: boolean
  name: string | null
  organization: string | null
  description: string | null
  latestVersion: string | null
  repoUrl: string | null
  tags: string[] | null
  versions: ModuleVersionDto[] | null
  installCount: number
  iconUrl: string | null
  compatibility: Record<string, string> | null
  publishedAt: string | null
}

export async function searchModules(params: {
  query?: string
  tags?: string[]
  limit?: number
  offset?: number
}): Promise<SearchModulesResponse> {
  const data = await api.v1.getCliModulesSearch.fetchQuery({
    parameters: {
      query: {
        Query: params.query,
        Tags: params.tags,
        Limit: params.limit ?? 20,
        Offset: params.offset ?? 0,
      },
    },
  })
  return data as unknown as SearchModulesResponse
}

export interface MyModuleDto {
  name: string
  repoUrl: string
}

export async function getMyModules(): Promise<MyModuleDto[]> {
  const data = await api.v1.getCliModulesMine.fetchQuery()
  return data as unknown as MyModuleDto[]
}

export async function getModuleInfo(
  org: string,
  name: string,
): Promise<ModuleInfoResponse> {
  const data = await api.v1.getCliModulesInfoOrganizationModuleName.fetchQuery({
    parameters: {
      path: { organization: org, moduleName: name },
    },
  })
  return data as unknown as ModuleInfoResponse
}
