# Subtask s-a1 — Backend: AdminOnly visibility + centralized filter + list endpoint

**Intent:** Add `ServerVisibility.AdminOnly`, centralize the visibility filter behind a single service, route every existing visibility read through it, expose `GET /v1/servers`, and cover the allocator handler with regression tests. Optionally rename `Server.ServerVisibility` → `Server.Visibility` if EF agrees it's a no-op. Do not regenerate the openapi-qraft client — that's S-A2.

**Suggested files:**
- `backend/Api.Data/Server.cs` (add `AdminOnly = 3`; optional property rename)
- `modules/cli/backend/Cli/Endpoints/Projects/AllocateServer_ProjectCreatedEventHandler.cs` (route via new filter; pass requesting user id)
- `backend/Api/Repositories/LocationRepository.cs` (use filter)
- `backend/Api/Repositories/ServerRepository.cs` (rename uses if applicable)
- `backend/Api/DevSeeder.cs` (rename uses if applicable; do NOT seed AdminOnly)
- NEW: `backend/Api/Services/ServerVisibility/IServerVisibilityFilter.cs` + impl
- NEW: `backend/Api/Endpoints/Server/GetServersRoute.cs` (+ DTO)
- NEW: `backend/Api.Tests/Endpoints/Server/AllocateServer_ProjectCreatedEventHandlerTests.cs`
- NEW: `backend/Api.Tests/Endpoints/Server/GetServersRouteTests.cs`
- NEW: `backend/Api.Tests/Services/ServerVisibility/ServerVisibilityFilterTests.cs`

**Acceptance criteria:**
- [ ] `dotnet test backend/` passes (full suite)
- [ ] `dotnet build backend/Base.sln` is warning-free relative to baseline
- [ ] `ServerVisibility` enum has `Unknown = 0, Public = 1, Private = 2, AdminOnly = 3` (numeric values preserved)
- [ ] Property rename done OR explicitly skipped with rationale captured in commit message (if EF dry-run produces a schema change, skip and proceed)
- [ ] Every `ServerVisibility ==` hit outside `Server.cs` & test fixtures goes through `IServerVisibilityFilter`
- [ ] `GET /v1/servers` returns visible servers for the current user; AdminOnly excluded for non-admins, included for platform admins
- [ ] Test: non-admin calling `GET /v1/servers` while an AdminOnly server exists → response does NOT include it
- [ ] Test: allocator does NOT pick AdminOnly for non-admin even when it would otherwise be the only candidate in the location
- [ ] Test: allocator DOES pick AdminOnly for platform admin when no Public alternative exists in their location
- [ ] Swagger at `http://localhost:1235/swagger/v1/swagger.json` shows the new endpoint after `compozerr dev`
- [ ] All commits on branch `task/s-a1`; final commit message references this subtask file

**Notes / gotchas:**
- `RequireRole("admin")` policy at `AuthFeature.cs:97` is the EXISTING platform-admin gate. Reuse it. Do NOT touch `IOrganizationAuthorizationService` — that's org-scoped and the wrong gate for server visibility.
- `ICurrentUserAccessor` exposes the current user. The allocator handler should call it directly rather than plumbing user-id through the event payload — confirm the handler executes inside an HTTP request scope (it runs from `ProjectCreatedEvent` which is fired during `CreateProjectCommand`, so HTTP context is alive).
- Do NOT regenerate the openapi-qraft client. Leaving the regen for S-A2 keeps the generated-code diff paired with its consumer.
- If a property rename produces an EF migration, revert just the rename (keep all other work) and add a `// TODO Spec B` note.
- High-risk single line (do not edit in this subtask, but be aware): `backend/Api.Data/InternalDomain.cs:12` reads `Project.Server.HostName`. It still works in Spec A — flagged for Spec B.
- Architecture review (`.orchestrate/round-1/skill-notes.md`) has the full slice context. Read it before starting.
