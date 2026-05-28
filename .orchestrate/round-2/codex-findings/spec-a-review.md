# Spec A → main: Codex adversarial review

Branch under review: `task/multi-server-support-for-compozerr-plus--20260528-102504` (Spec A tip).
Diff vs `main`: 36 files, +1366/-40. Saved at `/tmp/specb-review/spec-a-vs-main.diff`.

## Findings

### 1. Auth/visibility correctness

- **[P2]** `backend/Api.Data/Repositories/ProjectRepository.cs:39`, `backend/Api/Endpoints/Projects/Project/Get/GetProjectRoute.cs:60`
  Project detail eager-loads `Project.Server.Location` and returns `project.Server.HostName`/location without `IServerVisibilityFilter`. Any non-admin user who can access that project can receive AdminOnly server identity.
  **Fix:** Apply `IServerVisibilityFilter` before projecting the server summary, or omit/mask the server field when not visible to the caller.

- **[P2]** `backend/Api.Data/Repositories/ProjectEnvironmentRepository.cs:41`, `backend/Api/Endpoints/Projects/Environments/Get/GetEnvironmentsCommandHandler.cs:35`
  Environment list joins through `Project.Server.Location` and returns `env.server` without `IServerVisibilityFilter`. Validator checks project ownership, not platform-admin visibility — AdminOnly server identity leaks to non-admin project members.
  **Fix:** Filter the projected server through `IServerVisibilityFilter` before returning `EnvironmentDto.Server`.

### 2. Allocator regression

- **[P2]** `modules/cli/backend/Cli/Endpoints/Projects/AllocateServer_ProjectCreatedEventHandler.cs:35`
  When no visible server exists for a non-admin, the allocator calls `.First()` on an empty filtered sequence — unhandled exception (500). Lowest-RAM heuristic itself is preserved.
  **Fix:** Use `FirstOrDefault()` and return a domain validation error with a "no visible servers available" message.

### 3. N+1 / eager-loading

No findings.

### 4. EF migration / schema drift

No findings.

### 5. Test coverage gaps

- **[P3]** `backend/Api.Tests/Endpoints/Server/AllocateServer_ProjectCreatedEventHandlerTests.cs:74`
  Missing gate (d): no allocator test for non-admin fallback when no Public server exists but a Private server does. Existing tests only cover Public/AdminOnly and RAM ordering.
  **Fix:** Add a test seeding only Private+AdminOnly candidates for a non-admin and assert Private is selected.

- **[P3]** `backend/Api.Tests/Endpoints/Server/AllocateServer_ProjectCreatedEventHandlerTests.cs:79`
  Missing gate (e): the AdminOnly-rejection test includes a Public fallback, so the "only AdminOnly servers exist for non-admin" error path is untested — exactly the path that throws unhandled on `.First()`.
  **Fix:** Add a test with only AdminOnly candidates for a non-admin and assert controlled failure (not 500).

### 6. Frontend type safety

- **[P3]** `frontend/src/generated/schema.ts:4998`, `backend/Api/Endpoints/Projects/Environments/Get/GetEnvironmentsResponse.cs:27`
  Generated client marks `server` as non-null even though backend DTO is `AllocatedServerSummary?`. UI defensively checks, but incorrect type misleads future consumers.
  **Fix:** Fix OpenAPI nullable emission so `server` is typed as `AllocatedServerSummary | null` in the generated schema.

## Verdict

**VERDICT: block**

The two P2 visibility leaks (§1) are the blockers — AdminOnly server identity (hostname, location) is readable by non-admins through both project detail and environments endpoints. The allocator's unhandled `.First()` on empty (§2) is a P2 crash path. Fix those three before merge.
