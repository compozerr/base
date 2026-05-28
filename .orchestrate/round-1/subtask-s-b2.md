# Subtask s-b2 — IEnvironmentTargetResolver seam + read-site fanout

**Intent:** Introduce a single `IEnvironmentTargetResolver` interface that resolves `ProjectEnvironmentId → IHostingApi` (and `→ Server` for DNS/HostName reads). Re-route every non-allocator call site that currently reads `project.ServerId`, `project.Server.HostName`, `project.Server.Ip`, or `project.Server.ApiDomain` through this resolver. Fix the highest-risk single line at `InternalDomain.cs:12`. This is the largest subtask in the run.

**Read first, before writing anything:**

- `.orchestrate/round-1/plan.md` — full plan + assumptions A2/A3/A4.
- `.orchestrate/round-1/skill-notes.md` §2 — **the complete enumeration of 31 verified call sites**. This is your work-list; do not stop short.
- `backend/Api.Hosting/HostingApiFactory.cs` — the existing `IHostingApiFactory` you'll wrap.

**Don't start any code edit before you have:**

1. Confirmed s-b1 is merged into your branch (your branch is created off it).
2. Walked the §2 enumeration and made your own list. If the count differs from 31, write down why before continuing.

**Suggested files** (full list in skill-notes §2 — abbreviated here):

- new `backend/Api.Hosting/EnvironmentTargetResolver/IEnvironmentTargetResolver.cs` (interface + impl). Shape:
  ```csharp
  public interface IEnvironmentTargetResolver
  {
      Task<IHostingApi> GetHostingApiAsync(ProjectEnvironmentId environmentId, CancellationToken ct);
      Task<Server> GetServerAsync(ProjectEnvironmentId environmentId, CancellationToken ct);
      Task<Server> GetServerForDomainAsync(Domain domain, CancellationToken ct); // applies apex fallback
  }
  ```
  Register in the appropriate `IFeature` (likely `ApiHostingFeature.cs`).
- `backend/Api.Data/InternalDomain.cs:12` — replace `Project?.Server?.HostName` with env's HostName, falling back to Production env's server's HostName when `ProjectEnvironmentId` is null. (You may need to add a navigation property to `Domain`/`InternalDomain` to reach the env's Server — investigate during the work.)
- The §2.A files (12 pure fan-out points, calls to `hostingApiFactory.GetHostingApiAsync(project.ServerId)`).
- The §2.B files (11 DNS/URL reads of `project.Server.HostName`/`Ip`/`ApiDomain`/`Location`).
- The §2.C files (5 repository/query reads, including the rename `GetAllActiveWithProjectAndServerAsync` → ...).
- Integration test that exercises `InternalDomain.Value` for an env on a different server than its project's "hint" server.
- Integration test for `UpdateDomains_DomainChangeEventHandler` fan-out (distinct envs on distinct servers → distinct hosting-api pushes).

**Acceptance criteria:**

- [ ] `dotnet build backend/Base.sln` clean.
- [ ] Full test suite (`dotnet test backend/`) green, including new tests.
- [ ] **Grep gate**: `rg 'project\.ServerId|Project\.ServerId|project\.Server\b|Project\.Server\b' --glob '!**/Migrations/**' --glob '!**/generated/**' backend/ modules/ | grep -v 'AllocateServer_ProjectCreatedEventHandler\|CreateEnvironmentCommandHandler\|IEnvironmentTargetResolver\|ProjectEnvironmentRepository\|/Project.cs\|/ApiDbContext.cs'` returns **empty** (or only test fixtures that explicitly set the legacy hint). Run this in the run worktree before signaling done.
- [ ] `InternalDomain.Value` correctness test: project P has prod env on Server A, preview env on Server B; the prod env's InternalDomain uses A's HostName, preview's uses B's HostName.
- [ ] Apex/project-level domain (where `Domain.ProjectEnvironmentId is null`) resolves to the production env's server. Cover with a test.
- [ ] `UpdateDomains_DomainChangeEventHandler` fan-out test: project with envs on Server A and Server B, domain-change event triggers update on BOTH servers.
- [ ] `ProjectEnvironmentHealthCheckJob` groups by `env.ServerId` (not `project.ServerId`).
- [ ] **Do NOT regenerate `frontend/src/generated/`** — wire-contract changes go in s-b3.

**Verification (mandatory):**

- The grep gate above is non-negotiable. If it produces any unexpected hit, route it through the resolver before signaling done.
- Run `compozerr dev`; manually hit the swagger endpoints affected by the changes (e.g. `POST /v1/projects/{id}/environments/{name}/start`) to confirm they still return 200.

**Notes / gotchas:**

- **The resolver replaces a pattern, not introduces a new one.** Every site that currently does `var serverId = project.ServerId; var api = await hostingApiFactory.GetHostingApiAsync(serverId);` should become `var api = await environmentTargetResolver.GetHostingApiAsync(environmentId, ct);`. If your patch grows a new helper per call site, you've drifted — back up.
- **Project-wide endpoints (Start, Stop, Delete, GetProjectServices) fan out across envs** — they take a project and must iterate envs. This is a semantic change; cover with tests. The handler signature changes from "get one server" to "for each env, get its server."
- **Apex domain fallback** is encoded *once* inside `GetServerForDomainAsync`. Don't replicate the fallback rule at every reader; reach for the resolver.
- **`VMPoolItem`** may not carry env scope (skill-notes R3). If it doesn't, route VM pooling reads through resolver-by-project (allocate to first env's server). Flag in your commit message as a known weakness; add a follow-up TODO. Do NOT schema-change VMPoolItem in this subtask.
- **Stripe subscription ordering** (skill-notes R4) is not affected by this subtask — you're not touching `CreateEnvironmentCommandHandler` again.
- **`ServerVisibility` filter is NOT in this tree** (Spec A not merged). The resolver does NOT filter by visibility; that's a follow-up. Document in code comments where filtering would slot in.
- **Stage your work**: (1) introduce resolver + DI registration + tests for the resolver itself; (2) port §2.A; (3) port §2.B; (4) port §2.C. Commit between stages if the child wants intermediate confidence — final merge-back commit can squash if needed.
- **Cli.Tests is new from s-b1.** Don't break those tests when the env-creation handler implicitly changes if you have to touch it (you shouldn't need to).

**Signal when done.** Run `/code-review` self-review before signaling. Address any blocking findings before commit.
