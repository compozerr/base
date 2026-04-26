# Merge + Verify: Primary HTTP Service + Health-State Feature

> **For the next session:** This is a handoff plan. The previous session built the feature across two repos. Your job is to verify the code, open new PRs, merge in the right order, deploy both sides, and run a real-world smoke test on kurvo-backend.

## Background — what was built

Feature: declare `healthcheck.service` in `compozerr.json` so the hosting blue-green deploy targets the right docker-compose service, plus a recurring HTTP health check that surfaces an `Unhealthy` state in the Compozerr project UI.

**Specs / plans (in the web worktree):**
- `docs/superpowers/specs/2026-04-24-primary-service-and-health-check-design.md`
- `docs/superpowers/plans/2026-04-24-primary-service-and-health-check.md`

**Branches (both named `feature/primary-service-health`):**
- Web: worktree at `/Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health`, pushed to `compozerr/web` (remote name `compozerr`).
- Hosting: worktree at `/Users/milo/milodev/gits/hosting/.worktrees/feature-primary-service-health`, pushed to `compozerr/hosting` (remote name `origin`).

**Existing PRs from previous session:**
- compozerr/web#98 — **may show as merged on GitHub but main has been reverted by the user.** Treat as already closed; you'll open a new PR.
- compozerr/hosting#35 — open draft PR. May be reusable, or you can open a fresh one. Verify state before deciding.

## Phase 1: Verify the code is sound

- [ ] **Step 1.1: Confirm branches still exist and are pushed**

  ```bash
  git -C /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health log --oneline origin/main..HEAD 2>/dev/null || \
  git -C /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health log --oneline compozerr/main..HEAD
  git -C /Users/milo/milodev/gits/hosting/.worktrees/feature-primary-service-health log --oneline origin/main..HEAD
  ```

  Both should list 14+ feature commits. If either branch was deleted on the remote, push again with `git push <remote> -u feature/primary-service-health`.

- [ ] **Step 1.2: Run the test suites locally**

  ```bash
  # Hosting tests
  cd /Users/milo/milodev/gits/hosting/.worktrees/feature-primary-service-health
  npx vitest run manager/services/__tests__/blue-green-deploy.test.ts \
                 manager/services/__tests__/environment-health-service.test.ts \
                 manager/services/__tests__/http-probe.test.ts \
                 manager/services/__tests__/compozerr-config-service.test.ts
  ```

  Expected: 36+ tests pass. Pre-existing `docker-build-service.test.ts` failures (BuildLogsService constructor mocking) are unrelated — ignore them.

  ```bash
  # Web build + tests
  cd /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health
  dotnet build backend/Base.sln
  dotnet test backend/Api.Tests/Api.Tests.csproj --no-build
  ```

  Expected: build clean (warnings OK), 18/18 tests pass.

- [ ] **Step 1.3: Spot-check the two review-finding fixes from the previous session**

  Both are commits on the branches. Confirm they're present:

  ```bash
  # Hosting: docker compose ps now uses executeCommandInWorkingDirectoryAsync
  grep -n "executeCommandInWorkingDirectoryAsync" \
    /Users/milo/milodev/gits/hosting/.worktrees/feature-primary-service-health/manager/services/environment-health-service.ts
  ```

  Expected: line 30 contains `executeCommandInWorkingDirectoryAsync`.

  ```bash
  # Web: ConcurrentDictionary used in Hangfire job
  grep -n "ConcurrentDictionary" \
    /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health/backend/Api.Hosting/Jobs/ProjectEnvironmentHealthCheckJob.cs
  ```

  Expected: at least one match.

## Phase 2: Open new PRs

Web's PR #98 looks merged (main was reverted) so you need a fresh one. Hosting's #35 may still be valid — check first.

- [ ] **Step 2.1: Web — close #98 if still open and create a new PR**

  ```bash
  gh pr view 98 --repo compozerr/web --json state,mergedAt
  ```

  - If `state == OPEN`: close it with `gh pr close 98 --repo compozerr/web --comment "superseded — opening fresh PR after main revert"`.
  - If `state == MERGED`: ignore; just open a new one.

  Then create the new PR (replace the body section at the bottom of the previous PR's description if you can find it via `gh pr view 98 --json body`):

  ```bash
  cd /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health
  gh pr create --repo compozerr/web --base main --head feature/primary-service-health \
      --title "feat: primary-service declaration + health-state UI" \
      --body-file - <<'EOF'
  ## Summary

  Adds `healthcheck.service` to `compozerr.json`, letting projects declare their primary HTTP service. Surfaces a new `Unhealthy` state in the UI when the declared service stops responding.

  Companion PR on hosting: compozerr/hosting#<NEW-NUMBER>.

  Re-opening after the previous merge of #98 was reverted.

  See `docs/superpowers/specs/2026-04-24-primary-service-and-health-check-design.md` for the full design and `docs/superpowers/plans/2026-04-24-primary-service-and-health-check.md` for the implementation breakdown.
  EOF
  ```

  Record the new PR number.

- [ ] **Step 2.2: Hosting — verify or recreate #35**

  ```bash
  gh pr view 35 --repo compozerr/hosting --json state,headRefName
  ```

  - If `state == OPEN` and `headRefName == feature/primary-service-health`: reuse it. Mark it ready for review with `gh pr ready 35 --repo compozerr/hosting`.
  - Otherwise: open a fresh one with `gh pr create --repo compozerr/hosting --base main --head feature/primary-service-health --title "feat(deploy): blue-green targets healthcheck.service + env-health endpoint" --body-file -` and a body referencing the web PR.

## Phase 3: Merge in the right order

**Hosting must merge and deploy first** — the web Hangfire job calls the hosting `/projects/:pid/environments/:envId/health` endpoint, which only exists after the hosting deploy lands.

- [ ] **Step 3.1: Merge the hosting PR**

  ```bash
  gh pr merge <hosting-pr#> --repo compozerr/hosting --squash --delete-branch=false
  ```

  (Keep the branch alive in case a fix-up commit is needed during smoke testing.)

- [ ] **Step 3.2: Deploy the hosting manager**

  The merge to `main` with `manager/**` changes auto-triggers `.github/workflows/deploy-manager.yml`. **Watch the run**:

  ```bash
  gh run list --repo compozerr/hosting --workflow deploy-manager.yml --limit 3
  gh run watch <run-id> --repo compozerr/hosting
  ```

  If the auto-trigger doesn't fire within ~30s (e.g. paths filter mismatch), dispatch manually:

  ```bash
  gh workflow run deploy-manager.yml --repo compozerr/hosting --ref main
  ```

  Wait for the run to finish green. SSH-verify the new endpoint exists:

  ```bash
  ssh debian@fra1.compozerr.com "sudo journalctl -u manager --since '2 minutes ago' --no-pager | tail -30"
  # Look for restart logs + the /projects/:projectId/environments/:envId/health route being registered.
  ```

- [ ] **Step 3.3: Merge the web PR**

  ```bash
  gh pr merge <web-pr#> --repo compozerr/web --squash --delete-branch=false
  ```

- [ ] **Step 3.4: Deploy the web app**

  Web does NOT auto-deploy on merge — it deploys on tag push via `nbgv`. From a clone of the main worktree (NOT the feature worktree, since you tag against the merged main):

  ```bash
  cd /Users/milo/milodev/gits/web
  git fetch compozerr main && git checkout main && git pull compozerr main

  # Push main, tag, push tag — this triggers the production deploy
  git push compozerr main && nbgv tag && git push compozerr $(git describe --tags --abbrev=0)
  ```

  Watch the deploy:

  ```bash
  ssh debian@compozerr.com -p 2222 "docker logs main-backend-1 --since 2m | tail -50"
  # Wait for "Now listening on" and the EF migration AddProjectEnvironmentHealth to apply.
  ```

  Verify the column exists:

  ```bash
  ssh debian@compozerr.com -p 2222 'docker exec main-db-1 psql -U postgres2 -d myapp -c "\d api.\"ProjectEnvironments\"" | grep -E "HealthStatus|LastHealthCheckAtUtc|ConsecutiveHealthFailures"'
  ```

  Expected: three rows.

## Phase 4: Real-world smoke test (kurvo-backend)

The original bug reporter. The local repo lives at `/Users/milo/milodev/gits/blog`? No — kurvo-backend is at `https://github.com/tomilodk/kurvo` per the projects DB. Project ID `517924f8-30c4-4238-b826-e56b036fa688`. Clone it locally if you don't have it.

- [ ] **Step 4.1: Find the right service name**

  Look at the project's docker-compose.yml. The user's intent is the actual HTTP API service — likely `web` (port 8080) per `api.ProjectServices`. The wrong choice (e.g. `curves-engine` on port 3457) is what caused the original bug.

  ```bash
  # From the project's compozerr.json source repo:
  cat docker-compose.yml | grep -A 2 "^services:"
  ```

  Pick the service whose container actually answers HTTP requests on its primary port without needing a runtime mount/command beyond what's declared in compose.

- [ ] **Step 4.2: Add healthcheck config to kurvo-backend**

  Edit `compozerr.json` in the kurvo-backend repo:

  ```json
  {
    "type": "project",
    "name": "kurvo-backend",
    "healthcheck": {
      "service": "web",
      "path": "/health"
    }
  }
  ```

  (Adjust `service` and `path` to match what's actually exposed. If the API has no `/health` endpoint, use `/` and confirm it returns 2xx/3xx.)

  Commit + push to trigger a deploy.

- [ ] **Step 4.3: Watch the deploy log**

  Deployment ID will be visible at `https://compozerr.com/projects/517924f8-30c4-4238-b826-e56b036fa688/deployments/`.

  **What you want to see (the fix):**

  ```
  Attempting zero-downtime blue-green deploy
  Blue-green: service 'web' port 8080 → temp 18080, image: <real-image>
  Temp container healthy
  Traefik switched to temp port
  Main service healthy
  Traefik switched back to original port
  Zero-downtime blue-green deploy completed
  ```

  **What you should NOT see anymore (the original bug):**

  ```
  Blue-green: port 3457 → temp 13457, image: osrm/osrm-backend
  Temp container failed health check
  Blue-green failed, falling back to force-recreate
  ```

- [ ] **Step 4.4: Verify the periodic health check works**

  Within ~5 minutes of deploy completion (the recurring job runs every 5 min), the env should flip to `Healthy`:

  ```bash
  ssh debian@compozerr.com -p 2222 'docker exec main-db-1 psql -U postgres2 -d myapp -c "SELECT \"Id\", \"HealthStatus\", \"LastHealthCheckAtUtc\", \"ConsecutiveHealthFailures\" FROM api.\"ProjectEnvironments\" WHERE \"ProjectId\" = '\''517924f8-30c4-4238-b826-e56b036fa688'\'';"'
  ```

  Expected: `HealthStatus = 1` (Healthy) and `LastHealthCheckAtUtc` updated within the last few minutes.

- [ ] **Step 4.5: Verify the UI renders correctly**

  Open https://compozerr.com/projects/517924f8-30c4-4238-b826-e56b036fa688 — the project list and detail page should render Healthy with no regressions. Look for the per-env "checked Ns ago" text (only visible when env is Unhealthy, but also peek at the env list to confirm no layout breaks).

- [ ] **Step 4.6: Force an outage — verify Unhealthy detection**

  SSH into the kurvo-backend VM (`compozerr ssh -e prod` from `/Users/milo/milodev/gits/blog` if that's wired up, or `compozerr ssh` from the kurvo project dir) and stop the primary container:

  ```bash
  sudo docker stop kurvo-backend-web-1   # or whatever the container name is
  ```

  Wait ~10–15 minutes (2 consecutive 5-minute failures × debounce). The DB `HealthStatus` should flip to `2` (Unhealthy), and the project list at `https://compozerr.com/projects` should render the red `AlertTriangle` badge with tooltip.

- [ ] **Step 4.7: Restore — verify recovery**

  ```bash
  sudo docker start kurvo-backend-web-1
  ```

  Within ~5 minutes, `HealthStatus` should return to `Healthy` and the badge should clear.

## Phase 5: Wrap-up

- [ ] **Step 5.1: Delete the feature branches**

  ```bash
  gh api -X DELETE repos/compozerr/web/git/refs/heads/feature/primary-service-health
  gh api -X DELETE repos/compozerr/hosting/git/refs/heads/feature/primary-service-health
  ```

- [ ] **Step 5.2: Clean up local worktrees**

  ```bash
  git -C /Users/milo/milodev/gits/web worktree remove .worktrees/feature-primary-service-health
  git -C /Users/milo/milodev/gits/hosting worktree remove .worktrees/feature-primary-service-health
  ```

- [ ] **Step 5.3: Report back to the user**

  Quick summary: PRs merged, deploys green, kurvo-backend deploy log shows the fixed blue-green flow, HealthStatus flipped correctly during outage and recovered. Note any deviations from the expected behavior.

## If something fails

- **Hosting deploy fails on the GitHub workflow** → check the run logs (`gh run view <id> --log`). Most likely tsc errors from a transitive change. Fix on the branch, push, re-merge.
- **Migration fails on production** → connect via SSH to compozerr.com:2222, inspect logs (`docker logs main-backend-1`). The migration is purely additive (3 nullable/defaulted columns) and shouldn't conflict.
- **Blue-green still logs the old warning** → the wrong `healthcheck.service` was set in compozerr.json (mismatched the docker-compose service name). Verify the name with `docker compose ps --format json` on the project VM.
- **HealthStatus never updates** → check the Hangfire dashboard at `https://compozerr.com/jobs/hangfire`. Verify `ProjectEnvironmentHealthCheckJob` is scheduled and running. Check backend logs for "Could not resolve hosting API" warnings — would indicate the env's project has no ServerId set.
