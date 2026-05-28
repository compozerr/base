# Spec B — per-environment server allocation: round 1 plan

## Brief

> Spec B — per-environment server allocation (Compozerr multi-server). See the condensed brief in the 'Spec B handoff' section of /Users/milo/milodev/gits/web/.worktrees/multi-server-support-for-compozerr-plus--20260528-102504/.orchestrate/REPORT.md — read that file first for the full brief.

Spec B handoff (from prior REPORT.md, verbatim):

> Move `ServerId` from `Project` → `ProjectEnvironment`. Add server-picker step to create-project + create-environment flows (CLI + web). Ensure per-env Traefik/DNS routing follows the env's server. Foundations from Spec A (already merged): `IServerVisibilityFilter`, `GET /v1/servers`, `AllocatedServerSummary` DTO, env-side `server` field on `GetEnvironmentsResponse`. Known hazards to address explicitly:
>
> - `backend/Api.Data/InternalDomain.cs:12` reads `Project.Server.HostName` — rewire to `Environment.Server.HostName` (highest-risk single line).
> - 23+ scattered `project.ServerId` / `project.Server` reads to fan out; `ProjectEnvironmentHealthCheckJob.cs:21-24` groups by `Project.ServerId` and is broken-by-construction post-migration.
> - `UpdateDomains_DomainChangeEventHandler.cs:19` pushes domain config to `domainProject.ServerId` — must fan out per-env.
> - Every `hostingApiFactory.GetHostingApiAsync(project.ServerId)` call site must route via env.
> - `ProjectDto.ServerId` is on the CLI wire contract — version or keep a derived field.
> - Hosting repo (`/Users/milo/milodev/gits/hosting`) `HostingEnvironmentsResponse` does not currently carry per-env server identity — add it.
> - Cli.Tests/ is empty; `CreateEnvironmentCommandHandler` has no test. Build coverage during the migration.
> - Reconsider whether `ServerVisibility` property rename to `Visibility` is worth doing now (was skipped in Spec A to minimize diff).

## Spec A foundations: present (post-rebase)

At approval gate the user chose **"Rebase Spec B on Spec A's branch"**. The run branch was reset to the tip of `task/multi-server-support-for-compozerr-plus--20260528-102504`, so Spec A's foundations are now in the working tree:

- `IServerVisibilityFilter` — **present** (`backend/Api.Data/Services/IServerVisibilityFilter.cs` + impl).
- `AllocatedServerSummary` DTO — **present** (`backend/Api.Abstractions/AllocatedServerSummary.cs`).
- `GET /v1/servers` endpoint — **present** (`backend/Api/Endpoints/Server/GetServersRoute.cs`).
- Env-side `server` field on `GetEnvironmentsResponse` — **present**.
- `ServerVisibility.AdminOnly` enum value — **present**.

This **simplifies s-b3** — the picker UI just consumes the existing `GET /v1/servers` (already AdminOnly-aware); env-row server display is already wired in the frontend (just sourced from `project.server` today via the `Server` DTO field; s-b3 changes that to `env.server`). The `AllocatedServerSummary` shape is the wire type for env-server summaries.

This run's PR will be a follow-up to Spec A's PR — both branches must be merged in order (`task/multi-server-…-102504` first, then this run's branch). Run REPORT.md will document that.

## Skills consulted

**Matched & executed**

- `improve-codebase-architecture` (non-grilling variant) — invoked as Agent subagent. Output captured at `.orchestrate/round-1/skill-notes.md`. Identified 31 verified call sites that assume one-server-per-project, recommended a new `IEnvironmentTargetResolver` seam as the deepening, and proposed a 4-subtask decomposition. Used directly in the plan below.

**Rejected**

- `grill-with-docs` — there is no `CONTEXT.md` and no `docs/adr/`; the skill's grilling input is missing.
- `to-issues` — the orchestrator IS the work-breakdown mechanism.
- `diagnose` — not a bug; planned change.
- `tdd` — per-subtask concern (each subtask's acceptance criteria already require tests where coverage is missing).
- `refactor-architecture` — that skill IS a multi-round orchestrator; running it here would nest orchestrators.
- `triage`, `to-prd`, `grill-me`, `find-skills`, `caveman`, `handoff`, `zoom-out`, `write-a-skill`, `app-store-screenshots`, `marketing-psychology`, `microsoft-foundry`, `setup-matt-pocock-skills` — not relevant.

## Assumptions

A1. **Scope expanded to /web + /cli + /hosting** (per user approval). Children spawned for s-b3 may edit `/Users/milo/milodev/gits/cli` and `/Users/milo/milodev/gits/hosting` in addition to `/Users/milo/milodev/gits/web`. **Caveat**: the orchestrate-task framework only manages a worktree/branch for the run's primary repo (`/web`). Cross-repo work in `/cli` and `/hosting` will be made on each of those repos' current branches and **committed but not merged**; their commits are surfaced in REPORT.md as "ready-to-PR" follow-ups the user owns. The child session must `git status` and commit each repo separately; do not push.

A2. **Schema strategy: option B (keep `Project.ServerId` as a nullable hint).** Per skill notes §4, keep `Project.ServerId` after the migration as a deprecated default-server hint. `ProjectEnvironment.ServerId` becomes the load-bearing FK. Wire-compat win on CLI (downgrade-safe). Drop `Project.ServerId` in a follow-up after CLI version bump.

A3. **Allocator moves to env creation.** `AllocateServer_ProjectCreatedEventHandler` continues to allocate for the auto-created production env (and writes both `Project.ServerId` hint + `ProjectEnvironment.ServerId`). `CreateEnvironmentCommandHandler` accepts an optional `ServerId?`; if null, falls back to the same allocator. Allocation stays synchronous, ordered after Stripe subscription creation (preserve commit `bbc17ed0` ordering).

A4. **Apex/project-level domain fallback.** When `Domain.ProjectEnvironmentId` is null, server resolution falls back to the project's Production env's server. Encoded as a single helper to avoid the rule leaking.

A5. **`ServerVisibility` → `Visibility` rename: deferred.** Only worthwhile bundled with Spec A's `AdminOnly`; defer to a follow-up.

A6. **CLI repo + hosting repo changes ARE in this run** (in s-b3) per user approval. Children make those changes on the current default branch of each external repo, commit locally, and leave them un-pushed for the user to PR.

A7. **Run branch is rebased onto Spec A tip.** Spec A's audit trail moved to `.orchestrate/spec-a-round-1/` and `.orchestrate/spec-a-REPORT.md`. The Spec A commits remain in this branch's history.

## Subtasks

### s-b1 — Schema + backfill + allocator move (sequential, first)

**Intent**: Add `ProjectEnvironment.ServerId` as nullable FK, backfill from `Project.ServerId`, move allocation to env-level. Add tests for `CreateEnvironmentCommandHandler` (which has none today).

**Suggested files**:

- `backend/Api.Data/ProjectEnvironment.cs` (add `ServerId?` + nav)
- `backend/Api.Data/ApiDbContext.cs` (configure FK, index)
- new EF migration under `backend/Api.Data/Migrations/`
- `modules/cli/backend/Cli/Endpoints/Projects/AllocateServer_ProjectCreatedEventHandler.cs` (also write the new prod env's `ServerId`; keep writing `Project.ServerId` as hint)
- `backend/Api/Endpoints/Projects/Environments/Create/CreateEnvironmentCommand.cs` + Validator + Handler (accept optional `ServerId`, allocator fallback when null)
- new `modules/cli/backend/Cli.Tests/Features/Projects/CreateEnvironmentCommandHandlerTests.cs` (R5 mitigation)

**Acceptance criteria**:

- `dotnet build backend/Base.sln` clean.
- `dotnet test backend/Api.Tests/Api.Tests.csproj` and `dotnet test modules/cli/backend/Cli.Tests/Cli.Tests.csproj` green.
- New tests cover: (a) explicit ServerId honored; (b) allocator fallback when ServerId omitted; (c) Stripe subscription compensation on env-creation failure still works.
- DB integration check: seeded DB with N projects results in every `ProjectEnvironment` having non-null `ServerId` after migration.
- `Project.ServerId` is **still written** by the allocator (compat hint).

**Notes**: `ProjectEnvironment.ServerId` is **nullable** in the schema (option B) — required (non-null) constraint would block the legacy/backfill path. Add an integrity-check note for prod cutover.

### s-b2 — IEnvironmentTargetResolver seam + read-site fanout (sequential, after s-b1)

**Intent**: Introduce a single `IEnvironmentTargetResolver` interface that owns "given a ProjectEnvironmentId, give me the hosting API / Server". Re-route every non-allocator read site enumerated in skill-notes §2.A + §2.B + §2.C through it. Fix the highest-risk single line at `InternalDomain.cs:12`.

**Suggested files** (per skill-notes §2; full enumeration there):

- new `backend/Api.Hosting/EnvironmentTargetResolver/IEnvironmentTargetResolver.cs` + impl
- `backend/Api.Data/InternalDomain.cs:12` (env's HostName, with production-env fallback for apex)
- `backend/Api.Hosting/Jobs/DeployProjectJob.cs:34`
- `backend/Api.Hosting/VMPooling/Core/BaseVMPooler.cs:69`
- `backend/Api.Hosting/EventHandlers/UpdateDomains_DomainChangeEventHandler.cs:19` (fan-out: every distinct env-server)
- `backend/Api.Hosting/Jobs/ProjectEnvironmentHealthCheckJob.cs:21-24` (group by env's ServerId)
- `backend/Api/Endpoints/Projects/Project/{Start,Stop,Delete,ExecuteCommand}/...CommandHandler.cs`
- `backend/Api/Endpoints/Projects/Environments/Delete/DeleteEnvironmentCommandHandler.cs:45,47`
- `backend/Api/Endpoints/Projects/Services/{ChangeProtocol,ExposeTcp,UnexposeTcp}/...CommandHandler.cs`
- `backend/Api/Endpoints/Projects/Environments/Services/GetServiceLogs/GetServiceLogsCommandHandler.cs`
- `backend/Api/Endpoints/Projects/ProjectsGroup.cs` (debug terminal)
- `backend/Api/Endpoints/Projects/Domains/{Parent/Get,Verify}/*` + `backend/Api/Jobs/DomainVerificationJob.cs` (A-record verification fanout per skill-notes R2)
- `modules/terminal/backend/Terminal/TerminalHub.cs:82,90,110,128`
- `backend/Api/Endpoints/Projects/Environments/Services/GetEnvironmentServicesCommandHandler.cs`
- `backend/Api/Endpoints/Projects/Services/Get/GetProjectServicesCommandHandler.cs`
- `backend/Api/Endpoints/Projects/Environments/Deployments/GetEnvironmentDeploymentsRoute.cs`
- `backend/Api/Endpoints/Projects/Deployments/{GetDeploymentsRoute,GetDeploymentRoute}.cs`
- `backend/Api/Endpoints/Projects/Environments/Domains/GetEnvironmentDomainsCommandHandler.cs`
- `backend/Api.Data/Repositories/{ProjectEnvironmentRepository,ProjectRepository,DomainRepository}.cs`
- `backend/Api/Endpoints/Server/TcpAllocations/GetTcpAllocationsCommandHandler.cs`

**Acceptance criteria**:

- Zero non-test reads of `project.ServerId` / `project.Server.*` outside (a) `AllocateServer_ProjectCreatedEventHandler` and (b) the new resolver. Enforce with a grep gate (subtask self-check).
- `InternalDomain.Value` produces correct hostname for prod-env + preview-env on different servers (write a unit test).
- Apex/custom-domain verification resolves to env's IP when `Domain.ProjectEnvironmentId` is set, else falls back to Production env's IP.
- `dotnet build` and full test suite green.
- `UpdateDomains_DomainChangeEventHandler` writes to every distinct server hosting an env of the project (fan-out test).

**Notes**:

- This is the biggest subtask. Sub-stages inside the child: (1) introduce resolver interface + impl + DI registration; (2) port pure fan-out call sites (§2.A); (3) port URL/DNS reads (§2.B); (4) port repository/query reads (§2.C). Commit in stages if the child wants intermediate confidence.
- **Do NOT regenerate `frontend/src/generated/schema.ts` in this subtask.** Endpoint signatures may shift (some take EnvironmentId now). Regeneration is s-b3's responsibility (after both backend changes settle).

### s-b3 — Wire contracts + frontend picker UI (sequential, after s-b2)

**Intent**: Extend backend DTOs so the frontend can show "the env's server" everywhere it shows server. Add a server-picker step to the create-environment flow on the web. Pull `GET /v1/servers` from Spec A (minimal — without `AdminOnly`) so the picker has a source.

**Suggested files**:

- new `backend/Api/Endpoints/Server/GetServersRoute.cs` + `GetServersResponse.cs` (minimal endpoint; **without** AdminOnly filter — that's Spec A scope; **TODO**: add a visibility filter once Spec A merges)
- `backend/Api/Endpoints/Server/ServerGroup.cs` (register the route)
- `modules/cli/backend/Cli/Endpoints/Projects/ProjectDto.cs` (derive `ServerId` from production env's `ServerId`; add new `Environments` list field with per-env server)
- `backend/Api/Endpoints/Projects/Environments/Get/GetEnvironmentsResponse.cs` (add `Server` field — match Spec A shape so the eventual merge is trivial)
- `backend/Api/Endpoints/Projects/Environments/Get/GetEnvironmentsCommandHandler.cs` (eager-load env.Server)
- `backend/Api/Endpoints/Projects/Project/Get/GetProjectResponse.cs` + Route (add `Server` per env if needed)
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/environments/index.tsx` (server picker in create-env modal)
- regenerate `frontend/src/generated/schema.ts` + `services/V1Service.ts`
- frontend display of allocated server on environments rows (sourced from `env.server`, not `project.server` — matches Spec A's frontend shape)

**Acceptance criteria**:

- `cd frontend && npm run typecheck` and `npm run build` pass.
- Swagger regen produces a diff confined to the new endpoint + new fields (no unrelated drift; check with `git diff --stat frontend/src/generated/`).
- Backend tests green.
- Visual verification: create-environment modal shows a server picker; selecting a server creates the env on that server; environments tab shows the right server per row.
- If Puppeteer access is blocked (no seeded password user — see prior Spec A REPORT), child must STOP, document VERIFICATION_BLOCKED, and signal subtask-done — do not silently skip.

**Notes**:

- **Coordinate with Spec A's eventual merge.** If Spec A lands first, this subtask's `GET /v1/servers` and env.Server field will conflict. Resolution rule: take Spec A's filter-aware version, keep this subtask's per-env wiring.
- CLI binary (`/Users/milo/milodev/gits/cli`) picker + hosting repo (`/Users/milo/milodev/gits/hosting`) `HostingEnvironmentsResponse` change are **out of scope** (per A1); REPORT.md documents them as follow-ups.

## Groups

All three subtasks share files (s-b1 ↔ s-b2 share `CreateEnvironmentCommandHandler.cs` + `AllocateServer_ProjectCreatedEventHandler.cs`; s-b2 ↔ s-b3 share several handler/response files and indirectly `schema.ts`). Connected-components grouping yields **one group, sequential**:

- **Group G1 (sequential): s-b1 → s-b2 → s-b3**

No inter-group parallelism. This is the safest interpretation given the read-site fanout and DTO ripple.

## Risks

(Distilled from skill-notes §6; full mitigations there.)

1. **R1 — Broken URL strings during rollout (`InternalDomain.cs:12`).** Backend reading new field before migration completes → empty hostnames in URLs. Mitigation: backfill is mandatory pre-deploy; resolver returns env's HostName with prod-env fallback when env.ServerId is null (transition period); add a deploy-time integrity check that fails if any active env lacks ServerId.

2. **R2 — DNS A-record verification breaks for non-prod env custom domains.** Three readers (`GetParentDomain`, `GetVerifyRoute`, `DomainVerificationJob`) use `Project.Server.Ip`. Mitigation: resolve via `Domain.ProjectEnvironmentId?.Server?.Ip` with prod-env fallback; add an integration test for custom-domain on Server B preview env.

3. **R3 — VM pooling assumes one server per project (`BaseVMPooler.cs:69`).** If `VMPoolItem` doesn't carry env scope, pool deploys land on wrong server. Mitigation: inspect VMPoolItem schema during s-b2; if schema bump needed, scope to a clearly-flagged follow-up rather than expanding this run.

4. **R4 — Allocator + Stripe ordering (commit `bbc17ed0` just landed per-env subs).** If env-allocation fires before subscription creation, partial-failure orphans billing. Mitigation: keep allocation synchronous inside `CreateEnvironmentCommandHandler`, ordered after subscription creation but before `AddAsync` — preserve the existing try/catch+compensate pattern.

5. **R5 — `CreateEnvironmentCommandHandler` has no test today.** A materially behaviour-changing edit without coverage = blind change. Mitigation: write the tests in s-b1 *before* the migration commit lands.

6. **R6 — Generated client drift in s-b3.** s-b2 will incidentally change swagger surface (new handler signatures), so regenerating the openapi-qraft client in s-b3 may show a wider diff than just the new endpoint. Mitigation: s-b2 explicitly does NOT regenerate; s-b3 regenerates and reviews the diff for unrelated drift; if drift extends beyond the planned changes, child STOPs and reports.

7. **R7 — Coordinate with Spec A merge.** Spec A's branch is unmerged. If user merges Spec A to main mid-run, child branches diverge. Mitigation: surface this as decision point #1 at the approval gate. If user wants Spec A merged first, abort this run cleanly and re-run after.

## Estimated wall time

- s-b1: 1.5–2h (schema + tests is the bulk; allocator move is small)
- s-b2: 2.5–4h (30+ call sites; resolver introduction + DI; integration tests)
- s-b3: 1–1.5h (DTO extension + picker UI + regen — if Puppeteer blocked, child stops short and reports)
- **Total: 5–7.5h** wall time, sequential.

## Decision points the user should confirm

1. **Spec A coverage.** Spec A's branch is unmerged. Options: (a) **proceed** — Spec B pulls a minimal `GET /v1/servers` (no AdminOnly filter) into s-b3 and merges with Spec A later; (b) **abort + merge Spec A first**, then re-run Spec B which sees Spec A's foundations; (c) **rebase Spec B on Spec A** before any subtask runs. Recommendation: **(a)** — Spec A is small and clean, and the conflict at merge time is mechanical.
2. **Scope boundary**: CLI repo (`/cli`) and hosting repo (`/hosting`) changes are deferred to follow-ups (A1). Confirm you're OK with this, vs. expanding scope to include them.
3. **Schema option B** (keep `Project.ServerId` as nullable hint, drop in follow-up) vs. **option A** (drop immediately). Recommend B — preserves CLI wire compat. Confirm.
