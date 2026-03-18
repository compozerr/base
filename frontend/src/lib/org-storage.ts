export const ORG_STORAGE_KEY = "compozerr-current-org-id"

export function getCurrentOrgId(): string | null {
  return localStorage.getItem(ORG_STORAGE_KEY)
}

export function setCurrentOrgId(orgId: string): void {
  localStorage.setItem(ORG_STORAGE_KEY, orgId)
}

/** Fetch wrapper that includes credentials and X-Organization-Id header */
export function orgFetch(url: string, init?: RequestInit): Promise<Response> {
  const headers = new Headers(init?.headers)
  const orgId = getCurrentOrgId()
  if (orgId) headers.set("X-Organization-Id", orgId)
  return fetch(url, { ...init, credentials: "include", headers })
}
