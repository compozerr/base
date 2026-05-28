# Skill: improve-codebase-architecture — findings for this slice

## Smells we'd lock in (if not addressed)

- **Allocation logic split between event-handler and (incoming) command path.**
  `modules/cli/backend/Cli/Endpoints/Projects/AllocateServer_ProjectCreatedEventHandler.cs:13-38` mutates `ProjectCreatedEvent.Entity.ServerId` from inside event-dispatch. Adding env-level allocation either duplicates this handler or inlines it in `CreateEnvironmentCommandHandler.cs:28-38`. Recommendation: consolidate into `IServerAllocator.AllocateFor(env|project, requestingUserId)` so the visibility filter + admin gate live in **one** place. **Deferred to Spec B** — Spec A keeps the current handler but injects the new central filter into it.

- **Visibility is filtered ad-hoc at every read site.** Today: `AllocateServer_…EventHandler.cs:24,31`, `LocationRepository.cs:23`, `ServerRepository.AddNewServer`, `DevSeeder.cs`. Adding `AdminOnly` to a hand-rolled filter at each site is the exact scattering the brief warns against. **Spec A action:** introduce a single `IServerVisibilityFilter` (or method on `IServerRepository`) keyed on a `userId` and route every existing read through it.

- **`InternalDomain.Value` reads `Project.Server.HostName` directly** (`backend/Api.Data/InternalDomain.cs:12`). When server moves to env, this lambda silently composes from the (now-removed-or-stale) `Project.Server`. Highest-risk single line in the slice. **Deferred to Spec B** but flagged in the Spec B handoff at end of run.

- **23+ scattered `project.ServerId` / `project.Server` reads.** Notable: `DeployProjectJob.cs:34`, `ProjectEnvironmentHealthCheckJob.cs:21-24` (groups envs by `Project.ServerId`!), `ExposeTcpCommandHandler.cs:25`, `TerminalHub.cs:90-128`, `UpdateDomains_DomainChangeEventHandler.cs:19`, `ProjectsGroup.cs:46`. **Spec B problem**, not Spec A.

- **`ProjectDto.ServerId` is on the CLI wire contract.** Removing the column without versioning breaks installed CLIs. **Spec B handoff note.**

## Terminology to sharpen (do now, before code lands)

- **Two "admin" concepts already coexist.** `RequireRole("admin")` (`AuthFeature.cs:97`) is **platform admin** (Identity-level). `IOrganizationAuthorizationService.IsAdminAsync` is **org admin**. The new `AdminOnly` server visibility means *platform* admin. **Spec A action:** name the new auth helper `IPlatformAuthorizationService` (or `ICurrentUserAccessor.IsPlatformAdmin()`); never reuse `IOrganizationAuthorizationService.IsAdminAsync` for server gating.
- **Rename `Server.ServerVisibility` → `Server.Visibility`** while we're touching it. Otherwise call sites read `ServerVisibility.ServerVisibility.AdminOnly`. Property-only rename, no schema impact.
- **"Allocate" vs "assign" vs "pick".** Once the picker lands (Spec B), allocate becomes a fallback. Decide naming there.

## Coverage gaps

- Zero tests on `AllocateServer_ProjectCreatedEventHandler`. Adding visibility filtering with no net is risky. **Spec A must add coverage here.**
- `Cli.Tests/` directory is empty (only obj/). CLI picker will land into untested territory — Spec B concern.
- No `CreateEnvironmentCommandHandler` test. Spec B concern.
- No test verifies `InternalDomain.Value` resolves the correct hostname. Spec B concern.
- No multi-server-per-project test for `ProjectEnvironmentHealthCheckJob`. Spec B concern.

## Cross-repo notes

- Hosting (`/Users/milo/milodev/gits/hosting/manager/services/domain-service.ts`) does NOT consume `ServerVisibility` today. Spec A is invisible to hosting.
- `Server.ApiDomain` is the per-server hosting-manager endpoint. Post-Spec-B, every `hostingApiFactory.GetHostingApiAsync(project.ServerId)` call site must route via env. Spec B concern.
- No `CONTEXT.md` / `docs/adr/` exist. Worth seeding with Server / Project / Environment / Domain / PlatformAdmin / OrgAdmin terms — **deferred** (not load-bearing for Spec A).
