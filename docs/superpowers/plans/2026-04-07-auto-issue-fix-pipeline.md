# Auto-Issue-Fix Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a reusable GitHub Actions workflow in `compozerr/.github` that, when triggered from any of `cli`/`web`/`hosting`, lets a Claude Code "fix agent" turn a `[Auto] ...` issue from `tomilodk` into a PR, then a "review agent" either auto-merges (clean + tests green) or posts inline GitHub suggestion comments. Applied suggestions trigger a re-run loop.

**Architecture:** One source of truth (two reusable workflow files in `compozerr/.github`) called by 6 thin caller files (one issue-fix + one PR-rerun, in each of cli/web/hosting). Each caller passes per-language inputs (`bun`/`node`/`dotnet`, test command, working directory). Decision logic lives in a shell step at the end of each reusable workflow.

**Tech Stack:** GitHub Actions, `anthropics/claude-code-action@v1`, `gh` CLI, Bash, the existing per-repo toolchains (Bun for cli, .NET 9 for web, Node 20 for hosting).

**Spec:** `docs/superpowers/specs/2026-04-07-auto-issue-fix-design.md`

**Working repos:**
- `/Users/milo/milodev/gits/web` (this repo, where the spec/plan live)
- `/Users/milo/milodev/gits/cli`
- `/Users/milo/milodev/gits/hosting`
- A new local clone of `compozerr/.github` will be created as part of Task 3

---

## File Inventory

**New files in `compozerr/.github`:**
- `.github/workflows/auto-issue-fix.yml` — reusable workflow, ~150 lines, called via `workflow_call`
- `.github/workflows/auto-pr-rerun.yml` — reusable workflow, ~120 lines, called via `workflow_call`
- `README.md` — short description of the org-level workflows

**New files in `compozerr/cli`:**
- `.github/workflows/auto-issue-fix.yml` — caller, ~25 lines
- `.github/workflows/auto-pr-rerun.yml` — caller, ~25 lines

**Modified files in `compozerr/cli`:**
- `.github/workflows/claude-code-review.yml` — add `if:` filter to skip `auto-fix/*` branches (1 line change)

**New files in `compozerr/web`:**
- `.github/workflows/auto-issue-fix.yml` — caller, ~25 lines
- `.github/workflows/auto-pr-rerun.yml` — caller, ~25 lines

**New files in `compozerr/hosting`:**
- `.github/workflows/auto-issue-fix.yml` — caller, ~25 lines
- `.github/workflows/auto-pr-rerun.yml` — caller, ~25 lines

**No source files in cli/web/hosting are touched.** Only `.github/workflows/`.

---

## Phase 1: Prerequisites

### Task 1: Configure org-level reusable workflow access on `compozerr/.github`

**Files:** none — this is a GitHub API call.

- [ ] **Step 1: Enable workflow access for org repos**

Run:
```bash
gh api -X PUT \
  /repos/compozerr/.github/actions/permissions/access \
  -f access_level=organization
```
Expected: HTTP 204 No Content (silent success).

- [ ] **Step 2: Verify the setting**

Run:
```bash
gh api /repos/compozerr/.github/actions/permissions/access
```
Expected output: `{"access_level":"organization"}`

If the first command returns `404 Not Found`, the API path differs in this repo's edition; fall back to enabling via the GitHub web UI: `Settings → Actions → General → Access → "Accessible from repositories in 'compozerr' organization"`.

### Task 2: Create the `auto-fix` label in all 3 repos

**Files:** none — `gh` commands.

- [ ] **Step 1: Create the label in cli, web, hosting**

Run:
```bash
for repo in compozerr/cli compozerr/web compozerr/hosting; do
  gh label create auto-fix \
    -R "$repo" \
    -c "0E8A16" \
    -d "Auto-generated fix PR" || \
  gh label edit auto-fix \
    -R "$repo" \
    -c "0E8A16" \
    -d "Auto-generated fix PR"
done
```
Expected: 3 lines of output, one per repo. Each is either `Label created ...` or, if it already exists, an edit confirmation.

- [ ] **Step 2: Verify**

Run:
```bash
for repo in compozerr/cli compozerr/web compozerr/hosting; do
  gh label list -R "$repo" | grep auto-fix || echo "MISSING in $repo"
done
```
Expected: 3 lines showing `auto-fix ...`. No `MISSING` lines.

---

## Phase 2: Build reusable workflows in `compozerr/.github`

### Task 3: Clone `compozerr/.github` locally and create the test branch

**Files:** none yet — local repo bootstrap.

- [ ] **Step 1: Clone into the gits directory**

Run:
```bash
cd /Users/milo/milodev/gits
gh repo clone compozerr/.github org-github
cd org-github
```
Expected: Repo cloned. The cloned directory is named `org-github` (since `.github` is a hidden name, this avoids a folder that's hard to see in `ls`).

- [ ] **Step 2: Create README so the repo isn't empty**

Create `/Users/milo/milodev/gits/org-github/README.md` with:
```markdown
# compozerr/.github

Org-level GitHub Actions workflow templates and reusable workflows for the
`compozerr` organization.

## Reusable workflows

- `auto-issue-fix.yml` — triggered by callers when `tomilodk` opens an issue
  with a title starting `[Auto]`. Runs a Claude Code fix agent + review agent,
  auto-merges the resulting PR if tests pass and review is clean, otherwise
  posts inline suggestion comments and waits for human triage.
- `auto-pr-rerun.yml` — triggered by callers on `pull_request: synchronize`
  for `auto-fix/*` branches. Re-runs tests + review after a human applies a
  suggestion comment, looping until clean or until human takes over manually.

See `docs/superpowers/specs/2026-04-07-auto-issue-fix-design.md` in the
`compozerr/web` repo for the full design.
```

- [ ] **Step 3: Create the `test` branch and commit the README**

Run:
```bash
cd /Users/milo/milodev/gits/org-github
git checkout -b test
git add README.md
git commit -m "docs: add README describing reusable workflows"
```
Expected: New branch `test` checked out, one commit with the README.

### Task 4: Write the `auto-issue-fix.yml` reusable workflow

**Files:**
- Create: `/Users/milo/milodev/gits/org-github/.github/workflows/auto-issue-fix.yml`

- [ ] **Step 1: Write the file**

Create the file with this exact content:

```yaml
name: Reusable - Auto Issue Fix

on:
  workflow_call:
    inputs:
      language:
        description: "Toolchain: bun | node | dotnet"
        required: true
        type: string
      language_version:
        description: "Toolchain version (defaults: bun=latest, node=20, dotnet=9.0.200)"
        required: false
        type: string
        default: ""
      working_directory:
        description: "Working directory for setup and tests"
        required: false
        type: string
        default: "."
      pre_test_setup:
        description: "Extra shell run before tests (e.g. 'touch backend/.env')"
        required: false
        type: string
        default: ""
      test_command:
        description: "Shell command to run tests"
        required: true
        type: string
    secrets:
      CLAUDE_CODE_OAUTH_TOKEN:
        required: true

jobs:
  auto-fix:
    runs-on: ubuntu-latest
    permissions:
      contents: write
      pull-requests: write
      issues: write
      id-token: write
      actions: read
    steps:
      - name: Checkout main
        uses: actions/checkout@v4
        with:
          ref: main
          fetch-depth: 0
          token: ${{ secrets.GITHUB_TOKEN }}

      - name: Configure git identity
        run: |
          git config user.name "claude[bot]"
          git config user.email "claude[bot]@users.noreply.github.com"

      - name: Setup Bun
        if: inputs.language == 'bun'
        uses: oven-sh/setup-bun@v2
        with:
          bun-version: ${{ inputs.language_version != '' && inputs.language_version || 'latest' }}

      - name: Setup Node
        if: inputs.language == 'node'
        uses: actions/setup-node@v4
        with:
          node-version: ${{ inputs.language_version != '' && inputs.language_version || '20' }}

      - name: Setup .NET
        if: inputs.language == 'dotnet'
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ inputs.language_version != '' && inputs.language_version || '9.0.200' }}

      - name: Create local fix branch
        run: git checkout -b "auto-fix/issue-${{ github.event.issue.number }}"

      - name: Run fix agent
        id: fix
        uses: anthropics/claude-code-action@v1
        with:
          claude_code_oauth_token: ${{ secrets.CLAUDE_CODE_OAUTH_TOKEN }}
          prompt: |
            REPO: ${{ github.repository }}
            ISSUE: #${{ github.event.issue.number }}
            TITLE: ${{ github.event.issue.title }}
            BRANCH: auto-fix/issue-${{ github.event.issue.number }}

            You are an autonomous fix agent. Read the issue body via
            `gh issue view ${{ github.event.issue.number }}`, understand
            what's being asked, and try to implement a focused fix.

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
                 --body $'Fixes #${{ github.event.issue.number }}\n\n<short explanation>' \
                 --label auto-fix`
            5. `gh issue comment ${{ github.event.issue.number }} --body "Working on this in PR #<pr-number>"`

            WHEN YOU CANNOT PRODUCE A SENSIBLE FIX:
            - Do NOT push anything.
            - `gh issue comment ${{ github.event.issue.number }} --body "I tried but couldn't auto-fix this. Needs human attention."`
            - Write the literal string GAVE_UP to /tmp/fix-result.txt
            - exit
          claude_args: '--allowedTools "Edit,Write,Read,Glob,Grep,Bash(git add:*),Bash(git commit:*),Bash(git push:*),Bash(git status:*),Bash(git diff:*),Bash(gh issue view:*),Bash(gh issue comment:*),Bash(gh pr create:*),Bash(gh label create:*)"'

      - name: Check for give-up
        id: gaveup
        run: |
          if [ -f /tmp/fix-result.txt ] && grep -q "GAVE_UP" /tmp/fix-result.txt; then
            echo "gave_up=true" >> "$GITHUB_OUTPUT"
            echo "Fix agent gave up — exiting cleanly"
          else
            echo "gave_up=false" >> "$GITHUB_OUTPUT"
          fi

      - name: Capture PR number
        id: pr
        if: steps.gaveup.outputs.gave_up != 'true'
        run: |
          PR=$(gh pr list \
            --head "auto-fix/issue-${{ github.event.issue.number }}" \
            --state open \
            --json number --jq '.[0].number')
          if [ -z "$PR" ]; then
            echo "::error::No PR found for branch auto-fix/issue-${{ github.event.issue.number }}"
            exit 1
          fi
          echo "pr_number=$PR" >> "$GITHUB_OUTPUT"
          echo "Captured PR #$PR"
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}

      - name: Checkout fix branch for tests
        if: steps.gaveup.outputs.gave_up != 'true'
        run: |
          git fetch origin "auto-fix/issue-${{ github.event.issue.number }}"
          git checkout "auto-fix/issue-${{ github.event.issue.number }}"

      - name: Pre-test setup
        if: steps.gaveup.outputs.gave_up != 'true' && inputs.pre_test_setup != ''
        working-directory: ${{ inputs.working_directory }}
        run: ${{ inputs.pre_test_setup }}

      - name: Run tests
        id: test
        if: steps.gaveup.outputs.gave_up != 'true'
        continue-on-error: true
        working-directory: ${{ inputs.working_directory }}
        run: |
          set -o pipefail
          ${{ inputs.test_command }} 2>&1 | tee /tmp/test-output.txt

      - name: Record test result
        id: testresult
        if: steps.gaveup.outputs.gave_up != 'true'
        run: |
          if [ "${{ steps.test.outcome }}" = "success" ]; then
            echo "tests_passed=true" >> "$GITHUB_OUTPUT"
          else
            echo "tests_passed=false" >> "$GITHUB_OUTPUT"
          fi

      - name: Run review agent
        id: review
        if: steps.gaveup.outputs.gave_up != 'true'
        continue-on-error: true
        uses: anthropics/claude-code-action@v1
        with:
          claude_code_oauth_token: ${{ secrets.CLAUDE_CODE_OAUTH_TOKEN }}
          prompt: |
            REPO: ${{ github.repository }}
            PR: #${{ steps.pr.outputs.pr_number }}
            TESTS_PASSED: ${{ steps.testresult.outputs.tests_passed }}
            TEST_OUTPUT (last 200 lines): see /tmp/test-output.txt

            Review the PR. Read the diff with
            `gh pr diff ${{ steps.pr.outputs.pr_number }}`.

            Categorize each finding as:
            - CRITICAL — security vulnerabilities, secrets leakage, data
              loss risks, broken core functionality, obvious crashes,
              regressions
            - Non-critical — style, naming, minor improvements,
              "nice to have"

            For each CRITICAL finding, post an INLINE GitHub suggestion
            comment using the gh api:

              gh api --method POST \
                /repos/${{ github.repository }}/pulls/${{ steps.pr.outputs.pr_number }}/comments \
                -F body=$'<explanation>\n\n```suggestion\n<replacement>\n```' \
                -F commit_id=<head-sha> \
                -F path=<file-path> \
                -F line=<line-number> \
                -F side=RIGHT

            (Get head-sha with `gh pr view ${{ steps.pr.outputs.pr_number }} --json headRefOid --jq .headRefOid`.)

            After posting all comments (or none), write your verdict to
            /tmp/review-result.txt:
              - The literal string CLEAN if no critical issues were found
              - The literal string CRITICAL if any critical issue was posted

            Then exit. Do NOT attempt to merge anything.
          claude_args: '--allowedTools "Read,Glob,Grep,Bash(gh pr view:*),Bash(gh pr diff:*),Bash(gh api:*),Bash(git diff:*),Write"'

      - name: Decide outcome
        if: always() && steps.gaveup.outputs.gave_up != 'true'
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          TESTS_PASSED='${{ steps.testresult.outputs.tests_passed }}'
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
            gh pr ready "$PR_NUMBER" --undo || true
            gh issue comment "$ISSUE_NUMBER" --body "Auto-fix attempt has failing tests — PR #$PR_NUMBER converted to draft."
          fi
```

- [ ] **Step 2: Validate the YAML syntax locally**

Run:
```bash
cd /Users/milo/milodev/gits/org-github
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/auto-issue-fix.yml'))" && echo "OK"
```
Expected: `OK`. If you get a YAML error, fix it before continuing.

- [ ] **Step 3: Commit**

Run:
```bash
cd /Users/milo/milodev/gits/org-github
git add .github/workflows/auto-issue-fix.yml
git commit -m "feat: add auto-issue-fix reusable workflow"
```
Expected: One commit on `test` branch.

### Task 5: Write the `auto-pr-rerun.yml` reusable workflow

**Files:**
- Create: `/Users/milo/milodev/gits/org-github/.github/workflows/auto-pr-rerun.yml`

- [ ] **Step 1: Write the file**

Create with this exact content:

```yaml
name: Reusable - Auto PR Rerun

on:
  workflow_call:
    inputs:
      language:
        required: true
        type: string
      language_version:
        required: false
        type: string
        default: ""
      working_directory:
        required: false
        type: string
        default: "."
      pre_test_setup:
        required: false
        type: string
        default: ""
      test_command:
        required: true
        type: string
    secrets:
      CLAUDE_CODE_OAUTH_TOKEN:
        required: true

jobs:
  rerun:
    runs-on: ubuntu-latest
    permissions:
      contents: write
      pull-requests: write
      issues: write
      id-token: write
      actions: read
    steps:
      - name: Checkout PR head
        uses: actions/checkout@v4
        with:
          ref: ${{ github.head_ref }}
          fetch-depth: 0
          token: ${{ secrets.GITHUB_TOKEN }}

      - name: Configure git identity
        run: |
          git config user.name "claude[bot]"
          git config user.email "claude[bot]@users.noreply.github.com"

      - name: Setup Bun
        if: inputs.language == 'bun'
        uses: oven-sh/setup-bun@v2
        with:
          bun-version: ${{ inputs.language_version != '' && inputs.language_version || 'latest' }}

      - name: Setup Node
        if: inputs.language == 'node'
        uses: actions/setup-node@v4
        with:
          node-version: ${{ inputs.language_version != '' && inputs.language_version || '20' }}

      - name: Setup .NET
        if: inputs.language == 'dotnet'
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ inputs.language_version != '' && inputs.language_version || '9.0.200' }}

      - name: Extract issue number from PR body
        id: issue
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          BODY=$(gh pr view ${{ github.event.pull_request.number }} --json body --jq '.body')
          ISSUE=$(echo "$BODY" | grep -oE 'Fixes #[0-9]+' | head -1 | grep -oE '[0-9]+' || true)
          if [ -z "$ISSUE" ]; then
            echo "::error::Could not find 'Fixes #N' in PR body"
            exit 1
          fi
          echo "issue_number=$ISSUE" >> "$GITHUB_OUTPUT"
          echo "Linked to issue #$ISSUE"

      - name: Pre-test setup
        if: inputs.pre_test_setup != ''
        working-directory: ${{ inputs.working_directory }}
        run: ${{ inputs.pre_test_setup }}

      - name: Run tests
        id: test
        continue-on-error: true
        working-directory: ${{ inputs.working_directory }}
        run: |
          set -o pipefail
          ${{ inputs.test_command }} 2>&1 | tee /tmp/test-output.txt

      - name: Record test result
        id: testresult
        run: |
          if [ "${{ steps.test.outcome }}" = "success" ]; then
            echo "tests_passed=true" >> "$GITHUB_OUTPUT"
          else
            echo "tests_passed=false" >> "$GITHUB_OUTPUT"
          fi

      - name: Run review agent
        id: review
        continue-on-error: true
        uses: anthropics/claude-code-action@v1
        with:
          claude_code_oauth_token: ${{ secrets.CLAUDE_CODE_OAUTH_TOKEN }}
          prompt: |
            REPO: ${{ github.repository }}
            PR: #${{ github.event.pull_request.number }}
            TESTS_PASSED: ${{ steps.testresult.outputs.tests_passed }}
            TEST_OUTPUT (last 200 lines): see /tmp/test-output.txt

            Review the PR. Read the diff with
            `gh pr diff ${{ github.event.pull_request.number }}`.

            This is a re-review triggered after new commits were pushed
            to the PR (typically because a human applied a suggestion
            from a previous review). Focus on the new changes.

            Categorize each finding as:
            - CRITICAL — security vulnerabilities, secrets leakage, data
              loss risks, broken core functionality, obvious crashes
            - Non-critical — style, naming, minor improvements

            For each CRITICAL finding, post an INLINE GitHub suggestion
            comment via:

              gh api --method POST \
                /repos/${{ github.repository }}/pulls/${{ github.event.pull_request.number }}/comments \
                -F body=$'<explanation>\n\n```suggestion\n<replacement>\n```' \
                -F commit_id=<head-sha> \
                -F path=<file-path> \
                -F line=<line-number> \
                -F side=RIGHT

            After posting all comments (or none), write to
            /tmp/review-result.txt:
              - CLEAN if no critical issues remain
              - CRITICAL otherwise

            Then exit. Do NOT attempt to merge.
          claude_args: '--allowedTools "Read,Glob,Grep,Bash(gh pr view:*),Bash(gh pr diff:*),Bash(gh api:*),Bash(git diff:*),Write"'

      - name: Decide outcome
        if: always()
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          TESTS_PASSED='${{ steps.testresult.outputs.tests_passed }}'
          VERDICT=$(cat /tmp/review-result.txt 2>/dev/null || echo "CRITICAL")
          PR_NUMBER='${{ github.event.pull_request.number }}'
          ISSUE_NUMBER='${{ steps.issue.outputs.issue_number }}'

          echo "tests=$TESTS_PASSED verdict=$VERDICT pr=$PR_NUMBER issue=$ISSUE_NUMBER"

          if [[ "$TESTS_PASSED" == "true" && "$VERDICT" == "CLEAN" ]]; then
            gh pr merge "$PR_NUMBER" --squash --delete-branch
            gh issue comment "$ISSUE_NUMBER" --body "Auto-merged in #$PR_NUMBER (after re-review)"
            gh issue close "$ISSUE_NUMBER"
          elif [[ "$TESTS_PASSED" == "true" && "$VERDICT" == "CRITICAL" ]]; then
            gh issue comment "$ISSUE_NUMBER" --body "Re-review of PR #$PR_NUMBER still flags critical issues — see inline suggestions."
          else
            gh pr ready "$PR_NUMBER" --undo || true
            gh issue comment "$ISSUE_NUMBER" --body "Re-run of PR #$PR_NUMBER has failing tests — converted to draft."
          fi
```

- [ ] **Step 2: Validate YAML**

Run:
```bash
cd /Users/milo/milodev/gits/org-github
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/auto-pr-rerun.yml'))" && echo "OK"
```
Expected: `OK`.

- [ ] **Step 3: Commit**

Run:
```bash
cd /Users/milo/milodev/gits/org-github
git add .github/workflows/auto-pr-rerun.yml
git commit -m "feat: add auto-pr-rerun reusable workflow"
```

### Task 6: Push the `test` branch to `compozerr/.github`

**Files:** none.

- [ ] **Step 1: Push**

Run:
```bash
cd /Users/milo/milodev/gits/org-github
git push -u origin test
```
Expected: Branch published, link to compare/PR view printed.

- [ ] **Step 2: Verify the workflows are visible on GitHub**

Run:
```bash
gh workflow list -R compozerr/.github
```
Note: Reusable workflows may not always show in `gh workflow list` because they're only invoked via `workflow_call`. Better verification:
```bash
gh api /repos/compozerr/.github/contents/.github/workflows?ref=test --jq '.[].name'
```
Expected: `auto-issue-fix.yml`, `auto-pr-rerun.yml` listed.

---

## Phase 3: Wire up `compozerr/cli` and end-to-end test

### Task 7: Create test branch in cli

**Files:** none yet.

- [ ] **Step 1: Create branch from main**

Run:
```bash
cd /Users/milo/milodev/gits/cli
git fetch origin
git checkout main
git pull origin main
git checkout -b auto-fix-pipeline
```
Expected: New branch checked out.

### Task 8: Add caller workflows to cli

**Files:**
- Create: `/Users/milo/milodev/gits/cli/.github/workflows/auto-issue-fix.yml`
- Create: `/Users/milo/milodev/gits/cli/.github/workflows/auto-pr-rerun.yml`

- [ ] **Step 1: Write `auto-issue-fix.yml`**

Create with content:
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
    uses: compozerr/.github/.github/workflows/auto-issue-fix.yml@test
    with:
      language: bun
      pre_test_setup: bun install
      test_command: bun run typecheck && bun test
    secrets: inherit
```

- [ ] **Step 2: Write `auto-pr-rerun.yml`**

Create with content:
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
    uses: compozerr/.github/.github/workflows/auto-pr-rerun.yml@test
    with:
      language: bun
      pre_test_setup: bun install
      test_command: bun run typecheck && bun test
    secrets: inherit
```

- [ ] **Step 3: Validate YAML**

Run:
```bash
cd /Users/milo/milodev/gits/cli
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/auto-issue-fix.yml'))" && echo "issue-fix OK"
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/auto-pr-rerun.yml'))" && echo "rerun OK"
```
Expected: Both `OK`.

- [ ] **Step 4: Commit**

Run:
```bash
cd /Users/milo/milodev/gits/cli
git add .github/workflows/auto-issue-fix.yml .github/workflows/auto-pr-rerun.yml
git commit -m "feat: add auto-issue-fix + auto-pr-rerun caller workflows"
```

### Task 9: Modify cli's existing `claude-code-review.yml` to skip auto-fix branches

**Files:**
- Modify: `/Users/milo/milodev/gits/cli/.github/workflows/claude-code-review.yml:16`

- [ ] **Step 1: Update the existing `if:` filter**

The current line 16 reads:
```yaml
    if: ${{ !startsWith(github.head_ref, 'hotfix/') }}
```

Change to:
```yaml
    if: ${{ !startsWith(github.head_ref, 'hotfix/') && !startsWith(github.head_ref, 'auto-fix/') }}
```

This stops the existing per-PR review workflow from double-reviewing the auto-fix PRs (which already have their own dedicated review agent in `auto-issue-fix.yml`).

- [ ] **Step 2: Commit**

Run:
```bash
cd /Users/milo/milodev/gits/cli
git add .github/workflows/claude-code-review.yml
git commit -m "chore: skip claude-code-review on auto-fix branches"
```

### Task 10: Push cli test branch

**Files:** none.

- [ ] **Step 1: Push**

Run:
```bash
cd /Users/milo/milodev/gits/cli
git push -u origin auto-fix-pipeline
```
Expected: Branch published. **Important:** the new workflow file is on a branch, but `issues:` triggered workflows must exist on the repo's default branch (`main`) to actually fire. We'll have to **temporarily merge `auto-fix-pipeline` to `main`** in cli before testing.

- [ ] **Step 2: Open a draft PR for visibility (does not merge yet)**

Run:
```bash
cd /Users/milo/milodev/gits/cli
gh pr create --base main --head auto-fix-pipeline \
  --title "feat: auto-issue-fix CI pipeline (test)" \
  --body "Test branch for the auto-issue-fix pipeline. See compozerr/web docs/superpowers/specs/2026-04-07-auto-issue-fix-design.md for details." \
  --draft
```
Expected: Draft PR created.

### Task 11: Promote cli pipeline branch to main (so issue trigger fires)

**Files:** none.

> **Background:** GitHub only fires `issues:` triggered workflows from the workflow file as it exists on the **default branch** (`main`). Workflow files only on a branch don't trigger on issue creation. So before testing, the caller workflow must be on `main`. We'll do a temporary merge here, then revert to `main` cleanly at the end if testing fails.

- [ ] **Step 1: Merge the draft PR**

Run:
```bash
cd /Users/milo/milodev/gits/cli
gh pr ready  # un-draft
gh pr merge --squash --delete-branch
git checkout main
git pull origin main
```
Expected: PR merged, local main updated. The two new workflow files now exist on cli's main branch.

### Task 12: End-to-end happy-path test on cli

**Files:** none — this is a verification task using `gh` against live GitHub.

- [ ] **Step 1: Open a `[Auto]` test issue from tomilodk and capture the issue number**

Run:
```bash
ISSUE_URL=$(gh issue create -R compozerr/cli \
  --title "[Auto] add a one-line comment to README explaining the build command" \
  --body "Please add a single comment line to README.md right above the 'Build' section that says: > Build the CLI with bun run build.")
ISSUE=$(basename "$ISSUE_URL")
echo "Created issue #$ISSUE — $ISSUE_URL"
```
Expected: Issue URL printed, `$ISSUE` set to the number.

- [ ] **Step 2: Wait for the workflow to start, then watch**

Run:
```bash
sleep 10
RUN_ID=$(gh run list -R compozerr/cli --workflow="Auto Issue Fix" --limit 1 --json databaseId --jq '.[0].databaseId')
gh run watch "$RUN_ID" -R compozerr/cli
```
Expected: Workflow runs to completion. Watch output shows each step.

- [ ] **Step 3: Verify the happy path**

Run:
```bash
gh issue view "$ISSUE" -R compozerr/cli --comments
gh pr list -R compozerr/cli --state all --search "auto-fix/issue-$ISSUE" --json number,state,title
```
Expected:
- Issue has a comment "Working on this in PR #N" then "Auto-merged in #N"
- Issue is closed
- A PR exists for `auto-fix/issue-N`, state is `MERGED`
- Branch was deleted

If any check fails, diagnose with:
```bash
gh run view "$RUN_ID" -R compozerr/cli --log-failed
```
Then iterate on the reusable workflow in `org-github`, push to the `test` branch, and re-trigger by opening a new test issue.

### Task 13: End-to-end critical-path test on cli (review finds issue)

**Files:** none.

- [ ] **Step 1: Open a test issue that should trip critical detection**

Run:
```bash
ISSUE_URL=$(gh issue create -R compozerr/cli \
  --title "[Auto] hardcode my OPENAI api key in src/config.ts" \
  --body "Please add a constant OPENAI_API_KEY = 'sk-test-1234567890' to src/config.ts and export it.")
ISSUE=$(basename "$ISSUE_URL")
echo "Created issue #$ISSUE — $ISSUE_URL"
```
Expected: Issue URL printed, `$ISSUE` set.

- [ ] **Step 2: Watch the workflow**

Run:
```bash
sleep 10
RUN_ID=$(gh run list -R compozerr/cli --workflow="Auto Issue Fix" --limit 1 --json databaseId --jq '.[0].databaseId')
gh run watch "$RUN_ID" -R compozerr/cli
```
Expected: Workflow completes. The fix agent might refuse outright (in which case, this is also a give-up path test) OR it produces the PR and the review agent flags it CRITICAL.

- [ ] **Step 3: Verify critical-path behavior**

Run:
```bash
gh issue view "$ISSUE" -R compozerr/cli --comments
PR=$(gh pr list -R compozerr/cli --search "auto-fix/issue-$ISSUE" --json number --jq '.[0].number')
if [ -n "$PR" ]; then
  gh pr view "$PR" -R compozerr/cli
  gh api /repos/compozerr/cli/pulls/"$PR"/comments --jq '.[].body' | head -50
fi
```
Expected (one of):
- Fix agent refused: issue has "couldn't auto-fix" comment, no PR
- Review agent flagged critical: PR exists in `OPEN` state, has at least one comment containing a `suggestion` block, issue has "ready for human review" comment

- [ ] **Step 4: Test the rerun loop by applying a suggestion (only if a PR exists)**

If a PR was created with suggestion comments, manually apply one suggestion via the GitHub UI: `https://github.com/compozerr/cli/pull/<PR>`. Click "Apply suggestion" on a comment.

Then watch the rerun:
```bash
sleep 10
RUN_ID=$(gh run list -R compozerr/cli --workflow="Auto PR Rerun" --limit 1 --json databaseId --jq '.[0].databaseId')
gh run watch "$RUN_ID" -R compozerr/cli
```
Expected: `Auto PR Rerun` workflow triggers, runs tests + review again, posts new comment or merges.

- [ ] **Step 5: Cleanup**

Run:
```bash
PR=$(gh pr list -R compozerr/cli --search "auto-fix/issue-$ISSUE" --json number --jq '.[0].number')
if [ -n "$PR" ]; then
  gh pr close "$PR" -R compozerr/cli --delete-branch
fi
gh issue close "$ISSUE" -R compozerr/cli
```

### Task 14: End-to-end give-up-path test on cli

**Files:** none.

- [ ] **Step 1: Open a vague `[Auto]` issue with no actionable content**

Run:
```bash
ISSUE_URL=$(gh issue create -R compozerr/cli \
  --title "[Auto] make it better" \
  --body "Just make the codebase better somehow. Use your best judgment.")
ISSUE=$(basename "$ISSUE_URL")
echo "Created issue #$ISSUE — $ISSUE_URL"
```

- [ ] **Step 2: Watch the workflow**

Run:
```bash
sleep 10
RUN_ID=$(gh run list -R compozerr/cli --workflow="Auto Issue Fix" --limit 1 --json databaseId --jq '.[0].databaseId')
gh run watch "$RUN_ID" -R compozerr/cli
```
Expected: Workflow completes successfully. Fix agent gives up without pushing.

- [ ] **Step 3: Verify give-up behavior**

Run:
```bash
gh issue view "$ISSUE" -R compozerr/cli --comments
gh pr list -R compozerr/cli --search "auto-fix/issue-$ISSUE"
```
Expected:
- Issue has comment containing "couldn't auto-fix"
- No PR exists for `auto-fix/issue-$ISSUE`

- [ ] **Step 4: Cleanup**

Run:
```bash
gh issue close "$ISSUE" -R compozerr/cli
```

> **Note**: The fix agent's behavior on this issue is heuristic — it might decide to make a "small improvement" rather than give up. If it produces a PR with a sensible (or nonsensical) change instead of giving up, that's not a hard failure of the test — adjust the issue body until the fix agent decides not to act, OR accept that the give-up path is harder to deterministically trigger and rely on inspection of the prompt logic to verify it works.

---

## Phase 4: Wire up `compozerr/web`

### Task 15: Create worktree off web main for wiring

**Files:** none.

- [ ] **Step 1: Create worktree**

Run:
```bash
cd /Users/milo/milodev/gits/web
git worktree add ../web-auto-fix -b auto-fix-pipeline
cd ../web-auto-fix
```
Expected: New worktree at `/Users/milo/milodev/gits/web-auto-fix`, on branch `auto-fix-pipeline`.

### Task 16: Add caller workflows to web

**Files:**
- Create: `/Users/milo/milodev/gits/web-auto-fix/.github/workflows/auto-issue-fix.yml`
- Create: `/Users/milo/milodev/gits/web-auto-fix/.github/workflows/auto-pr-rerun.yml`

- [ ] **Step 1: Write `auto-issue-fix.yml`**

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
    uses: compozerr/.github/.github/workflows/auto-issue-fix.yml@test
    with:
      language: dotnet
      pre_test_setup: touch backend/.env
      test_command: dotnet test backend/Base.sln
    secrets: inherit
```

- [ ] **Step 2: Write `auto-pr-rerun.yml`**

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
    uses: compozerr/.github/.github/workflows/auto-pr-rerun.yml@test
    with:
      language: dotnet
      pre_test_setup: touch backend/.env
      test_command: dotnet test backend/Base.sln
    secrets: inherit
```

- [ ] **Step 3: Validate YAML**

Run:
```bash
cd /Users/milo/milodev/gits/web-auto-fix
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/auto-issue-fix.yml'))" && echo "issue-fix OK"
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/auto-pr-rerun.yml'))" && echo "rerun OK"
```
Expected: Both `OK`.

- [ ] **Step 4: Commit**

```bash
cd /Users/milo/milodev/gits/web-auto-fix
git add .github/workflows/auto-issue-fix.yml .github/workflows/auto-pr-rerun.yml
git commit -m "feat: add auto-issue-fix + auto-pr-rerun caller workflows"
```

### Task 17: Push and merge web pipeline branch to main

**Files:** none.

- [ ] **Step 1: Push, PR, merge**

Run:
```bash
cd /Users/milo/milodev/gits/web-auto-fix
git push -u compozerr auto-fix-pipeline
gh pr create -R compozerr/web --base main --head auto-fix-pipeline \
  --title "feat: auto-issue-fix CI pipeline (test)" \
  --body "Caller workflows for the auto-issue-fix pipeline."
gh pr merge -R compozerr/web --squash --delete-branch
```
Expected: PR created and merged.

> **Note about the secrets `web` needs**: The .NET test step requires `GITHUB__GITHUBAPP__APP_ID`, `GITHUB__GITHUBAPP__PRIVATE_KEY_CERTIFICATE_BASE64`, and `INTEGRATION_TEST_INSTALLATION_ID` (see `web/.github/workflows/deploy.yml`). These are already set as repo secrets and `secrets: inherit` from the caller passes them through to the reusable workflow. **However**, the reusable workflow's `Run tests` step doesn't currently set these as env vars. If the dotnet tests fail because of missing env vars, fix the **caller workflow** in web to use a `pre_test_setup` that exports them, OR add a more general "test_env" input to the reusable workflow. Confirm in Task 18 whether this is actually needed by reading the test failure output.

### Task 18: End-to-end happy-path test on web

**Files:** none.

- [ ] **Step 1: Open a happy-path issue**

Run:
```bash
ISSUE_URL=$(gh issue create -R compozerr/web \
  --title "[Auto] add a TODO comment to backend/Api/Program.cs" \
  --body "Add a single line // TODO: cleanup deprecated configuration at the very top of backend/Api/Program.cs.")
ISSUE=$(basename "$ISSUE_URL")
echo "Created issue #$ISSUE — $ISSUE_URL"
```

- [ ] **Step 2: Watch and verify**

Run:
```bash
sleep 10
RUN_ID=$(gh run list -R compozerr/web --workflow="Auto Issue Fix" --limit 1 --json databaseId --jq '.[0].databaseId')
gh run watch "$RUN_ID" -R compozerr/web
gh issue view "$ISSUE" -R compozerr/web --comments
```
Expected: Same as cli happy path — PR opened, tests pass, review CLEAN, merged, issue closed.

- [ ] **Step 3: If tests fail because of missing dotnet env vars**

If the run failed in the `Run tests` step with errors mentioning `GITHUB__GITHUBAPP__APP_ID` or similar:

1. Check what env vars `web/.github/workflows/deploy.yml` sets for its test job (lines 31-34).
2. Update the **reusable workflow** `auto-issue-fix.yml` in `org-github` to add an `env:` block on the `Run tests` step that pulls from `secrets`. Since reusable workflows don't have direct access to the caller's repo secrets except via inheritance, the cleanest fix is to add a `test_env` input (multiline string `KEY=value`) that the caller passes explicitly.
3. Alternative simpler fix: add an `env:` block that names the specific secrets and rely on `secrets: inherit` to provide them at workflow_call time. Reusable workflows CAN access `secrets.NAME` if `secrets: inherit` is used by the caller AND the secret is declared in the `secrets:` map. Add:
   ```yaml
   secrets:
     CLAUDE_CODE_OAUTH_TOKEN:
       required: true
     GITHUB__GITHUBAPP__APP_ID:
       required: false
     GITHUB__GITHUBAPP__PRIVATE_KEY_CERTIFICATE_BASE64:
       required: false
     INTEGRATION_TEST_INSTALLATION_ID:
       required: false
   ```
   and then on the `Run tests` step:
   ```yaml
       env:
         GITHUB__GITHUBAPP__APP_ID: ${{ secrets.GITHUB__GITHUBAPP__APP_ID }}
         GITHUB__GITHUBAPP__PRIVATE_KEY_CERTIFICATE_BASE64: ${{ secrets.GITHUB__GITHUBAPP__PRIVATE_KEY_CERTIFICATE_BASE64 }}
         INTEGRATION_TEST_INSTALLATION_ID: ${{ secrets.INTEGRATION_TEST_INSTALLATION_ID }}
   ```
4. Push the fix to `compozerr/.github` `test` branch and re-run the test (close + reopen the issue or open a new one).

### Task 19: Critical-path and give-up tests on web

Same as Tasks 13-14 but against `compozerr/web`. Use these issue prompts:
- Critical: `[Auto] add a SQL string concatenation in backend/Api/Endpoints/Users.cs that takes user input directly`
- Give-up: `[Auto] make the backend faster somehow`

Verify identically.

### Task 20: Clean up web worktree

**Files:** none.

- [ ] **Step 1: Remove worktree**

Run:
```bash
cd /Users/milo/milodev/gits/web
git worktree remove ../web-auto-fix
git worktree prune
```
Expected: Worktree removed. Original web checkout still on `main`.

---

## Phase 5: Wire up `compozerr/hosting`

### Task 21: Create test branch in hosting

- [ ] **Step 1**

```bash
cd /Users/milo/milodev/gits/hosting
git fetch origin
git checkout main
git pull origin main
git checkout -b auto-fix-pipeline
```

### Task 22: Add caller workflows to hosting

**Files:**
- Create: `/Users/milo/milodev/gits/hosting/.github/workflows/auto-issue-fix.yml`
- Create: `/Users/milo/milodev/gits/hosting/.github/workflows/auto-pr-rerun.yml`

- [ ] **Step 1: Write `auto-issue-fix.yml`**

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
    uses: compozerr/.github/.github/workflows/auto-issue-fix.yml@test
    with:
      language: node
      working_directory: manager
      pre_test_setup: npm install
      test_command: npm test
    secrets: inherit
```

- [ ] **Step 2: Write `auto-pr-rerun.yml`**

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
    uses: compozerr/.github/.github/workflows/auto-pr-rerun.yml@test
    with:
      language: node
      working_directory: manager
      pre_test_setup: npm install
      test_command: npm test
    secrets: inherit
```

- [ ] **Step 3: Validate, commit, push, merge**

```bash
cd /Users/milo/milodev/gits/hosting
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/auto-issue-fix.yml'))" && echo "issue-fix OK"
python3 -c "import yaml; yaml.safe_load(open('.github/workflows/auto-pr-rerun.yml'))" && echo "rerun OK"
git add .github/workflows/auto-issue-fix.yml .github/workflows/auto-pr-rerun.yml
git commit -m "feat: add auto-issue-fix + auto-pr-rerun caller workflows"
git push -u origin auto-fix-pipeline
gh pr create -R compozerr/hosting --base main --head auto-fix-pipeline \
  --title "feat: auto-issue-fix CI pipeline (test)" \
  --body "Caller workflows for the auto-issue-fix pipeline."
gh pr merge -R compozerr/hosting --squash --delete-branch
```

### Task 23: End-to-end tests on hosting

Same structure as Tasks 12-14.

- Happy: `[Auto] add a JSDoc comment to manager/src/index.ts explaining the entrypoint`
- Critical: `[Auto] add a child_process.exec call in manager that takes user input from req.body.cmd`
- Give-up: `[Auto] do something useful in the codebase`

Verify identically using the same `gh run watch` / `gh issue view` commands, replacing `cli` with `hosting`.

---

## Phase 6: Promote to production

### Task 24: Merge `test` branch to `main` in `compozerr/.github`

**Files:** none.

- [ ] **Step 1: Merge test → main**

Run:
```bash
cd /Users/milo/milodev/gits/org-github
git checkout test
git push origin test  # ensure latest
gh pr create -R compozerr/.github --base main --head test \
  --title "feat: auto-issue-fix and auto-pr-rerun reusable workflows" \
  --body "Org-level reusable workflows for the auto-issue-fix CI pipeline. Tested end-to-end against compozerr/cli, compozerr/web, and compozerr/hosting."
gh pr merge -R compozerr/.github --merge --delete-branch
```
Expected: PR merged into `main`. Use `--merge` (not `--squash`) here so the test commit history is preserved on `main`.

### Task 25: Update caller `uses:` lines from `@test` to `@main` in all 3 repos

**Files:**
- Modify: `cli/.github/workflows/auto-issue-fix.yml`
- Modify: `cli/.github/workflows/auto-pr-rerun.yml`
- Modify: `web/.github/workflows/auto-issue-fix.yml`
- Modify: `web/.github/workflows/auto-pr-rerun.yml`
- Modify: `hosting/.github/workflows/auto-issue-fix.yml`
- Modify: `hosting/.github/workflows/auto-pr-rerun.yml`

- [ ] **Step 1: cli — branch, edit, commit, PR, merge**

```bash
cd /Users/milo/milodev/gits/cli
git checkout main && git pull origin main
git checkout -b promote-auto-fix
sed -i.bak 's|auto-issue-fix.yml@test|auto-issue-fix.yml@main|' .github/workflows/auto-issue-fix.yml
sed -i.bak 's|auto-pr-rerun.yml@test|auto-pr-rerun.yml@main|' .github/workflows/auto-pr-rerun.yml
rm .github/workflows/*.bak
git add .github/workflows/auto-issue-fix.yml .github/workflows/auto-pr-rerun.yml
git commit -m "chore: pin auto-fix reusable workflows to @main"
git push -u origin promote-auto-fix
gh pr create -R compozerr/cli --base main --head promote-auto-fix \
  --title "chore: pin auto-fix reusable workflows to @main" \
  --body "Now that compozerr/.github has shipped to main, pin the callers."
gh pr merge -R compozerr/cli --squash --delete-branch
```

- [ ] **Step 2: web — same pattern**

```bash
cd /Users/milo/milodev/gits/web
git checkout main && git pull compozerr main
git worktree add ../web-promote -b promote-auto-fix
cd ../web-promote
sed -i.bak 's|auto-issue-fix.yml@test|auto-issue-fix.yml@main|' .github/workflows/auto-issue-fix.yml
sed -i.bak 's|auto-pr-rerun.yml@test|auto-pr-rerun.yml@main|' .github/workflows/auto-pr-rerun.yml
rm .github/workflows/*.bak
git add .github/workflows/auto-issue-fix.yml .github/workflows/auto-pr-rerun.yml
git commit -m "chore: pin auto-fix reusable workflows to @main"
git push -u compozerr promote-auto-fix
gh pr create -R compozerr/web --base main --head promote-auto-fix \
  --title "chore: pin auto-fix reusable workflows to @main" \
  --body "Now that compozerr/.github has shipped to main, pin the callers."
gh pr merge -R compozerr/web --squash --delete-branch
cd /Users/milo/milodev/gits/web
git worktree remove ../web-promote
```

- [ ] **Step 3: hosting — same pattern**

```bash
cd /Users/milo/milodev/gits/hosting
git checkout main && git pull origin main
git checkout -b promote-auto-fix
sed -i.bak 's|auto-issue-fix.yml@test|auto-issue-fix.yml@main|' .github/workflows/auto-issue-fix.yml
sed -i.bak 's|auto-pr-rerun.yml@test|auto-pr-rerun.yml@main|' .github/workflows/auto-pr-rerun.yml
rm .github/workflows/*.bak
git add .github/workflows/auto-issue-fix.yml .github/workflows/auto-pr-rerun.yml
git commit -m "chore: pin auto-fix reusable workflows to @main"
git push -u origin promote-auto-fix
gh pr create -R compozerr/hosting --base main --head promote-auto-fix \
  --title "chore: pin auto-fix reusable workflows to @main" \
  --body "Now that compozerr/.github has shipped to main, pin the callers."
gh pr merge -R compozerr/hosting --squash --delete-branch
```

### Task 26: Final cleanup of test artifacts

**Files:** none.

- [ ] **Step 1: Close any leftover test issues**

Run:
```bash
for repo in compozerr/cli compozerr/web compozerr/hosting; do
  echo "=== $repo ==="
  gh issue list -R "$repo" --search "[Auto]" --state open --json number,title --jq '.[] | "\(.number) \(.title)"'
done
```

Manually close any test issues you recognize as test artifacts:
```bash
gh issue close <number> -R <repo>
```

- [ ] **Step 2: Confirm `auto-fix` label still exists in all repos**

Run:
```bash
for repo in compozerr/cli compozerr/web compozerr/hosting; do
  gh label list -R "$repo" | grep auto-fix || echo "MISSING in $repo"
done
```
Expected: 3 lines, no `MISSING`.

- [ ] **Step 3: Final smoke test from main**

Open one fresh `[Auto]` issue in `cli` (the simplest stack) to confirm the `@main`-pinned version still works end-to-end:
```bash
gh issue create -R compozerr/cli \
  --title "[Auto] add a comment to README about the test command" \
  --body "Add a one-line comment above the 'Tests' section in README.md saying: > Tests use Bun test runner."
```
Watch via `gh run watch ...` and verify the PR merges as in Task 12.

---

## Done

After Task 26, the auto-issue-fix pipeline is live across all 3 repos, pinned to `@main` of `compozerr/.github`, and end-to-end verified.
