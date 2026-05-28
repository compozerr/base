# Subtask f-a1 — Spec A auth + allocator fixes

**Intent:** Address the Codex P2 findings on Spec A's branch BEFORE rebasing Spec B on top: (1) close the AdminOnly visibility leak on the project-detail server display, (2) close the same leak on the environments-list server display, (3) replace the allocator's unhandled `.First()` on empty filtered list with a controlled error, (4) backfill the missing allocator tests (gates d + e).

**Branch:** `task/multi-server-support-for-compozerr-plus--20260528-102504` (Spec A tip). You are NOT on Spec B. The Spec B resolver, env-side ServerId column, and per-env allocation do not exist in this branch.

**Working tree:** `/Users/milo/milodev/gits/web/.worktrees/spec-b-per-env-server-allocation-20260528-151212/.sessions/f-a1` (your worktree is branched off Spec A's tip, not Spec B). Confirm before editing: `git log --oneline -1` should show `d35f80ca orchestrate: round 1 report` (or its descendants) at the top.

**Read first:**

- `.orchestrate/subtask.md` (this file).
- `/Users/milo/milodev/gits/web/.worktrees/spec-b-per-env-server-allocation-20260528-151212/.orchestrate/round-2/codex-findings/spec-a-review.md` — the source of truth for the findings.
- `backend/Api.Data/Services/IServerVisibilityFilter.cs` + impl in `backend/Api/Services/ServerVisibility/` — understand the filter's signature.
- `modules/cli/backend/Cli/Endpoints/Projects/AllocateServer_ProjectCreatedEventHandler.cs` — the file with the `.First()` bug.
- `backend/Api.Tests/Endpoints/Server/AllocateServer_ProjectCreatedEventHandlerTests.cs` — existing test shape.

## Findings to address

### F1 [P2] Project detail server-visibility leak

**Files:**
- `backend/Api.Data/Repositories/ProjectRepository.cs:39`
- `backend/Api/Endpoints/Projects/Project/Get/GetProjectRoute.cs:60`

**Problem:** Project detail eager-loads `Project.Server.Location` and returns `project.Server.HostName`/location to any project member, even if the server is AdminOnly. The current user might be a project member but not a platform admin.

**Fix:** Apply `IServerVisibilityFilter` after loading the project but BEFORE projecting the response. If the server is not visible to the current user, set `Server = null` in the response (do NOT throw — graceful degradation; project should still load).

### F2 [P2] Environments list server-visibility leak

**Files:**
- `backend/Api.Data/Repositories/ProjectEnvironmentRepository.cs:41`
- `backend/Api/Endpoints/Projects/Environments/Get/GetEnvironmentsCommandHandler.cs:35`

**Problem:** Same shape — env list returns `env.server` joined through `Project.Server`. Validator only checks project ownership.

**Fix:** Same pattern — filter the projected `Server` summary per env. If the server is AdminOnly and the user isn't admin, set that env's `Server` field to null. Other envs in the same list may keep their (non-AdminOnly) server.

### F3 [P2] Allocator `.First()` crash on empty filtered list

**File:** `modules/cli/backend/Cli/Endpoints/Projects/AllocateServer_ProjectCreatedEventHandler.cs:35`

**Problem:** After applying `IServerVisibilityFilter`, the list can be empty (non-admin in a region with only AdminOnly servers). `.First()` throws — surfaces as a 500 to the user.

**Fix:** Replace `.First()` with `FirstOrDefault()`. If null: throw a domain validation exception (or use the existing project-creation failure path) with a message like `"No server available in location <location> for your account. Contact an administrator."`. The existing `ProjectCreatedEvent` flow must surface this as a 4xx (validation), not a 5xx.

### F4 [P3] Backfill allocator tests for gates (d) and (e)

**File:** `backend/Api.Tests/Endpoints/Server/AllocateServer_ProjectCreatedEventHandlerTests.cs`

**Problem:**
- Gate (d): no test for non-admin fallback when only Private+AdminOnly are available — Private should be picked.
- Gate (e): no test for the "only AdminOnly servers exist for a non-admin" error path — exactly the path F3 fixes.

**Fix:** Add two test cases:
1. Seeded servers: 1× Private + 1× AdminOnly in same location. Non-admin caller. Expect: Private chosen.
2. Seeded servers: only 1× AdminOnly in caller's region. Non-admin caller. Expect: controlled failure (whatever exception type F3 uses), NOT an `InvalidOperationException` from `.First()`, NOT a 500. Assert the error type and a non-empty message.

## Acceptance criteria

- [ ] `dotnet build backend/Base.sln` clean — zero errors. (You may need a `backend/.env`; copy from `/Users/milo/milodev/gits/web/backend/.env` if it exists, or scaffold from `backend/.env.example`.)
- [ ] `dotnet test backend/Api.Tests/Api.Tests.csproj` green — including the two new tests.
- [ ] `dotnet test backend/Base.sln` runs (not skipped) and green.
- [ ] Grep gate: `rg 'env\.Server\b|project\.Server\b' backend/Api/Endpoints --glob '!**/Migrations/**'` — every hit returns through `IServerVisibilityFilter` or is in a filter call-site. No raw projections returning Server-shaped data without the filter.
- [ ] Commit message references the Codex finding IDs (F1, F2, F3, F4).
- [ ] **Do NOT touch any Spec B files** — no resolver, no env.ServerId, no per-env allocation. This branch doesn't know about Spec B.

## Verification

- Run `compozerr dev` from your worktree (the f-a1 worktree). Log in via GitHub. As a non-admin org member of a project where the project's server is set to `AdminOnly = 3` (toggle one in psql: `UPDATE servers SET server_visibility = 3 WHERE id = '<X>'`), confirm:
  - `GET /v1/projects/<id>` returns the project but `server = null`.
  - `GET /v1/projects/<id>/environments` returns envs but rows whose servers are AdminOnly have `server = null`.
  - As platform admin (Identity role `"admin"`), the same calls DO return the server.

If Puppeteer is BLOCKED (no seeded password user, same as Spec A): document `VERIFICATION_BLOCKED` in your done report and proceed with the test-based proof; do not silently skip.

## Notes / gotchas

- `IServerVisibilityFilter` exists in this tree (Spec A added it). Re-read it before applying; the signature may take a `UserId` or use `CurrentUserAccessor`.
- The leaks are P2 (project member sees AdminOnly hostname), not P1 (random unauthenticated user sees it) — they're real but graceful nulls are acceptable; a hard 403 would over-react.
- Don't expand scope. P3 codegen nullability fix is a separate follow-up; don't touch it in this subtask.

Follow `CHILD.md` for commit/review/signal flow. Run `/code-review` self-review before signaling done.
