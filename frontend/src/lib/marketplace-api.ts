import { apiBaseUrl } from '@/api-client'

const BASE_URL = apiBaseUrl || ''

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
  const searchParams = new URLSearchParams()
  if (params.query) searchParams.set('query', params.query)
  if (params.tags) params.tags.forEach((t) => searchParams.append('tags', t))
  if (params.limit) searchParams.set('limit', String(params.limit))
  if (params.offset) searchParams.set('offset', String(params.offset))

  const res = await fetch(
    `${BASE_URL}/v1/cli/modules/search?${searchParams}`,
    { credentials: 'include' },
  )
  if (!res.ok) {
    throw new Error(`Search failed: ${res.status}`)
  }
  return res.json()
}

export async function getModuleInfo(
  org: string,
  name: string,
): Promise<ModuleInfoResponse> {
  const res = await fetch(
    `${BASE_URL}/v1/cli/modules/info/${encodeURIComponent(org)}/${encodeURIComponent(name)}`,
    { credentials: 'include' },
  )
  if (!res.ok) {
    throw new Error(`Module info failed: ${res.status}`)
  }
  return res.json()
}
