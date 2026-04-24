import { setCurrentOrgId } from "./org-storage"
import { queryClient } from "@/api-client"
import { router } from "@/router"

/**
 * Switch the active organization.
 *
 * Query keys are not scoped by org id (the org id is injected at request time
 * via the X-Organization-Id header in api-client.ts), so cached data from the
 * previous org would otherwise be served on navigation. We must wipe the
 * query cache and invalidate the router so route loaders re-run.
 */
export function switchOrg(orgId: string): void {
  setCurrentOrgId(orgId)
  queryClient.removeQueries()
  void router.invalidate()
}
