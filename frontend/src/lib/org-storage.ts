export const ORG_STORAGE_KEY = "compozerr-current-org-id"

export function getCurrentOrgId(): string | null {
  return localStorage.getItem(ORG_STORAGE_KEY)
}

export function setCurrentOrgId(orgId: string): void {
  localStorage.setItem(ORG_STORAGE_KEY, orgId)
}
