# Plan — Multi-server support (Spec A: AdminOnly visibility + dashboard display)

## Brief

> Multi-server support for Compozerr, plus admin-only server visibility.
>
> Today Server entity exists with a ServerVisibility enum (Public, Private, Unknown). Server allocation lives on Project (Project.ServerId) and is auto-assigned by an event handler based on location + RAM load. ProjectEnvironment inherits the project's server. Domain routing uses per-server Traefik instances writing dynamic.yml; DNS resolves to the server's IP.
>
> Wanted changes (one cohesive feature, but candidates for splitting into two specs A then B):
>
> **A) Server visibility + admin gating:**
>   1. Extend ServerVisibility enum with a third state (proposed name: AdminOnly).
>   2. Only platform admins can: (a) see AdminOnly servers in the server list, (b) be assigned to / create projects on an AdminOnly server. Need strong backend validation in the create-project / allocate-server path. Confirm whether 'admin' here means platform/system admin or org admin — the existing IOrganizationAuthorizationService.IsAdminAsync is org-scoped, which is probably the wrong gate for this. May need a platform-admin role/policy.
>   3. Show the allocated server on the project / environment dashboard as a read-only property.
>
> **B) Per-environment server allocation + picker + domain routing:** (out of scope for this run)
>
> User has indicated preference for shipping A first as its own PR, then B as a follow-up — but final decomposition is at the orchestrator's discretion after deep-thinking. Follow CLAUDE.md: regen the openapi-qraft client whenever endpoints change, use the generated api.v1.* client (no raw fetch), test in compozerr dev before claiming done.

(Full brief at `.orchestrate/round-1/brief.md`. Code-shape map and architectural notes at `.orchestrate/round-1/skill-notes.md`.)

## Scope decision

**This run delivers Spec A only.** Spec B (move `ServerId` from `Project` → `ProjectEnvironment` + per-env picker + per-env Traefik/DNS routing) is a separate `/orchestrate-task` run, recommended in the final REPORT.md. Rationale: the user explicitly preferred shipping A first as its own PR; Spec B is large (23+ scattered `project.ServerId` reads, schema migration, hosting-repo coordination, `InternalDomain.Value` rewrite) and warrants its own deep-think pass. Spec A is independently shippable and unblocks Spec B's terminology + central-visibility-filter foundations.

## Skills consulted

- **Matched & invoked:** `improve-codebase-architecture` — produced `skill-notes.md`. Findings folded into subtask scope (rename `ServerVisibility` → `Visibility`, centralize visibility filter, use existing platform-admin policy not org admin).
- **Rejected:**
  - `grill-with-docs` — no `CONTEXT.md` or `docs/adr/` exist in the repo; nothing to grill against.
  - `to-issues` — orchestrator IS the breakdown mechanism here.
  - `diagnose` — no bug to diagnose.
  - `tdd` — per-subtask concern; subtask specs require tests for the high-risk regression path (allocator handler).
  - `refactor-architecture` — too heavyweight; this is feature work, not a multi-round refactor loop.

## Assumptions (surfaced for approval)

1. **Platform admin == existing Identity role `"admin"`.** `RequireRole("admin")` already exists at `AuthFeature.cs:97`, used by `CreateNewServerRoute.cs:12` and `AdminSentEmailsGroup.cs:12`. Spec A reuses this. We do NOT introduce a new role string.
2. **`Server.ServerVisibility` property is safe to rename to `Visibility`.** It's a C# property over an int-mapped enum column; renaming the property does not alter the schema. Will verify via `dotnet ef migrations add --dry-run` early in S-A1 and revert with `[Column("ServerVisibility")]` if EF disagrees.
3. **No CLI wire-contract change yet.** `ProjectDto.ServerId` stays. Spec A does not move data; Spec B handles that.
4. **`AdminOnly` is the chosen enum-value name** (not "InviteOnly" or "PlatformOnly"). The user used both "invite only" and "admin only" in the brief and settled on the latter.
5. **"Allocated server" is the dashboard label** for the read-only display. Renaming "allocate" → "assign" deferred to Spec B (when the user-facing picker arrives).
6. **No hosting-repo (`/Users/milo/milodev/gits/hosting`) changes for Spec A.** The hosting layer doesn't read `ServerVisibility`; AdminOnly is purely a web-app concept.

## Subtasks

### S-A1 — Backend: AdminOnly visibility + centralized filter + list endpoint

**Intent:** Add `ServerVisibility.AdminOnly`, sharpen naming, centralize the visibility filter behind one service, route every existing visibility read through it, expose a new `GET /v1/servers` endpoint that returns the visible-to-current-user list, and cover the allocator handler with regression tests.

**Suggested files (read or edit):**
- `backend/Api.Data/Server.cs` (add enum value; rename property `ServerVisibility` → `Visibility`)
- `modules/cli/backend/Cli/Endpoints/Projects/AllocateServer_ProjectCreatedEventHandler.cs` (route via new filter; pass requesting user id)
- `modules/cli/backend/Cli/Endpoints/Projects/CreateProjectCommandHandler.cs` (or wherever the command is sent — pass user-id into the event so the allocator has it; alternatively read `CurrentUserAccessor.CurrentUserId` inside the handler)
- `backend/Api/Repositories/LocationRepository.cs` (route via filter)
- `backend/Api/Repositories/ServerRepository.cs` (any `Where(x => x.ServerVisibility == ...)` sites; rename uses)
- `backend/Api/DevSeeder.cs` (rename property usage; do NOT seed AdminOnly servers — Public stays the default)
- **NEW:** `backend/Api/Services/ServerVisibility/IServerVisibilityFilter.cs` + impl — single source of truth for "which servers can user X see/be-assigned-to". Signature roughly: `IQueryable<Server> ApplyTo(IQueryable<Server> query, UserId? requestingUserId)`.
- **NEW:** `backend/Api/Endpoints/Server/GetServersRoute.cs` (+ DTO) — returns visible servers; uses filter service.
- **NEW:** `backend/Api.Tests/Endpoints/Server/AllocateServer_ProjectCreatedEventHandlerTests.cs` — regression coverage on allocator.
- **NEW:** `backend/Api.Tests/Endpoints/Server/GetServersRouteTests.cs` — assert non-admin cannot see AdminOnly servers.
- **NEW:** `backend/Api.Tests/Services/ServerVisibility/ServerVisibilityFilterTests.cs` — covers the filter rules in isolation.

**Acceptance criteria:**
- [ ] `dotnet test backend/` passes (full test run, not just the new tests).
- [ ] `dotnet build backend/Base.sln` is warning-free (or no new warnings).
- [ ] `ServerVisibility` enum has values `Unknown = 0, Public = 1, Private = 2, AdminOnly = 3` (numeric values preserved for existing rows).
- [ ] `Server.Visibility` property is renamed (or kept as `ServerVisibility` if EF migration check rejects rename — note the decision in the subtask handoff).
- [ ] Every grep hit for `ServerVisibility ==` outside `Api.Data/Server.cs` is replaced by a call to the new visibility filter, OR is justified inline (e.g. test fixture seeding a Public server).
- [ ] `GET /v1/servers` returns the user's visible servers; AdminOnly excluded for non-admins. Admins see all visibilities.
- [ ] Regression test: a non-platform-admin user requesting `GET /v1/servers` while the DB contains an AdminOnly server gets back a list without that server.
- [ ] Regression test: `AllocateServer_ProjectCreatedEventHandler` does NOT pick an AdminOnly server for a non-admin user even when it would otherwise be the only candidate in that location.
- [ ] Regression test: `AllocateServer_ProjectCreatedEventHandler` DOES pick an AdminOnly server for a platform admin when no Public alternative exists in their location.
- [ ] Swagger doc at `http://localhost:1235/swagger/v1/swagger.json` includes the new endpoint after running `compozerr dev`.
- [ ] All commits live on this branch (`task/multi-server-support-for-compozerr-plus--20260528-102504`); cmux subtask branch is `task/s-a1`.

**Notes / gotchas:**
- `RequireRole("admin")` policy already exists at `AuthFeature.cs:97`. Reuse it for the admin-only branch of the endpoint and for the platform-admin check inside the filter. Do NOT touch `IOrganizationAuthorizationService` — that's org-scoped and the wrong gate.
- When passing user-id into the allocator handler, prefer `ICurrentUserAccessor.CurrentUserId` inside the handler over plumbing it through the event payload (less surface area). If the handler runs outside an HTTP context (e.g. background job), confirm that's not the case for `ProjectCreatedEvent` — the prior architecture review didn't see one.
- Do NOT regenerate the openapi-qraft client in this subtask — leave that for S-A2 so the regen is paired with the frontend code that consumes the new types (avoids two separate "regen + nothing uses it" commits).
- If the rename `ServerVisibility` → `Visibility` triggers an EF migration, REVERT the rename and add `// TODO Spec B: rename via explicit migration` rather than authoring a migration here. Property rename is a polish; the central filter is the load-bearing change.

### S-A2 — Frontend: regen API client + show allocated server on dashboards

**Intent:** Regenerate the openapi-qraft client to pick up the new `GET /v1/servers` endpoint, then surface the allocated server on the project page and on each environment row/page as a read-only field. No picker, no edit — display only.

**Suggested files (read or edit):**
- `frontend/src/generated/` (regenerated wholesale — do not hand-edit)
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/index.tsx` (project page — surface `Server: <name> (<location>)`)
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/environments/index.tsx` (env list — surface server per env row; for Spec A all envs of a project share the project's server, so it'll show the same value everywhere, but the wiring must read from the env if the API exposes it — otherwise from the project)
- Any "servers list" admin page (only if one already exists; otherwise skip — Spec B will introduce the picker UI)

**Acceptance criteria:**
- [ ] `cd frontend && npm run typecheck` passes.
- [ ] `cd frontend && npm run build` succeeds.
- [ ] Generated client (`frontend/src/generated/`) exposes a hook for the new endpoint (e.g. `api.v1.getServers.useQuery()`).
- [ ] All API calls use the generated `api.v1.*` client — NO raw `fetch()` / `useQuery()` / `useMutation()` / `orgFetch()`.
- [ ] Project detail page renders the allocated server name (and location if available) as a read-only field. If `project.serverId` is null, render `"Not yet allocated"` or equivalent — do not crash.
- [ ] Each environment row on the environments page renders its allocated server. For Spec A, all envs of a project share the project's server — the UI must still pull the value from the data shape and not hardcode `project.server`.
- [ ] User has run `compozerr dev` and visually verified both surfaces show a server name in the browser before claiming done.
- [ ] All commits live on this branch; cmux subtask branch is `task/s-a2`.

**Notes / gotchas:**
- This subtask depends on S-A1 being merged into the run branch. Start it AFTER the orchestrator confirms S-A1 merged.
- Regenerate the client via the exact command in CLAUDE.md (port `1235`, NOT `5000`): `npx openapi-qraft --plugin tanstack-query-react --plugin openapi-typescript http://localhost:1235/swagger/v1/swagger.json --clean -o src/generated --export-type --properties-required-by-default --default-non-nullable`. Backend must be up via `compozerr dev` first.
- After regen, the diff inside `frontend/src/generated/` should be confined to the new endpoint + its DTO. Any unexpected churn in unrelated endpoints means a backend route signature drifted — investigate before committing.
- Do NOT add a server picker — that's Spec B. If you find yourself wanting to add a `<select>` for server choice, STOP; the read-only display is Spec A's hard ceiling.

## Groups

- **Group G1 (sequential):** S-A1 → S-A2.
  S-A2 cannot start until S-A1 merges, because S-A2 regenerates the API client from S-A1's new endpoint. The orchestrator runs G1 alone (no parallel siblings) and respects the dependency by waiting for S-A1's `subtask-done-s-a1` signal + merge before spawning S-A2.

No parallelism this run — Spec A is too small to benefit, and the dependency makes inter-task overlap unsafe.

## Risks (≥3)

1. **Renaming `Server.ServerVisibility` → `Server.Visibility` triggers an unintended EF migration or breaks DTOs.** Mitigation: subtask must dry-run `dotnet ef migrations add --no-build` and inspect for column-name changes before committing the rename; if it produces a migration, revert the rename (keep the field as-is, only add the new enum value) and proceed without the polish. Acceptance criteria explicitly allows this fallback.

2. **AdminOnly servers leak via a read site we don't migrate.** The architecture review found 4+ scattered `ServerVisibility ==` sites today; new ones can be added in parallel work outside this run. Mitigation: S-A1 introduces a *single* `IServerVisibilityFilter` and routes EVERY current site through it; the subtask includes a grep step in its acceptance to ensure no stray comparisons remain. Test: integration test creates an AdminOnly server, hits `GET /v1/servers` as non-admin, asserts absence.

3. **Allocator handler silently picks an AdminOnly server for a non-admin user.** Today the handler filters `Public`; broadening to "any visible server" without the user-context filter would regress. Mitigation: regression tests are first-class acceptance criteria (both "non-admin must NOT get AdminOnly" and "admin MAY get AdminOnly when no Public is available"). Test fixture seeds a location with only an AdminOnly server and asserts the non-admin path falls back / fails gracefully.

4. **"Platform admin" gate is checked in the wrong service** (e.g. accidentally wired to `IOrganizationAuthorizationService.IsAdminAsync`, which is org-scoped). Mitigation: subtask spec explicitly names the existing `RequireRole("admin")` policy at `AuthFeature.cs:97` as the sole source of truth; subtask must verify that policy's definition before authoring the filter. Reviewer (Phase 5 hook) should flag any reference to `IOrganizationAuthorizationService` introduced in this run.

5. **Frontend regen surfaces unrelated openapi drift** (a backend endpoint changed signature between the last regen and now, producing a giant generated diff that's hard to review). Mitigation: S-A2 first commits the generated diff in isolation (one commit just for `frontend/src/generated/`) so reviewers can `git diff main -- frontend/src/generated/` cleanly; the subsequent commit adds the consumer code. If the regen diff is huge, the subtask flags it as STUCK rather than burying it in a feature PR.

## Estimated wall time

- S-A1: ~45–75 min in a single Claude session (enum + filter + endpoint + 3 tests; mostly mechanical).
- S-A2: ~30–45 min (regen + two UI surfaces, no design work).
- Plus orchestrator merge-back overhead (~5 min/subtask).

**Total: ~1.5–2.5 hours wall time, sequential.**

## Decision points the user should confirm before approval

1. **Scope split (Spec A only this run, Spec B as a follow-up `/orchestrate-task`).** If you want B included, hit `n` and re-brief with both A and B scoped — the deep-think would re-decompose with the migration + 23 call-site sweep + hosting-coordination on the critical path.
2. **`AdminOnly` as the chosen enum value name** (vs `PlatformOnly` or `InviteOnly`). Architecture review preferred `PlatformOnly` to keep terminology consistent with the existing `RequireRole("admin")` (platform-admin) gate; I went with `AdminOnly` per your latest message. Say so at approval if you want the rename.
3. **Property rename `Server.ServerVisibility` → `Server.Visibility`** (no schema change expected, but adds noise). Acceptable to skip if you prefer minimal-diff PRs — say so and I'll instruct S-A1 to skip the rename.
