import { setCurrentOrgId } from "./org-storage"
import { api } from "@/api-client"

/** Switch the active organization. Persists to storage, dispatches event, and invalidates org-scoped queries. */
export function switchOrg(orgId: string): void {
  setCurrentOrgId(orgId)
  api.v1.getProjects.invalidateQueries()
}
