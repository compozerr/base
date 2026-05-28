# Subtask s-b3 — Wire contracts + frontend picker UI

**Intent:** Make the per-env server visible on the wire and selectable in the create-environment flow across **web + CLI binary + hosting repo**. The branch is rebased on Spec A, so `GET /v1/servers`, `IServerVisibilityFilter`, `AllocatedServerSummary`, and env-side `server` field are already present. Your job is to (a) make `EnvironmentDto.ServerId` writable from the create-env flow, (b) make the env-side `server` field source from `env.Server` (not `project.Server`), (c) add the picker UI, (d) regenerate the openapi-qraft client, (e) propagate the per-env wire change to the CLI binary at `/Users/milo/milodev/gits/cli`, and (f) extend the hosting repo's `HostingEnvironmentsResponse` to carry per-env server identity.

**Scope: 3 repos.** You work in /web (run worktree, branched), /cli (current branch, commit locally only — DO NOT push), /hosting (current branch, commit locally only — DO NOT push). Use `git status` in each repo to confirm cleanliness before starting. If either external repo has uncommitted work you didn't make, STOP and signal subtask-stuck with what you found.

**Read first, before writing anything:**

- `.orchestrate/round-1/plan.md` — full plan + assumption A1 (scope is web only), A6 (no CLI/hosting repo edits).
- `.orchestrate/round-1/skill-notes.md` §5 — wire-contract impact + recommended Spec A coordination.
- The prior Spec A run's REPORT.md at `/Users/milo/milodev/gits/web/.worktrees/multi-server-support-for-compozerr-plus--20260528-102504/.orchestrate/REPORT.md` — esp. the "Puppeteer verification BLOCKED" section. The same constraint applies here.
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/environments/index.tsx` — the existing create-environment modal you're extending.
- `backend/Api/Endpoints/Projects/Environments/Get/GetEnvironmentsResponse.cs` and Handler — where the new `Server` field on env rows goes.

**Suggested files (in /web):**

- `backend/Api/Endpoints/Projects/Environments/Get/GetEnvironmentsResponse.cs` — Spec A already added a `Server: AllocatedServerSummary?` field. After s-b2 the source of the field is `env.Server.Location` (not `project.Server.Location`). Verify the handler matches (s-b2 may have already done this).
- `backend/Api/Endpoints/Projects/Environments/Get/GetEnvironmentsCommandHandler.cs` — confirm it includes `env.Server.Location` (after s-b2's read-site fanout it should already).
- `modules/cli/backend/Cli/Endpoints/Projects/ProjectDto.cs` — keep returning a derived `ServerId` from the production env's ServerId (fallback `Project.ServerId` hint, option B). Add a new field `EnvironmentServers: List<(Guid EnvironmentId, Guid ServerId, string ServerHostName)>` (or similar) so the CLI binary can route per-env without parsing names.
- The create-env endpoint command (already updated in s-b1 to accept `ServerId?`) — verify swagger shows the parameter.
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/environments/index.tsx` — add a server-picker step to the create-environment modal. Use `api.v1.getServers.useQuery()` (the Spec A endpoint, AdminOnly-aware via `IServerVisibilityFilter`). Send `ServerId` in the create-env mutation. Default selection: the project's current production env's server.
- Display the env's allocated server name + location in the env list rows — Spec A already set up the field plumbing; just confirm the rendered source.
- Project detail page — Spec A added a server display sourced from `project.server`. Adjust it to source from the production env's `server` (or display all envs' servers) so the value remains accurate after migration.
- Regenerate `frontend/src/generated/`:
  ```bash
  cd frontend && npx openapi-qraft --plugin tanstack-query-react --plugin openapi-typescript http://localhost:1235/swagger/v1/swagger.json --clean -o src/generated --export-type --properties-required-by-default --default-non-nullable
  ```
  Verify the diff is scoped: (a) new `serverId` parameter on create-environment, (b) the new `EnvironmentServers` field on `ProjectDto`. If unrelated drift appears, STOP and report.

**Suggested files (in /Users/milo/milodev/gits/cli):**

- `src/utils/api/types/environment-types.ts` — add `serverId: string | null` to `EnvironmentDto`. Add a corresponding type if needed for the per-env server summary on `ProjectDto`.
- `src/utils/api/types/project-dto.ts` — mark `serverId` as `@deprecated` (still returned; derived from production env). Add the new per-env list field.
- Locate the `compozerr env create` command (likely under `src/commands/` or `src/utils/commands/`); add an interactive server-picker prompt that queries the backend's `/v1/servers` and lets the user pick (default = project's prod env's server).
- Locate the `compozerr project create` command; add the same picker step *before* the project is created (creates a project with a chosen prod env server). Pass the picked `ServerId` in the create-project request body — that may require extending the create-project backend command/handler too; if so, do it in /web concurrently.
- Commit locally in `/Users/milo/milodev/gits/cli` (`git add -A; git commit -m "feat(cli): per-env server picker + serverId wire field"`). Do NOT push.

**Suggested files (in /Users/milo/milodev/gits/hosting):**

- `manager/types/HostingEnvironmentsResponse.d.ts` — add `serverHostName: string` (or `serverId: string`) per row.
- `manager/services/deployment-api-client.ts:158` — update consumer to handle the new field (defensive — likely already passes through opaquely; verify).
- `manager/__tests__/domain-service.test.ts:604` — update test fixtures to include the new field.
- Commit locally in `/Users/milo/milodev/gits/hosting` (`git add -A; git commit -m "feat(hosting): per-env server identity in HostingEnvironmentsResponse"`). Do NOT push.

**Acceptance criteria:**

- [ ] `dotnet build backend/Base.sln` clean.
- [ ] `cd frontend && npm run typecheck` passes (zero new errors — pre-existing main-branch errors are unaffected; document them by name in commit message).
- [ ] `cd frontend && npm run build` succeeds.
- [ ] Backend test suite green.
- [ ] `git diff --stat frontend/src/generated/` shows changes ONLY in expected files (`schema.ts`, `services/V1Service.ts`) and the diff content is scoped to the changes above. No unrelated drift.
- [ ] Create-env modal shows server picker (populated by `GET /v1/servers`); selecting a server creates env on that server (verify by querying DB after create).
- [ ] Env list rows display the env's server name + location.
- [ ] `ProjectDto.ServerId` (CLI wire-out) continues to return a valid Guid (derived from prod env's ServerId or Project.ServerId hint).
- [ ] **CLI repo**: `/Users/milo/milodev/gits/cli` has a local commit with the picker + new wire field. Run `cd /Users/milo/milodev/gits/cli && bun run typecheck` (or whatever the CLI's typecheck command is — check its package.json) to confirm it compiles. Report the commit hash in your done signal.
- [ ] **Hosting repo**: `/Users/milo/milodev/gits/hosting` has a local commit with the type extension + test updates. Run its test suite if present. Report the commit hash in your done signal.
- [ ] **Do NOT push** to either external repo. Leave the commits for the user to PR.

**Verification (mandatory):**

- Run `compozerr dev` from your worktree.
- Open `http://localhost:1234`, log in (GitHub OAuth — see prior Spec A REPORT for the BLOCKED-on-puppeteer caveat).
- If Puppeteer can't log in (no seeded password user; no dev bypass), follow the **STOP-and-report** rule: signal `VERIFICATION_BLOCKED` with what you completed; do NOT silently skip.
- If you can log in: walk the create-env flow with two different servers (use psql to spin up a fake second server seed if needed); confirm the env DB row has the picked `ServerId`.

**Notes / gotchas:**

- **Spec A is already in this branch** (run branch was rebased on Spec A tip). `GET /v1/servers` exists with AdminOnly filtering; `AllocatedServerSummary` exists; env-side `Server` field on `GetEnvironmentsResponse` exists. After s-b2's read-site fanout, the source of that field should already be `env.Server.Location`. Your work is incremental on top.
- **Generated client regen drift.** The whole `frontend/src/generated/` tree gets rewritten. Stage the regen as a separate commit step so reviewers can diff cleanly. Confirm only relevant changes via `git diff --stat`.
- **Cross-repo commit hygiene.** Each external repo gets a separate commit. The commit message in `/cli` references this run's branch name; the commit in `/hosting` does the same. The web repo's commit can `Co-developed-by` if you want, but don't over-engineer the messages.
- **If either external repo is on a feature branch you don't recognize**, STOP and signal subtask-stuck. The user's working state in unrelated repos is not yours to touch.
- **If the existing Spec A project detail page server display becomes misleading** (because it reads `project.server` which after s-b2 still has the option-B hint, but envs may now diverge from it), update the display to source from env-level. Don't leave a "wrong-but-plausible" value on the page.

**Signal when done.** Run `/code-review` self-review before signaling.
