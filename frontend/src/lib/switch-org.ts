import { setCurrentOrgId } from "./org-storage"
import { queryClient } from "@/api-client"
import { router } from "@/router"

/**
 * Switch the active organization.
 *
 * Query keys are not scoped by org id (the org id is injected at request time
 * via the X-Organization-Id header in api-client.ts), so cached data from the
 * previous org would otherwise be served on navigation. resetQueries clears
 * cached data AND triggers a refetch on any active observers — removeQueries
 * only clears the cache and leaves mounted hooks stuck displaying stale data
 * until something else forces them to re-render (e.g. a manual page reload).
 */
export function switchOrg(orgId: string): void {
  setCurrentOrgId(orgId)
  void queryClient.resetQueries()
  void router.invalidate()
}
