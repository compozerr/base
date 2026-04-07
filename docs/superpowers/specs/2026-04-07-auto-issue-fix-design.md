# Auto-issue-fix pipeline design

**Date**: 2026-04-07
**Scope**: `compozerr/cli`, `compozerr/web`, `compozerr/hosting`, new `compozerr/.github`
**Goal**: When `tomilodk` files an issue with a title starting `[Auto]` in any of the three repos, a Claude Code agent automatically opens a fix PR. A second Claude Code agent reviews that PR and either auto-merges (tests green, nothing critical) or posts inline GitHub suggestion comments for human triage. When the human applies any suggestion, the review/test cycle re-runs.

## Trigger criteria

```yaml
on:
  issues:
    types: [opened]
jobs:
  fix:
    if: >
      github.event.issue.user.login == 'tomilodk' &&
      startsWith(github.event.issue.title, '[Auto]')
```

Exact-match author, title prefix `[Auto]` (case-sensitive). Only `opened` — not `edited` or `reopened`. Re-trigger by closing and opening a new issue.

## Architecture

One source of truth, three thin callers:

```
compozerr/.github/.github/workflows/
  ├─ auto-issue-fix.yml      (reusable, called via workflow_call)
  └─ auto-pr-rerun.yml       (reusable, called via workflow_call)

compozerr/cli/.github/workflows/
  ├─ auto-issue-fix.yml      (caller, ~20 lines, language: bun)
  └─ auto-pr-rerun.yml       (caller, ~20 lines, language: bun)

compozerr/web/.github/workflows/
  ├─ auto-issue-fix.yml      (caller, language: dotnet)
  └─ auto-pr-rerun.yml       (caller, language: dotnet)

compozerr/hosting/.github/workflows/
  ├─ auto-issue-fix.yml      (caller, language: node)
  └─ auto-pr-rerun.yml       (caller, language: node)
```

When the auto-fix flow needs to evolve, the reusable workflows in `compozerr/.github` are the only thing that changes. The 6 caller files stay touched.

## Reusable workflow inputs

Both `auto-issue-fix.yml` and `auto-pr-rerun.yml` accept the same input set:

| Input | Type | Required | Default | Notes |
|---|---|---|---|---|
| `language` | string | yes | — | One of `bun`, `node`, `dotnet`. Selects the right setup action. |
| `language_version` | string | no | per-language | `9.0.200` for dotnet, `20` for node, `latest` for bun |
| `working_directory` | string | no | `.` | cwd for setup and tests |
| `pre_test_setup` | string | no | — | extra shell run before tests (e.g. `touch backend/.env`) |
| `test_command` | string | yes | — | shell command to run tests |

`secrets: inherit` is used by every caller so `CLAUDE_CODE_OAUTH_TOKEN` (and any per-repo test secrets) flow through automatically.

### Permissions block (declared on the calling job)

```yaml
permissions:
  contents: write
  pull-requests: write
  issues: write
  id-token: write
```

`id-token: write` is required by `anthropics/claude-code-action@v1` for OIDC.

## Reusable workflow: `auto-issue-fix.yml`

### Step sequence

1. `actions/checkout@v4` — `ref: main`, `fetch-depth: 0`, `token: ${{ secrets.GITHUB_TOKEN }}`
2. Configure git identity as `claude[bot] <claude[bot]@users.noreply.github.com>`
3. Conditional setup based on `language`:
   - `bun` → `oven-sh/setup-bun@v2` with `bun-version: ${{ inputs.language_version || 'latest' }}`
   - `node` → `actions/setup-node@v4` with `node-version: ${{ inputs.language_version || '20' }}`
   - `dotnet` → `actions/setup-dotnet@v1` with `dotnet-version: ${{ inputs.language_version || '9.0.200' }}`
4. Create local branch `auto-fix/issue-${{ github.event.issue.number }}` (`git checkout -b`). The fix agent will commit and push it.
5. **Run fix agent** (`anthropics/claude-code-action@v1`) — see prompt below
6. **Guard step**: if `/tmp/fix-result.txt` contains `GAVE_UP`, exit the job cleanly (skipping all later steps). Otherwise continue.
7. **Capture PR number** via `gh pr list --head auto-fix/issue-${{ github.event.issue.number }}`, write to `steps.pr.outputs.pr_number`. Fail the job if no PR was found (the fix agent should have created one).
8. Run `pre_test_setup` shell (if provided) inside `working_directory`
9. Run `test_command` inside `working_directory`. **Capture exit code into `steps.test.outputs.tests_passed` (`true`/`false`). Do NOT fail the job here** — we want the review agent to run regardless. Use `continue-on-error: true` and a follow-up bash step that reads `steps.test.outcome`.
10. **Run review agent** (`anthropics/claude-code-action@v1`, `continue-on-error: true`) — see prompt below. Receives the test output and `tests_passed` flag.
11. **Decision step** (bash, `if: always()`) — see logic below. Runs even if the review agent failed.

### Fix agent

**Allowed tools** (claude_args):
```
--allowedTools 'Edit,Write,Read,Glob,Grep,Bash(git add:*),Bash(git commit:*),Bash(git push:*),Bash(git status:*),Bash(git diff:*),Bash(gh issue view:*),Bash(gh issue comment:*),Bash(gh pr create:*),Bash(gh label create:*)'
```

Deliberately **no** `gh pr merge`, **no** `Bash(rm:*)`, **no** writes anywhere under `.github/`, **no** writes to `*.env*` or `secrets/*` (enforced via prompt + post-run guard step).

**Prompt** (passed to `claude-code-action` `prompt:` input):
```
REPO: ${{ github.repository }}
ISSUE: #${{ github.event.issue.number }}
TITLE: ${{ github.event.issue.title }}
BRANCH: auto-fix/issue-${{ github.event.issue.number }}

You are an autonomous fix agent. Read the issue body via `gh issue view`,
understand what's being asked, and try to implement a focused fix.

RULES:
- Read CLAUDE.md in the repo for conventions and follow them.
- Do NOT touch files under .github/, scripts/deploy*, or any *.env* file.
- Do NOT add documentation or refactor unrelated code.
- Smallest sensible diff that resolves the issue.

WHEN DONE (success path):
1. `git add` only the files you changed
2. `git commit -m "fix: <short summary> (#${{ github.event.issue.number }})"`
3. `git push -u origin auto-fix/issue-${{ github.event.issue.number }}`
4. `gh pr create --base main --head auto-fix/issue-${{ github.event.issue.number }} \
     --title "[Auto-fix] ${{ github.event.issue.title }}" \
     --body "Fixes #${{ github.event.issue.number }}\n\n<short explanation>" \
     --label auto-fix`
5. `gh issue comment ${{ github.event.issue.number }} --body "Working on this in PR #<pr-number>"`

WHEN YOU CANNOT PRODUCE A SENSIBLE FIX:
- Do NOT push anything.
- `gh issue comment ${{ github.event.issue.number }} --body "I tried but couldn't auto-fix this. Needs human attention."`
- Write the literal string `GAVE_UP` to /tmp/fix-result.txt
- exit
```

After the fix agent finishes, a small bash step checks `/tmp/fix-result.txt`:
- If it contains `GAVE_UP`, the workflow exits early with a neutral success — no test step, no review step, no decision step. Issue already has the comment.
- Otherwise, continue to test step.

### Review agent

**Allowed tools** (claude_args):
```
--allowedTools 'Read,Glob,Grep,Bash(gh pr view:*),Bash(gh pr diff:*),Bash(gh api:*),Bash(git diff:*),Write'
```

Deliberately **no** `Edit`, **no** `gh pr merge`, **no** `git push`, **no** `gh pr ready`. Review can only read and post comments.

**Prompt**:
```
REPO: ${{ github.repository }}
PR: #<pr-number-from-fix-agent-step>
TESTS_PASSED: ${{ steps.test.outputs.tests_passed }}
TEST_OUTPUT (last 200 lines): <captured from step 7>

Review this PR. Read the diff with `gh pr diff <pr-number>`.

Categorize each finding as:
- CRITICAL — security vulnerabilities, secrets leakage, data loss risks,
  broken core functionality, obvious crashes, regressions
- Non-critical — style, naming, minor improvements, "nice to have"

For each CRITICAL finding, post an INLINE GitHub suggestion comment using:

  gh api --method POST repos/${{ github.repository }}/pulls/<pr-number>/comments \
    -F body="<explanation>\n\n\`\`\`suggestion\n<replacement code>\n\`\`\`" \
    -F commit_id=<head-sha> \
    -F path=<file-path> \
    -F line=<line-number> \
    -F side=RIGHT

After posting all comments, write your final verdict to /tmp/review-result.txt:
  - The literal string "CLEAN" if no critical issues were found
  - The literal string "CRITICAL" if any critical issue was posted

Then exit. Do NOT attempt to merge anything.
```

### Decision step

```bash
TESTS_PASSED='${{ steps.test.outputs.tests_passed }}'
VERDICT=$(cat /tmp/review-result.txt 2>/dev/null || echo "CRITICAL")
PR_NUMBER='${{ steps.pr.outputs.pr_number }}'
ISSUE_NUMBER='${{ github.event.issue.number }}'

echo "tests=$TESTS_PASSED verdict=$VERDICT pr=$PR_NUMBER issue=$ISSUE_NUMBER"

if [[ "$TESTS_PASSED" == "true" && "$VERDICT" == "CLEAN" ]]; then
  gh pr merge "$PR_NUMBER" --squash --delete-branch
  gh issue comment "$ISSUE_NUMBER" --body "Auto-merged in #$PR_NUMBER"
  gh issue close "$ISSUE_NUMBER"
elif [[ "$TESTS_PASSED" == "true" && "$VERDICT" == "CRITICAL" ]]; then
  gh issue comment "$ISSUE_NUMBER" --body "PR #$PR_NUMBER ready — review agent flagged critical issues, see inline suggestions."
else
  gh pr ready "$PR_NUMBER" --undo  # convert to draft
  gh issue comment "$ISSUE_NUMBER" --body "Auto-fix attempt has failing tests — PR #$PR_NUMBER converted to draft."
fi
```

If the review agent crashed (no `/tmp/review-result.txt`), the fail-safe default is `CRITICAL`, which means the PR is left open with whatever happened, and the issue gets a "needs human review" comment.

### Capturing the PR number for the decision step

`claude-code-action@v1` doesn't provide a structured "PR number" output, so the workflow looks the PR up by branch name in step 7:

```yaml
- name: Capture PR number
  id: pr
  run: |
    PR=$(gh pr list \
      --head "auto-fix/issue-${{ github.event.issue.number }}" \
      --state open \
      --json number --jq '.[0].number')
    if [ -z "$PR" ]; then
      echo "No PR found for branch auto-fix/issue-${{ github.event.issue.number }}" >&2
      exit 1
    fi
    echo "pr_number=$PR" >> "$GITHUB_OUTPUT"
```

## Reusable workflow: `auto-pr-rerun.yml`

Same skeleton as `auto-issue-fix.yml` minus the fix agent:

1. `actions/checkout@v4` with `ref: ${{ github.head_ref }}`, `fetch-depth: 0`
2. Configure git identity
3. Conditional setup based on `language`
4. **Extract issue number** from the PR body via `gh pr view ${{ github.event.pull_request.number }} --json body --jq '.body' | grep -oE 'Fixes #[0-9]+' | head -1 | grep -oE '[0-9]+'`. Write to `steps.issue.outputs.issue_number`. Fail the step if not found (PR isn't really an auto-fix PR despite branch name).
5. Run `pre_test_setup` (if provided)
6. Run `test_command` with `continue-on-error: true`, capture `tests_passed`
7. Run **review agent** (`continue-on-error: true`) with same prompt as `auto-issue-fix.yml`. PR number is `${{ github.event.pull_request.number }}`.
8. **Decision step** (`if: always()`) — same three-branch logic as `auto-issue-fix.yml`. PR number is `${{ github.event.pull_request.number }}`; issue number is `${{ steps.issue.outputs.issue_number }}`.

## Per-repo caller workflows

Each repo gets two thin caller files. Naming and trigger filters are identical across the three repos.

### `compozerr/cli/.github/workflows/auto-issue-fix.yml`

```yaml
name: Auto Issue Fix
on:
  issues:
    types: [opened]

jobs:
  fix:
    if: github.event.issue.user.login == 'tomilodk' && startsWith(github.event.issue.title, '[Auto]')
    permissions:
      contents: write
      pull-requests: write
      issues: write
      id-token: write
      actions: read
    uses: compozerr/.github/.github/workflows/auto-issue-fix.yml@main
    with:
      language: bun
      pre_test_setup: bun install
      test_command: bun run typecheck && bun test
    secrets: inherit
```

(`test_command` matches the gates from the existing `cli/main.yml` CI — typecheck + test. Compile is intentionally omitted as it's slow and the existing PR-triggered CI runs it independently.)

### `compozerr/cli/.github/workflows/auto-pr-rerun.yml`

```yaml
name: Auto PR Rerun
on:
  pull_request:
    types: [synchronize]

jobs:
  rerun:
    if: startsWith(github.head_ref, 'auto-fix/')
    permissions:
      contents: write
      pull-requests: write
      issues: write
      id-token: write
      actions: read
    uses: compozerr/.github/.github/workflows/auto-pr-rerun.yml@main
    with:
      language: bun
      pre_test_setup: bun install
      test_command: bun run typecheck && bun test
    secrets: inherit
```

### `compozerr/web/.github/workflows/auto-issue-fix.yml`

```yaml
name: Auto Issue Fix
on:
  issues:
    types: [opened]

jobs:
  fix:
    if: github.event.issue.user.login == 'tomilodk' && startsWith(github.event.issue.title, '[Auto]')
    permissions:
      contents: write
      pull-requests: write
      issues: write
      id-token: write
      actions: read
    uses: compozerr/.github/.github/workflows/auto-issue-fix.yml@main
    with:
      language: dotnet
      pre_test_setup: touch backend/.env
      test_command: dotnet test backend/Base.sln
    secrets: inherit
```

### `compozerr/web/.github/workflows/auto-pr-rerun.yml`

Same as above but with `on: pull_request: types: [synchronize]` and the rerun reusable workflow.

### `compozerr/hosting/.github/workflows/auto-issue-fix.yml`

```yaml
name: Auto Issue Fix
on:
  issues:
    types: [opened]

jobs:
  fix:
    if: github.event.issue.user.login == 'tomilodk' && startsWith(github.event.issue.title, '[Auto]')
    permissions:
      contents: write
      pull-requests: write
      issues: write
      id-token: write
      actions: read
    uses: compozerr/.github/.github/workflows/auto-issue-fix.yml@main
    with:
      language: node
      working_directory: manager
      test_command: npm install && npm test
    secrets: inherit
```

### `compozerr/hosting/.github/workflows/auto-pr-rerun.yml`

Same but with the rerun trigger and reusable workflow.

## Failure modes

| Scenario | Outcome |
|---|---|
| Fix agent can't produce a fix | Issue gets comment "couldn't auto-fix, needs human attention". No PR. Workflow exits cleanly (neutral). |
| Fix agent succeeds, tests fail | PR opens, gets converted to draft. Issue gets comment "auto-fix attempt has failing tests". |
| Fix agent succeeds, tests pass, review CLEAN | PR squash-merged, branch deleted, issue commented + closed. |
| Fix agent succeeds, tests pass, review CRITICAL | PR stays open with inline suggestion comments. Issue commented "ready for human review". |
| Review agent crashes | Fail-safe: treated as CRITICAL. PR stays open as-is. Issue commented. |
| Reusable workflow itself errors | Standard GitHub Actions failure surfaced in the issue's caller workflow run. Issue gets no comment (visible failure on the Actions tab is enough for the operator). |

## Interaction with existing `cli` workflows

The cli repo already has:
- `claude.yml` — `@claude` mention handler. **Keep as-is.** Different trigger, no overlap.
- `claude-code-review.yml` — runs on every `pull_request: opened, synchronize` and creates `hotfix/` branches for critical issues. **Modify**: add an `if:` filter that skips PRs whose head branch starts with `auto-fix/` so it doesn't double-review the auto-fix PRs.
- `ci-failure-auto-fix.yml` — independent feature. **Keep as-is.**
- `main.yml` — independent feature. **Keep as-is.**

## Prerequisites

1. ✅ `compozerr/.github` repo created (private)
2. ✅ `CLAUDE_CODE_OAUTH_TOKEN` available to all 3 repos
3. **Reusable-workflow access** on `compozerr/.github`:
   ```
   gh api -X PUT \
     repos/compozerr/.github/actions/permissions/access \
     -f access_level=organization
   ```
4. **`auto-fix` label** in each of the 3 repos:
   ```
   gh label create auto-fix -R compozerr/cli -c "#0E8A16" -d "Auto-generated fix PR"
   gh label create auto-fix -R compozerr/web -c "#0E8A16" -d "Auto-generated fix PR"
   gh label create auto-fix -R compozerr/hosting -c "#0E8A16" -d "Auto-generated fix PR"
   ```
5. `tomilodk` is the GitHub login of the test/bot user that opens `[Auto]` issues. Confirmed by user.

## Rollout sequence

To minimize blast radius, ship in stages on a test branch first.

1. **Step A**: Push reusable workflows to a `test` branch in `compozerr/.github`. Configure org-level access (prerequisite #3).
2. **Step B**: Add caller workflows to **`cli` only**, pinned to `compozerr/.github/.github/workflows/...@test`.
3. **Step C — Happy path test on cli**: Open a `[Auto]` issue from `tomilodk` describing a trivial fix (e.g. "add a one-line comment to README"). Verify:
   - Workflow run triggered
   - Branch `auto-fix/issue-<n>` created with a commit
   - PR opened with `auto-fix` label
   - Tests ran and reported
   - Review agent posted CLEAN verdict
   - PR squash-merged, branch deleted
   - Issue commented and closed
4. **Step D — Critical path test on cli**: Open a `[Auto]` issue that the fix agent will tackle but the review agent should flag as critical (e.g. "remove the input sanitization in foo.ts"). Verify:
   - PR opened
   - Tests pass
   - Review agent posts inline suggestion comment
   - PR stays open
   - Apply the suggestion via the GitHub UI
   - `auto-pr-rerun` workflow triggers on `synchronize`
   - Re-review either passes (merges) or posts new comments
5. **Step E — Give-up path test on cli**: Open a vague `[Auto]` issue with no actionable content. Verify:
   - Issue gets "couldn't auto-fix" comment
   - No PR opened
   - Workflow run completes cleanly
6. **Step F**: Add caller workflows to `web`, repeat Steps C–E.
7. **Step G**: Add caller workflows to `hosting`, repeat Steps C–E.
8. **Step H**: Promote the reusable workflows from `test` to `main` in `compozerr/.github`. Update all caller `uses:` lines from `@test` to `@main`.

## Verification approach

Implementation happens in a git worktree off `main` in `compozerr/web` (where this spec lives), so I can test on a sandboxed branch before merging anything to main. Each test cycle is verified end-to-end via the GitHub CLI:

- `gh run watch <run-id>` to monitor workflow execution live
- `gh pr view <pr-number>` to verify PR state, label, comments
- `gh issue view <issue-number>` to verify status comments
- `gh run view <run-id> --log-failed` to debug failures

The implementation phase is not "done" until all three repos pass the happy path, critical path, and give-up path tests. After verification, the changes are merged to `main` in each repo and the worktree is cleaned up.

## Out of scope

- Retrying failed fix attempts (fix agent gets one shot per issue)
- Retrying failed test runs (test failures convert PR to draft, no retry)
- Branch protection rules (this design doesn't depend on them)
- Claude Code action version upgrades (pinned to `@v1`)
- Modifying `cli/main.yml`, `web/deploy.yml`, or `hosting/deploy-manager.yml` (they keep their existing triggers)
- Replacing `cli/claude-code-review.yml` (only adds an `if:` filter to skip `auto-fix/*` branches)
