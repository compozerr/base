import { setCurrentOrgId } from "./org-storage"
import { queryClient } from "@/api-client"

/** Switch the active organization. Persists to storage, dispatches event, and invalidates all queries. */
export function switchOrg(orgId: string): void {
  setCurrentOrgId(orgId)
  queryClient.invalidateQueries()
}
