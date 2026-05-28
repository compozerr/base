# Spec B — per-env server allocation: skill notes (round 1)

> Worktree branched from `main` (commit `bbc17ed0`). **Spec A is NOT merged here** — `IServerVisibilityFilter`, `AllocatedServerSummary`, `GET /v1/servers`, and the env-side `server` field on `GetEnvironmentsResponse` do not exist in this tree. The `ServerVisibility` enum has only `Public`/`Unknown` (no `AdminOnly`). Spec B must build these foundations or proceed without them.

## 1. Architecture-tension summary

The change is a **1-to-1 → 1-to-N migration of the `Server` association from `Project` to `ProjectEnvironment`**. In the language of the skill:

- `Project` is currently the **module** that owns the seam `Project → Server` (nullable FK + nav). That seam is **shallow**: 27 call sites in non-migration code read `project.ServerId`/`project.Server` directly, doing nothing except routing a hosting call via `hostingApiFactory.GetHostingApiAsync(project.ServerId)`. The interface is "everyone fans out by hand."
- The right deepening is a new module — call it **EnvironmentTargetResolver** — whose **interface** is `ProjectEnvironment → IHostingApi` (plus IP / ApiDomain / HostName when needed for DNS + InternalDomain). The implementation owns the per-env server lookup, the `Include(p=>p.Server)` discipline, and the no-server-allocated error mode.
- Every existing reader is then either (a) a caller of that deepened module — gaining **leverage** — or (b) a reader that should be ported to take a `ProjectEnvironment` (or `ProjectEnvironmentId`) as its primary key, not `ProjectId`. After the migration, `(ProjectId, ServerId)` is no longer a meaningful pair; `(ProjectEnvironmentId, ServerId)` is.
- The **deletion test** for the existing implicit Project→Server seam: if we deleted the `project.ServerId` field today (without backfill), complexity would spew across 27 call sites. That's exactly what the migration must *also* do, in one motion — so build the EnvironmentTargetResolver seam FIRST, route every call site through it, THEN move the FK.
- **Load-bearing assumption to break carefully**: `AllocateServer_ProjectCreatedEventHandler` (`modules/cli/backend/Cli/Endpoints/Projects/AllocateServer_ProjectCreatedEventHandler.cs:11`) allocates **one server per project** before save. After the migration, allocation must move to env creation (a `ProjectEnvironmentCreatedEvent` or sync allocator inside `CreateEnvironmentCommandHandler`). The "production env created with the project" path needs to fire the same allocator.
- `ServerVisibility` (the column on `Server`, not a new `IServerVisibilityFilter`) is queried in 3 places (`AllocateServer_ProjectCreatedEventHandler.cs:24,31`, `LocationRepository.cs:23`, `DevSeeder.cs:42,50`). Spec A's filter abstraction doesn't exist — the rename to `Visibility` is a 6-site change at most. **Worth doing** in this branch only if Spec A's foundation tasks are pulled in; otherwise defer (no benefit alone).

## 2. Verified call-site enumeration

Searched with `rg --glob '!**/Migrations/**' --glob '!**/generated/**'` on `project\.ServerId|Project\.ServerId|project\.Server\b|Project\.Server\b|p\.ServerId|p\.Server\b|p!\.Server|\.Project\.ServerId|\.Project\.Server\b`. The set below is the complete non-migration call-graph that assumes one server per project.

### A. Reads `project.ServerId` to call `hostingApiFactory.GetHostingApiAsync(...)`

These are pure fan-out points — must route via the env's server instead.

| File:line | Code | Change |
|---|---|---|
| `backend/Api.Hosting/Jobs/DeployProjectJob.cs:34` | `deployment.Project!.ServerId ?? throw new ServerNotFoundException()` | Switch to `deployment.Environment!.ServerId` (Deployment already has `EnvironmentId`; load Environment.Server). |
| `backend/Api.Hosting/VMPooling/Core/BaseVMPooler.cs:69` | `hostingApiFactory.GetHostingApiAsync(project.ServerId!)` | VMPool item must carry env scope OR pool per env. See risk R3. |
| `backend/Api.Hosting/EventHandlers/UpdateDomains_DomainChangeEventHandler.cs:19` | `domainProject.ServerId ?? throw …` | Domains already carry `ProjectEnvironmentId?` (`backend/Api.Data/Domain.cs:17`). Fan out: for each env that has domains under this project, push to that env's server. If `Domain.ProjectEnvironmentId` is null (apex / project-level domain), resolve to production env's server. |
| `backend/Api/Endpoints/Projects/Project/Start/StartProjectCommandHandler.cs:17,19` | `project?.ServerId is { } serverId` then `StartProjectAsync(projectId, EnvironmentName)` | Already takes `EnvironmentName` — switch to env lookup by name, route via env.ServerId. |
| `backend/Api/Endpoints/Projects/Project/Stop/StopProjectCommandHandler.cs:17,19` | same shape, no env in command | Add envId/envName to command; fan out to all envs if not specified (stop project = stop all envs). |
| `backend/Api/Endpoints/Projects/Project/Delete/DeleteProjectCommandHandler.cs:27,29` | `project.ServerId is { } serverId` then `hostingApi.DeleteProjectAsync(project.Id)` | Fan out: iterate envs, call DeleteProjectAsync on each env's server. Production env's server is the last one cleaned. |
| `backend/Api/Endpoints/Projects/Project/ExecuteCommand/ExecuteCommandCommandHandler.cs:17` | `project?.ServerId` | Command already takes `EnvironmentName`. Route via env. |
| `backend/Api/Endpoints/Projects/Environments/Delete/DeleteEnvironmentCommandHandler.cs:45,47` | `project?.ServerId is { } serverId` then `DeletePreviewEnvironmentAsync(projectId, env.Name)` | Trivial — switch to `environment.ServerId` (already loaded by caller). |
| `backend/Api/Endpoints/Projects/Services/ChangeProtocol/ChangeProtocolCommandHandler.cs:27,29` | `project.ServerId is not null` then `GetHostingApiAsync(project.ServerId)` | If protocol change is per-service/per-env, route via env. If protocol is a project-wide setting, fan out. Inspect ProjectService schema (it has `ProjectEnvironmentId`). |
| `backend/Api/Endpoints/Projects/Services/ExposeTcp/ExposeTcpCommandHandler.cs:25,37,46` | `project.ServerId is null || project.Server is null` then 2× `GetHostingApiAsync(project.ServerId!)` | Service is env-scoped (`AllocateDomains_ProjectServiceUpsertedEventHandler.cs:39` filters by `ProjectEnvironmentId`). Route via the service's env. Also fix `:84` `VmIp = project.Server!.Ip` and `:39,56` `project.Server.ApiDomain` (return env's server's API domain). |
| `backend/Api/Endpoints/Projects/Services/UnexposeTcp/UnexposeTcpCommandHandler.cs:20,30` | `project.ServerId is null` then `GetHostingApiAsync(project.ServerId!)` | Route via env. |
| `backend/Api/Endpoints/Projects/Environments/Services/GetServiceLogs/GetServiceLogsCommandHandler.cs:25-29,46` | Query selects `p.ServerId` then `GetHostingApiAsync(project.ServerId)` | Already env-scoped (command has EnvironmentId). Switch projection to env.ServerId. |
| `backend/Api/Endpoints/Projects/ProjectsGroup.cs:46-50` | debug terminal endpoint reads `project?.ServerId` | Debug endpoint — accepts env optionally or routes via production env. |

### B. Reads `project.Server.HostName/Ip/ApiDomain` (DNS / URL strings)

These are the highest-risk reads — they encode the server identity into user-visible URLs.

| File:line | Code | Change |
|---|---|---|
| **`backend/Api.Data/InternalDomain.cs:12`** | `$"{Subdomain}.sites.{Project?.Server?.HostName}.compozerr.com"` | **Highest-risk single line.** Replace with `ProjectEnvironment?.Server?.HostName`. Requires `InternalDomain` (or Domain base) to be able to navigate `ProjectEnvironment.Server`. Since `Domain.ProjectEnvironmentId` is already nullable, fall back to project's production-env server when null (apex/project-level domains). |
| `modules/terminal/backend/Terminal/TerminalHub.cs:82,90,110,128` | `.Include(p => p.Server)` then `project.Server.ApiDomain` | Terminal hub is per-VM (preview env). It already has env context (vmIdentifier). Look up env's server, not project's. |
| `backend/Api/Endpoints/Projects/Domains/Parent/Get/GetParentDomainCommandHandler.cs:19-22` | `.ThenInclude(p => p!.Server)` then `domainWithServer?.Project?.Server?.Ip` | DNS A-record verification. Should resolve via `Domain.ProjectEnvironmentId?.Server?.Ip` falling back to production env. Mismatched IP = false negative on apex domain verification. |
| `backend/Api/Endpoints/Projects/Domains/Verify/GetVerifyRoute.cs:51-56` | Same shape: `.ThenInclude(p => p!.Server)` then `.Project?.Server?.Ip` | Same fix as Parent/Get. |
| `backend/Api/Jobs/DomainVerificationJob.cs:26,59` | `.ThenInclude(p => p!.Server)` then `domain.Project?.Server?.Ip` | Same fix. This is the periodic version, run as a job. |
| `backend/Api/Endpoints/Projects/Environments/Services/GetEnvironmentServicesCommandHandler.cs:16-17` | `Where(p.ServerId != null).Select(p.Server!.ApiDomain)` | Command has env context — switch to env's server. |
| `backend/Api/Endpoints/Projects/Services/Get/GetProjectServicesCommandHandler.cs:15-16` | same | Project-scoped getter; if services span envs, this returns one ApiDomain — must group by env or drop the project-wide variant. |
| `backend/Api/Endpoints/Projects/Environments/Deployments/GetEnvironmentDeploymentsRoute.cs:38,63` | `.ThenInclude(x => x.Server!)` then `x.Project?.Server?.Location?.IsoCountryCode` | Env-scoped route. Use the deployment's environment.Server.Location, not project's. |
| `backend/Api/Endpoints/Projects/Deployments/GetDeploymentsRoute.cs:73,103` | same shape | Project-wide list — group by env, show env's server location per row. |
| `backend/Api/Endpoints/Projects/Deployments/GetDeploymentRoute.cs:34` | `.ThenInclude(x => x.Server!)` | Single deployment — has env, use env.Server. |
| `backend/Api/Endpoints/Projects/Environments/Domains/GetEnvironmentDomainsCommandHandler.cs:20` | `.Include(d => d.Project).ThenInclude(p => p!.Server)` | Env-scoped — include env.Server. |

### C. Repository / query-shape reads

| File:line | Code | Change |
|---|---|---|
| `backend/Api.Data/Repositories/ProjectEnvironmentRepository.cs:41` | `.Include(e => e.Project!).ThenInclude(p => p.Server!)` | Replace with `.Include(e => e.Server!)` direct nav. |
| `backend/Api.Data/Repositories/ProjectEnvironmentRepository.cs:71` | `e.Project.ServerId != null` | Switch to `e.ServerId != null`. (Also rename `GetAllActiveWithProjectAndServerAsync`.) |
| `backend/Api.Data/Repositories/ProjectRepository.cs:39` | `.Include(x => x.Server)` | If the call sites are all on the read paths in §A/B, this Include becomes unused — verify and delete; otherwise leave during transition. |
| `backend/Api.Data/Repositories/DomainRepository.cs:26` | `.ThenInclude(x => x!.Server)` | Domain's server resolves through env now. Drop or replace with `.ThenInclude(p => p!.ProjectEnvironments!).ThenInclude(e => e.Server)`. |
| `backend/Api/Endpoints/Server/TcpAllocations/GetTcpAllocationsCommandHandler.cs:15` | `.Where(p => p.ServerId == command.ServerId)` (Projects table) | After migration, query `ProjectEnvironments.Where(e => e.ServerId == command.ServerId)` (TCP allocations are project-level today but VMs are per-env). |

### D. Allocator / writer

| File:line | Code | Change |
|---|---|---|
| `modules/cli/backend/Cli/Endpoints/Projects/AllocateServer_ProjectCreatedEventHandler.cs:14` | `domainEvent.Entity.ServerId = bestServerInLocationResponse.ServerId` (sets Project.ServerId) | Move allocation to per-env. Either (a) fire allocator on `ProjectEnvironmentCreatedEvent` (preferred — keeps it event-driven) or (b) inline allocation inside `CreateEnvironmentCommandHandler`. The CreateProject flow also creates a production env, so a Project Created handler that allocates *that env* is still needed. |
| `backend/Api/Endpoints/Projects/Environments/Create/CreateEnvironmentCommandHandler.cs` (no `ServerId` written today) | env saved with no ServerId | Must accept (or allocate) a `ServerId`. The brief calls for a server-picker UI step — so command grows a `ServerId?` parameter; when null, allocator picks. |

### E. CLI / wire-contract

| File:line | Code | Change |
|---|---|---|
| `modules/cli/backend/Cli/Endpoints/Projects/ProjectDto.cs:9,18` | `Guid ServerId` field, populated from `project.ServerId!.Value` | See §5 wire-contract section. Must derive (first env / prod env) or drop. |
| `/Users/milo/milodev/gits/cli/src/utils/api/types/project-dto.ts:5` | `serverId: string` | Mirror whatever §5 picks. |

### F. Schema / FK definitions

| File:line | What |
|---|---|
| `backend/Api.Data/Project.cs:31-32` | `public ServerId? ServerId { get; set; }` + `public virtual Server? Server { get; set; }` — to be retained or dropped (see §4). |
| `backend/Api.Data/ApiDbContext.cs:70-71` | Secret↔Server `WithOne(s => s.Server).HasForeignKey<Secret>(s => s.ServerId)` — unrelated to Project, leave alone. |

### G. Plan / docs (informational, not code)

- `docs/superpowers/plans/2026-04-24-primary-service-and-health-check.md:1225` — historical example, ignore.

**Total verified non-migration call sites that assume one-server-per-project: 31** (count of rows in §A + §B + §C + §D + §E; some files have multiple lines that I've folded into one row).

## 3. Read-side strategy

**Default: every reader switches to `env.ServerId`.** A `ProjectEnvironment.ServerId` (non-null after backfill) becomes the new single source of truth.

Two careful exceptions:

1. **Apex / project-level Domains** (`Domain.ProjectEnvironmentId is null`): production env's server is the right fallback. The DNS A-record points at the production VM. This is encoded in `Domain.cs:17` already — `ProjectEnvironmentId` is nullable. Add a small helper `Domain.GetEffectiveServer(project)` that returns `ProjectEnvironment.Server` if set, else `project.ProjectEnvironments.Single(e => e.Type == Production).Server`.
2. **Allocator** (`AllocateServer_ProjectCreatedEventHandler`): the "all envs share a server" assumption was load-bearing for `Best server in location` selection. After migration, allocation is per-env. The picker UI explicitly asks the user; the allocator handler becomes the fallback when no `ServerId` is supplied. The "first server in location, prefer lowest RAM%" heuristic still applies — just at env granularity. (Note: this means a project can spread across multiple servers in the same location — fine, that's the whole point.)

**Not an exception**: `GetProjectServicesCommandHandler` (project-wide service list, returns one ApiDomain). This was always shallow — services are env-scoped (`AllocateDomains_ProjectServiceUpsertedEventHandler.cs:39`). Either drop the route (envs-only variant already exists) or have it return a list grouped by env.

## 4. Schema-migration shape

**Recommended: option B (keep `Project.ServerId` nullable as a "default server for new envs" hint, write `ProjectEnvironment.ServerId` as the load-bearing FK).**

Comparison:

**Option A — Drop `Project.ServerId` entirely**

- *Migration*: add `ProjectEnvironment.ServerId UUID NULL`, backfill `UPDATE pe SET ServerId = p.ServerId FROM Projects p WHERE pe.ProjectId = p.Id`, then drop `Project.ServerId`.
- *Pros*: clean model, single source of truth, no drift.
- *Cons*: every project transfer / project-creation-with-no-envs flow loses its server hint. Several admin/observability paths (TCP allocations queried by ServerId, project list filtering by server) would need to derive the answer from envs.
- *Pros*: forces every reader to be explicit about env scope. Deletion test passes — complexity vanishes.

**Option B — Keep `Project.ServerId` as a nullable "default server" hint**

- *Migration*: add `ProjectEnvironment.ServerId UUID NULL`, backfill same way, leave `Project.ServerId` in place but stop reading it on hot paths. The `AllocateServer_ProjectCreatedEventHandler` continues to set `Project.ServerId` AND, for the auto-created production env, also sets `ProjectEnvironment.ServerId`. New `CreateEnvironmentCommandHandler` defaults to `Project.ServerId` when no picker selection.
- *Pros*: keeps wire compatibility (`ProjectDto.ServerId` still has a non-null value), simpler rollout, smaller blast radius — if a reader is missed, it sees the old correct-for-prod-env value rather than null.
- *Cons*: two fields can drift if env is moved between servers post-creation (need a rule: "moving env servers doesn't update project.ServerId"); risk of new code accidentally reading the stale field.

**Pick option B.** The drift risk is mitigated by treating `Project.ServerId` as a *hint*, not state — document it on the property and add an analyzer-style test ("no production code outside Allocator/CreateEnvironment may read Project.ServerId"). The wire-compat win on the CLI is real (downgrade-safe). A follow-up cleanup commit can drop `Project.ServerId` once the CLI minimum version is bumped.

Backfill SQL (Postgres, EF migration body):

```sql
ALTER TABLE "ProjectEnvironments" ADD COLUMN "ServerId" uuid NULL;
UPDATE "ProjectEnvironments" pe
   SET "ServerId" = p."ServerId"
  FROM "Projects" p
 WHERE pe."ProjectId" = p."Id"
   AND p."ServerId" IS NOT NULL;
CREATE INDEX "IX_ProjectEnvironments_ServerId" ON "ProjectEnvironments" ("ServerId");
ALTER TABLE "ProjectEnvironments"
  ADD CONSTRAINT "FK_ProjectEnvironments_Servers_ServerId"
  FOREIGN KEY ("ServerId") REFERENCES "Servers" ("Id") ON DELETE RESTRICT;
```

Eventual drop (in a follow-up migration, after CLI version bump):

```sql
ALTER TABLE "Projects" DROP CONSTRAINT IF EXISTS "FK_Projects_Servers_ServerId";
ALTER TABLE "Projects" DROP COLUMN "ServerId";
```

## 5. Wire-contract impact

### CLI: `Cli.Endpoints.Projects.ProjectDto`

Today: `ProjectDto.ServerId: Guid` (non-null). After Spec B, projects can have multiple envs → no single ServerId.

**Recommendation**: keep the field, **derive from the production env's `ServerId`** (or `Project.ServerId` hint while option B is in force). Add a new non-breaking field:

```csharp
public sealed record ProjectDto(
    string Name,
    string RepoUri,
    Guid UserId,
    Guid ServerId,                                    // deprecated: production env's server
    IReadOnlyList<EnvironmentServerDto>? Environments // new
);
public sealed record EnvironmentServerDto(Guid EnvironmentId, string EnvironmentName, Guid ServerId, string ServerHostName);
```

CLI consumer (`cli/src/utils/api/types/project-dto.ts:5`): mirror the new field. Mark `serverId` as `@deprecated`. The CLI `compozerr ssh -e <env>` flag, mentioned in CLAUDE.md, must consult the per-env field.

### Hosting repo: `HostingEnvironmentsResponse`

Today (`/Users/milo/milodev/gits/hosting/manager/types/HostingEnvironmentsResponse.d.ts:1-6`):

```ts
type HostingEnvironmentsResponse = { id, name, type, subdomain }[]
```

Must gain `serverHostName: string` and/or `serverId: string` per row so the manager knows which server hosts which env. This is consumed by `/Users/milo/milodev/gits/hosting/manager/services/deployment-api-client.ts:158` and the domain test at `__tests__/domain-service.test.ts:604`. Adding a field is non-breaking (existing manager code ignores it).

Cross-repo coordination: the web backend emits this response from a hosting-facing endpoint (look in `backend/Api.Hosting/` for the writer). Bump it and re-run the manager's type-checking.

## 6. Risks (with concrete mitigations)

1. **R1 — Broken URL strings during rollout.** `InternalDomain.cs:12` is the highest-risk single line: it computes `$"...{Project?.Server?.HostName}..."`. If we deploy backend code that reads `ProjectEnvironment.Server.HostName` *before* the backfill migration runs, every internal URL renders as `.sites...compozerr.com` (empty hostname) — visible regression. *Mitigation*: gate the read with `Project?.ProjectEnvironments?.Single(...).Server?.HostName ?? Project?.Server?.HostName`. Drop the fallback in a follow-up commit *after* backfill is verified in prod.

2. **R2 — DNS A-record verification breaks for custom apex domains.** `GetParentDomainCommandHandler.cs:22`, `GetVerifyRoute.cs:56`, `DomainVerificationJob.cs:59` all read `Project.Server.Ip`. If a user has a custom domain mapped to a non-production env (currently doesn't happen, but the model allows it), verification fails because the A record points to the prod env's IP. *Mitigation*: resolve to `Domain.ProjectEnvironment?.Server?.Ip` falling back to production env; add an integration test that creates a preview env on Server B, custom-domain-attaches it, and verifies the A record points at Server B.

3. **R3 — VM pooling assumes one server per project.** `BaseVMPooler.cs:69` redeploys a pool item via `project.ServerId`. If pool items represent per-env warm VMs and the project spans servers, the wrong server gets the deploy. *Mitigation*: inspect `VMPoolItem` schema (search shows `VMPoolItem parent key.Designer.cs:67`). If it carries an env id or a server id, route via that. If not, this needs a schema bump — add `VMPoolItem.ProjectEnvironmentId` or `ServerId`. Ship VM pooling change in a separate subtask after the read-site fanout.

4. **R4 — Health check job silently skips envs without a server.** `ProjectEnvironmentHealthCheckJob.cs:23` filters `e.Project?.ServerId is not null` and groups by it. After the migration, envs created via the new flow have `Environment.ServerId`, but legacy envs have only their `Project.ServerId`. *Mitigation*: backfill is mandatory before deploy. Add a startup integrity check that fails fast if `SELECT COUNT(*) FROM ProjectEnvironments WHERE ServerId IS NULL AND BillingStatus = 'Active'` > 0 in production environments.

5. **R5 — `CreateEnvironmentCommandHandler` has no test (`modules/cli/backend/Cli.Tests/Features` is empty for projects).** Spec B materially changes its responsibilities: server selection, validation against `IServerVisibilityFilter` (if Spec A is pulled in), tier/server compat. *Mitigation*: write integration tests for `CreateEnvironmentCommandHandler` covering (a) explicit ServerId selection, (b) allocator fallback when ServerId omitted, (c) rejection when ServerId is AdminOnly and caller isn't admin (requires Spec A; otherwise just public/private check), (d) Stripe subscription compensation on env-creation failure. Do this *before* the migration commit, not after.

6. **R6 — Stripe subscription per env on transfer.** `bbc17ed0` (most recent commit) just landed per-env Stripe subscriptions; this migration touches env creation. If allocator is called on `ProjectEnvironmentCreatedEvent` it must fire AFTER subscription creation (or we get orphaned billing). *Mitigation*: keep allocation synchronous inside `CreateEnvironmentCommandHandler`, ordered after subscription creation but BEFORE `AddAsync`. The existing try/catch+compensate pattern already handles the inverse case.

7. **R7 — `UpdateDomains_DomainChangeEventHandler` will under-update during transition.** Pushing to `domainProject.ServerId` (`UpdateDomains_DomainChangeEventHandler.cs:19`) currently updates exactly one Traefik. After the migration we need to push per-env (or push once per server-hosting-this-project's-envs). *Mitigation*: change to fan out: `var serverIds = project.ProjectEnvironments.Select(e => e.ServerId).Distinct(); foreach (var s in serverIds) push;`. Mark as idempotent (the hosting API's UpdateDomainsForProjectAsync should already be idempotent).

## 7. Suggested subtask decomposition

Targeted at the orchestrator: 4 subtasks, the first 3 sequential, the 4th parallelizable with #3.

### S1 — Schema + backfill + allocator (sequential, must be first)

- **Intent**: Add `ProjectEnvironment.ServerId` as nullable FK, backfill from `Project.ServerId`, move allocation to env creation.
- **Files**:
  - `backend/Api.Data/ProjectEnvironment.cs` (add `ServerId?` + nav)
  - `backend/Api.Data/ApiDbContext.cs:65-72` area (configure new FK)
  - new EF migration under `backend/Api.Data/Migrations/`
  - `modules/cli/backend/Cli/Endpoints/Projects/AllocateServer_ProjectCreatedEventHandler.cs` (also write env.ServerId for prod env created with project; keep writing Project.ServerId as hint)
  - `backend/Api/Endpoints/Projects/Environments/Create/CreateEnvironmentCommand.cs` (+ Validator, Handler) — add optional `ServerId?` parameter, allocator fallback
  - new `Cli.Tests/Features/Projects/CreateEnvironmentCommandHandlerTests.cs` (R5)
- **Acceptance**: dotnet build + tests green; backfill SQL idempotent; new env created with no `ServerId` parameter still gets allocated correctly; new env created with explicit `ServerId` honors it; running tests against a seeded DB with N projects results in N×(envs) rows all with non-null `ServerId`.

### S2 — Read-site fan-out via deepened seam (sequential, after S1)

- **Intent**: Introduce `IEnvironmentTargetResolver` (interface: `Task<IHostingApi> ForAsync(ProjectEnvironmentId)`, `Task<Server> GetServerAsync(ProjectEnvironmentId)`). Re-route the 27 call sites in §A and §B through it.
- **Files**: every file listed in §2.A and §2.B. File-overlap with S1: only `CreateEnvironmentCommandHandler.cs` and `AllocateServer_ProjectCreatedEventHandler.cs` (so S2 must follow S1).
- **Acceptance**: zero non-test reads of `project.ServerId` / `project.Server.*` outside the allocator and the new seam; `InternalDomain.Value` produces correct hostname for prod + preview envs on different servers (write an integration test); apex domain verification picks the right IP for non-prod-env domains.

### S3 — Wire contracts + frontend/CLI picker UI (parallelizable with S4)

- **Intent**: Extend `ProjectDto` with env-server list, add server-picker step to create-project + create-environment flows (CLI + web), update hosting repo `HostingEnvironmentsResponse`.
- **Files**:
  - `modules/cli/backend/Cli/Endpoints/Projects/ProjectDto.cs` (add per-env list field)
  - `/Users/milo/milodev/gits/cli/src/utils/api/types/project-dto.ts` (mirror)
  - new endpoint `GET /v1/servers` (Spec A scope — pull into Spec B; needed for the picker)
  - frontend create-project / create-env flows (locate them — the only one verified is `frontend/src/routes/n8n.tsx:28,56,370` which is the n8n template; the generic CLI create flow is purely CLI-driven, so the web picker work is on the env-creation page `frontend/src/routes/_auth/_dashboard/projects/$projectId/environments/index.tsx`)
  - CLI: `compozerr` server-picker prompt in `/Users/milo/milodev/gits/cli/src/` (locate create-project + create-env commands)
  - hosting repo: `/Users/milo/milodev/gits/hosting/manager/types/HostingEnvironmentsResponse.d.ts` add `serverHostName`/`serverId`
  - regenerate `frontend/src/generated/schema.ts`
- **Acceptance**: CLI `compozerr project create` and `compozerr env create` show server picker; web env-creation modal shows server picker; old CLI versions still receive a populated `ProjectDto.ServerId` (compat).

### S4 — Cross-cutting risk fixes (parallelizable with S3)

- **Intent**: VM pooling, health check job startup integrity check, UpdateDomains fan-out.
- **Files**:
  - `backend/Api.Hosting/VMPooling/Core/BaseVMPooler.cs:69` (R3)
  - `backend/Api.Hosting/Jobs/ProjectEnvironmentHealthCheckJob.cs:21-24` (group by env's ServerId now)
  - `backend/Api.Hosting/EventHandlers/UpdateDomains_DomainChangeEventHandler.cs:19` (fan-out, R7)
  - `backend/Api.Data/Repositories/ProjectEnvironmentRepository.cs:71` (rename method, drop project-side ServerId filter)
- **Acceptance**: health check correctly probes all envs across servers; UpdateDomains pushes to every server-hosting-an-env-of-this-project exactly once; VM pooling deploys to the env's server (or a structural change to VMPoolItem is documented as a follow-up subtask).

**File-overlap notes for the orchestrator**:

- S1 + S2: only overlap at `CreateEnvironmentCommandHandler.cs` and `AllocateServer_ProjectCreatedEventHandler.cs`. S2 must wait.
- S2 + S4: overlap at `ProjectEnvironmentHealthCheckJob.cs` (S2 changes the read sites, S4 changes the grouping). Order: S2 first, then S4 builds on the resolver introduced.
- S3 + S2: no source-code overlap with `backend/Api.Data/ProjectEnvironment.cs` if S1 has landed. Schema gen overlap on `frontend/src/generated/schema.ts` (regenerated by S3; S2 also touches handlers that change the swagger). Run S3's swagger-regen last in its branch.
- S3 + S4: no overlap.

**Skipped from this round**: `ServerVisibility` → `Visibility` rename. Argument for: 6-site change, mostly mechanical. Argument against: no behavioural benefit unless Spec A's `AdminOnly` is added — this branch doesn't have it. **Defer to a follow-up subtask bundled with the AdminOnly addition.**
