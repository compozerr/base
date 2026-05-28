# Subtask f-b2 — Spec B P2 fixes + Api.Tests runnable

**Intent:** Resolve the four P2 findings on the Spec B branch, AND provision `backend/.env` so the full `Api.Tests` suite actually runs (Round-1 verification was incomplete because the build failed on the missing .env, and Api.Tests was silently skipped).

**Branch:** `task/spec-b-per-env-server-allocation-20260528-151212`, after f-b1 has merged into it.

**Read first:**

- `.orchestrate/subtask.md` (this file).
- `/Users/milo/milodev/gits/web/.worktrees/spec-b-per-env-server-allocation-20260528-151212/.orchestrate/round-2/codex-findings/spec-b-review.md` — P2 detail.
- `backend/Api.Data/InternalDomain.cs` — F-P2-1.
- `backend/Api/Endpoints/Projects/Project/Start/StartProjectCommandHandler.cs` — F-P2-2.
- `modules/cli/backend/Cli/Endpoints/Projects/ProjectDto.cs` — F-P2-3.
- `backend/Api.Hosting/Services/EnvironmentTargetResolver/EnvironmentTargetResolver.cs` — F-P2-4.
- `/Users/milo/milodev/gits/web/backend/.env.example` (if present) for the .env scaffolding.

## Findings to address

### F-P2-1 — InternalDomain emits malformed string when hostname unresolved

**File:** `backend/Api.Data/InternalDomain.cs:12` (`ResolveHostName` at L39).

**Problem:** `ResolveHostName()` returns `Project?.Server?.HostName ?? string.Empty`. Empty hostname → `subdomain.sites..compozerr.com` (silent malformed value passed downstream).

**Fix:** When hostname can't be resolved (env not loaded, env server is null, project server is null), `Value` should either (a) throw `InvalidOperationException("InternalDomain hostname cannot be resolved")` or (b) return an explicit `null`/sentinel that callers must handle. Pick (a) — explicit failure is what every caller wants; nobody should be using a malformed domain string.

Verify the existing `InternalDomainValueTests` still passes for the happy path and add a test for the unresolved-hostname path asserting the throw.

### F-P2-2 — `StartProjectCommandHandler` doesn't pass env name on fan-out

**File:** `backend/Api/Endpoints/Projects/Project/Start/StartProjectCommandHandler.cs:18`

**Problem:** Project-wide start loops env IDs and calls `StartProjectAsync(command.ProjectId, command.EnvironmentName)` with a null `EnvironmentName` per env. Preview env VMs need the env name in the VM identifier (`HostingApi.cs:221` `BuildVmIdentifier`). Result: preview envs don't start.

**Fix:** Carry env name and type through the fan-out. For prod env → call `StartProjectAsync(projectId, null)` or the project-level start. For preview env → call `StartProjectAsync(projectId, env.Name)` (or whatever the API contract requires for previews). Use the env's `Type` field to decide.

Add a test: project with one prod env on Server A and one preview env "feature-x" on Server B → verify Server A receives a start for the project, Server B receives a start for `(projectId, "feature-x")`.

Note: same shape may apply to `StopProjectCommandHandler` — review and fix if so.

### F-P2-3 — `ProjectDto.ServerId` no fallback to project hint

**File:** `modules/cli/backend/Cli/Endpoints/Projects/ProjectDto.cs:22,43`

**Problem:** `ProjectDto.ServerId` is derived from production env's ServerId. If a project has zero envs or production env has `ServerId == null` (legacy backfill state), L43 throws `InvalidOperationException`. Defeats the downgrade-safety promise of schema option B (the `Project.ServerId` hint should cover exactly these transition cases).

**Fix:** `effectiveServerId = productionEnvServerId ?? prodEnv?.ServerId ?? project.ServerId`. If still null after all three, return `null` (deprecated field, nullable for legacy) instead of throwing.

Add a test for ProjectDto construction: (a) project with no envs → `serverId = project.ServerId hint`; (b) project with prod env but null `ServerId` → `serverId = project.ServerId hint`; (c) project + prod env with ServerId → `serverId = env.ServerId`.

### F-P2-4 — Resolver silent fallback for env-scoped domains

**File:** `backend/Api.Hosting/Services/EnvironmentTargetResolver/EnvironmentTargetResolver.cs:56`

**Problem:** Env-scoped domain (non-null `ProjectEnvironmentId`) with missing/null env server falls through to production/project hint. Silent misrouting — the domain explicitly says "use env X's server" and gets routed to the prod env instead.

**Fix:** When `domain.ProjectEnvironmentId is not null`:
- If env doesn't exist → throw `InvalidOperationException("Domain references missing environment")`.
- If env exists but `env.ServerId is null` → throw `InvalidOperationException("Domain's environment has no allocated server")`.

Only fall back to production env when `domain.ProjectEnvironmentId IS null` (project-level/apex domain). This is the existing intended fallback, just gated more strictly.

Add a test: `GetServerForDomainAsync_FailsWhenEnvScopedDomainEnvHasNoServer`.

### F-P2-5 — Provision `backend/.env` so Api.Tests runs

**File:** `backend/.env`

**Problem:** Round-1 verification was incomplete — `dotnet build` failed with `MSB3030: Could not copy backend/.env because it was not found`, and `dotnet test backend/Base.sln` only ran Github.Tests + Cli.Tests; `Api.Tests` was skipped.

**Fix (workspace-local, NOT committed):**
- If `/Users/milo/milodev/gits/web/backend/.env` exists in the main worktree, copy it: `cp /Users/milo/milodev/gits/web/backend/.env backend/.env`. (Same pattern `compozerr dev` uses.)
- If it doesn't, scaffold from `backend/.env.example` (if it exists) with placeholder values that satisfy the build (real values not required for build/test).
- `.env` is gitignored — verify with `git check-ignore -v backend/.env` before staging anything. Do NOT commit it.

After this, `dotnet build backend/Base.sln` should produce 0 errors, and `dotnet test backend/Base.sln` should include `Api.Tests` in its run.

## Acceptance criteria

- [ ] All four P2s addressed with file:line citations in commit messages.
- [ ] `backend/.env` present in worktree, not committed.
- [ ] `dotnet build backend/Base.sln` clean — zero errors.
- [ ] `dotnet test backend/Base.sln --logger 'console;verbosity=minimal'` runs `Api.Tests` AND green.
- [ ] All four new tests (one per P2 finding) pass.
- [ ] Existing tests still pass — `InternalDomainValueTests`, `EnvironmentTargetResolverTests`, `CreateEnvironmentCommandHandlerTests` (incl. f-b1's additions).
- [ ] `cd frontend && npm run typecheck && npm run build` pass.

## Verification

- Run the targeted tests in isolation via `--filter`.
- Run the full suite end-to-end and capture the output for the run log.
- Spot-check via psql that the migration's backfill SQL runs cleanly against a `compozerr dev` DB (apply migration; query envs without ServerId).

Follow `CHILD.md`. `/code-review` self-review before signaling done.
