# Subtask s-b1 — Schema + backfill + allocator move

**Intent:** Add `ProjectEnvironment.ServerId` as a nullable FK to `Servers`, write an EF migration that backfills it from `Project.ServerId`, and move server allocation from the Project-created path to env-creation. Add tests for `CreateEnvironmentCommandHandler` (which has no coverage today). This is the foundation for the entire migration — do not touch read sites yet (that's s-b2's job).

**Read first, before writing anything:**

- `.orchestrate/round-1/plan.md` — full plan + assumptions.
- `.orchestrate/round-1/skill-notes.md` §3 (read-side strategy) and §4 (schema-migration shape, option B). Option B is **decided** — do not change the strategy without checking with the orchestrator.
- `backend/Api.Data/Project.cs` lines 31–32 — the existing `Project.ServerId?` + nav.
- `backend/Api.Data/ProjectEnvironment.cs` — the entity you're modifying.
- `backend/Api.Data/ApiDbContext.cs` lines 65–72 — where Project↔Server FK is configured today.
- `modules/cli/backend/Cli/Endpoints/Projects/AllocateServer_ProjectCreatedEventHandler.cs` — the allocator you're modifying.
- `backend/Api/Endpoints/Projects/Environments/Create/CreateEnvironmentCommand.cs` (+ Validator + Handler) — the command you're extending.

**Suggested files:**

- `backend/Api.Data/ProjectEnvironment.cs` — add `public ServerId? ServerId { get; set; }` and `public virtual Server? Server { get; set; }`.
- `backend/Api.Data/ApiDbContext.cs` — configure the new FK + index, mirroring the existing Project↔Server pattern.
- new EF migration: `dotnet ef migrations add AddServerIdToProjectEnvironment --project backend/Api.Data --startup-project backend/Api`. **The auto-generated migration body must be hand-edited to include the backfill SQL.** Backfill SQL (Postgres):
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
  Verify EF generates equivalent operations; if not, replace with `migrationBuilder.Sql(...)`.
- `modules/cli/backend/Cli/Endpoints/Projects/AllocateServer_ProjectCreatedEventHandler.cs` — when allocating for a newly-created project, also write `ServerId` onto the project's auto-created production env. Keep writing `Project.ServerId` (option B — hint).
- `backend/Api/Endpoints/Projects/Environments/Create/CreateEnvironmentCommand.cs` (+ Validator + Handler) — add optional `ServerId? ServerId` field. In the handler: if `ServerId` is non-null and the caller is allowed to see it (no visibility filter yet — Spec A not merged — so just verify the server exists and isn't deleted), assign it. Otherwise call the existing allocator-style helper to pick a server (factor the allocator's selection logic into a `IServerAllocator` if helpful, but a one-line method extraction is fine; do not over-engineer).
- new `modules/cli/backend/Cli.Tests/Features/Projects/CreateEnvironmentCommandHandlerTests.cs` — see acceptance criteria.

**Acceptance criteria:**

- [ ] `dotnet build backend/Base.sln` clean (zero warnings introduced).
- [ ] `dotnet test backend/Api.Tests/Api.Tests.csproj` green.
- [ ] `dotnet test modules/cli/backend/Cli.Tests/Cli.Tests.csproj` green.
- [ ] New tests cover at least: (a) `CreateEnvironment` with explicit `ServerId` writes that ServerId; (b) `CreateEnvironment` with null `ServerId` allocates one via the same logic the project-created allocator uses; (c) existing Stripe subscription compensation behavior is preserved (env-creation failure rolls back the subscription).
- [ ] Migration is reversible: `Down()` drops the FK + index + column without errors. (You don't need to actually run `dotnet ef migrations remove` — just verify the generated Down code.)
- [ ] `AllocateServer_ProjectCreatedEventHandler` still writes `Project.ServerId` (option B — kept as hint).
- [ ] When the migration runs against a DB with N projects, every active `ProjectEnvironment` has a non-null `ServerId` after migration. Verify by running migration against the dev DB and querying.
- [ ] **Do NOT modify any read-site outside the two files above.** Read-site fanout is s-b2's job.
- [ ] **Do NOT add `IServerVisibilityFilter` or `AllocatedServerSummary`.** Those are Spec A; out of scope here.
- [ ] **Do NOT regenerate `frontend/src/generated/`.** Wire changes go in s-b3.

**Verification (mandatory):**

- Run `compozerr dev` from this worktree; wait for migration to apply; query Postgres:
  ```bash
  docker exec $(docker ps -qf name=db-1) psql -U postgres -d myapp -c "SELECT COUNT(*) FROM \"ProjectEnvironments\" WHERE \"ServerId\" IS NULL;"
  ```
  Expected: 0 (after backfill) — or document if seeded DB legitimately has envs without project-level ServerId.

**Notes / gotchas:**

- **`ProjectEnvironment.ServerId` is nullable** in the schema (option B) — required (non-null) would block legacy/backfill envs. The non-null invariant is maintained operationally (allocator always writes one) and asserted in tests, not enforced by the column.
- The existing `AllocateServer_ProjectCreatedEventHandler` runs synchronously inside the Project create transaction (`domainEvent.Entity.ServerId = ...` *before* save). Keep your new env-side write in the same transaction.
- The `CreateEnvironmentCommandHandler` already creates Stripe subscriptions (commit `bbc17ed0`). Server allocation must happen AFTER subscription creation but BEFORE `AddAsync` — preserve that order so the existing try/catch+compensate pattern still rolls back cleanly.
- `ServerId` is a strongly-typed id (`IdBase<T>`); use the existing pattern from `Project.ServerId`.
- This subtask runs in its own session worktree; expect the dev DB to be a fresh one. If your worktree has its own Docker compose stack, you may need to free port 5432 or 1235 — see prior s-a2 child log for the pattern (`docker stop <other-worktree>-db-1`).

**Signal when done.** Follow the `CHILD.md` review/commit/signal flow exactly. Self-review with `/code-review` before signaling.
