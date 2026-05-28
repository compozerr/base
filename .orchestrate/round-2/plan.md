# Round 2 plan — fix-up after Codex adversarial reviews

## Trigger

Round 1 shipped (Spec A + Spec B branches built, /cli #35 and /hosting #74 merged), then Codex code-reviewed both diffs. Both verdicts: **block**. See:

- [`codex-findings/spec-a-review.md`](codex-findings/spec-a-review.md) — 2× P2 auth leaks + 1× P2 crash + 2× P3 test gaps + 1× P3 codegen.
- [`codex-findings/spec-b-review.md`](codex-findings/spec-b-review.md) — 4× P1 + 4× P2.

Also: my Round-1 verification claim was incomplete — `Api.Tests` never ran because `backend/.env` was missing from the worktree, so all new tests added in s-b1/s-b2 went unverified end-to-end.

## Scope of this round

Fix all P1 + P2 findings from both reviews. Leave P3 (test gaps + codegen nullability) as follow-ups unless trivial to bundle.

**Branch targets** (preserves separate-PR review):

- Spec A's branch (`task/multi-server-...102504`) gets its own fixes — that branch's PR will carry the auth-leak + allocator-crash patches.
- Spec B's branch (this run worktree) gets its own fixes, after rebasing on the updated Spec A tip.

## Subtasks

### f-a1 — Spec A auth + allocator fixes

Target branch: `task/multi-server-support-for-compozerr-plus--20260528-102504` (Spec A tip).

Worktree: spawned manually via `git worktree add` off Spec A's branch (not via `spawn-subtask.sh`, because the script defaults to the run branch's HEAD = Spec B).

Scope: 3× P2 from Spec A review (visibility leaks on project + env; allocator crash on empty filtered list). Also tackles P3 test gaps that prove the allocator fix (gates d + e).

Spec: [`subtask-f-a1.md`](subtask-f-a1.md).

### f-b1 — Spec B P1 fixes

Target branch: `task/spec-b-per-env-server-allocation-20260528-151212` (this run branch), **after** rebasing on the new Spec A tip from f-a1.

Worktree: spawned via `spawn-subtask.sh` (the run branch is the right base).

Scope: 4× P1 from Spec B review.

Spec: [`subtask-f-b1.md`](subtask-f-b1.md).

### f-b2 — Spec B P2 fixes + `.env` for tests

Target branch: same as f-b1, after f-b1 merges.

Scope: 4× P2 from Spec B review + provision a `backend/.env` symlink/copy so `Api.Tests` actually runs end-to-end.

Spec: [`subtask-f-b2.md`](subtask-f-b2.md).

## Sequence

```
f-a1 (Spec A branch)
  ↓ merge into Spec A branch
rebase Spec B branch onto new Spec A tip
  ↓
f-b1 (Spec B branch)
  ↓ merge into Spec B branch
f-b2 (Spec B branch)
  ↓ merge into Spec B branch
re-run `dotnet build` + `dotnet test backend/Base.sln` end-to-end
update REPORT.md + open Spec A and Spec B PRs
```

## Risks

R1. **Rebase conflicts.** If Spec A's fixes touch any of the files Spec B also changed (resolver, GetEnvironmentsCommandHandler, etc.), the rebase of Spec B onto the new Spec A tip will conflict. Mitigation: f-a1's changes are scoped to a small set of files (filter application + `FirstOrDefault`); Spec B already routes through the resolver, so the overlap is on `GetEnvironmentsCommandHandler.cs` (Spec A adds the projection; Spec B updates the data source). Likely a 1-hunk conflict resolvable by hand.

R2. **Spec A fix changes the visibility filter shape.** If applying `IServerVisibilityFilter` requires plumbing a `UserId` into endpoints that don't currently take one, the change ripples. Mitigation: the filter already exists; this is wiring it through the read path. Inspect the filter's signature before f-a1 starts (child does this in its READ-FIRST phase).

R3. **Stripe compensation refactor in f-b1 changes broader handler shape.** Wrapping subscription+allocation+AddAsync in one scope may re-order the existing handler. Mitigation: child preserves existing test coverage; if any of `CreateEnvironmentCommandHandlerTests` breaks, the refactor went too wide.

## Acceptance criteria for the round

- All P1 and P2 findings resolved (file:line citations in commit messages).
- `dotnet build backend/Base.sln` clean — zero errors.
- `dotnet test backend/Base.sln` runs `Api.Tests` (not just Github+Cli) and shows green.
- Spec A's branch has the auth + allocator fixes; Spec B's branch is rebased and has the resolver/delete/stripe/picker fixes.
- REPORT.md updated to reflect the round-2 work.
