# Primary HTTP Service Declaration + Health-Based Project State

**Date:** 2026-04-24
**Status:** Approved for implementation
**Repos touched:** `web`, `hosting`

## Problem

The hosting manager's blue-green deploy logic picks the first docker-compose service that publishes any port (`deployment-queue-service.ts:487-489`) and starts a temp container with a bare `docker run <image>` — no command, no volumes, no env — then health-checks `GET /` on it.

For kurvo-backend this picked the `curves-engine` service (`osrm/osrm-backend` image) on port 3457. `osrm/osrm-backend` needs a mounted map file and an `osrm-routed /data/map.osrm` command to start, and responds to `/` with a 400 even when running. Every deploy now logs `Temp container failed health check` and falls back to force-recreate after burning ~30s on doomed health probes.

Separately, the hosting manager has an on-demand `/health` endpoint but no periodic health loop that reports back to the web backend, and nothing in the UI tells the user "your deploy succeeded but your app is 502-ing."

## Goals

1. Let a project declare which docker-compose service is its primary HTTP entrypoint in `compozerr.json`.
2. Use that declaration to target the right container during blue-green deploys (and, with the configured health path, check it correctly).
3. Use the same declaration to run a periodic HTTP health check against deployed environments.
4. Surface "container up, app not responding" as a distinct `Unhealthy` state in the Compozerr UI.
5. Audit `compozerr.schema.json` to declare the fields the hosting side already parses but the schema currently forbids.

## Non-goals

- Not changing Traefik routing based on health. Unhealthy envs still serve traffic; this is a display + deploy-gate signal.
- No alerts/notifications on unhealthy state (follow-up).
- Not backfilling historical health data.
- Not removing the existing on-demand `/health` route; it coexists.

## Config shape (compozerr.json)

Extend the existing (permissive, undocumented) `healthcheck` block with a `service` field:

```json
{
  "type": "project",
  "name": "kurvo-backend",
  "healthcheck": {
    "service": "web",        // NEW — docker-compose service name, the primary HTTP entrypoint
    "path": "/health",       // default "/"
    "interval": "30s",       // default "30s"
    "timeout": "5s"          // default "5s"
  }
}
```

**Semantics:**

- **`service` unset** → the project opts out of both zero-downtime deploys and periodic HTTP health checking. Existing behavior is preserved where safe; see "Behavior changes" below.
- **`service` set** → blue-green targets that service by docker-compose service name, periodic job HTTP-GETs `http://<vm-internal>:<published-port><path>` with `timeout`, and non-2xx/3xx marks the environment as `Unhealthy`.
- Defaults: `path = "/"`, `interval = "30s"`, `timeout = "5s"`.

### Schema audit (`compozerr.schema.json`)

The schema is `additionalProperties: false` but hosting parses several undocumented fields. Add these:

| Field | Type | Parsed by |
|-------|------|-----------|
| `healthcheck.service` | string | hosting (new) |
| `healthcheck.path` | string | hosting |
| `healthcheck.interval` | string (duration e.g. "30s") | hosting |
| `healthcheck.timeout` | string (duration e.g. "5s") | hosting |
| `cwd` | string | hosting |
| `build` | boolean | hosting |
| `modules` | object (string → string) | hosting (alias of schema's `dependencies`) |
| `zeroDowntime` | boolean | hosting |
| `docker.composeCommand` | string | hosting |

**`modules` vs `dependencies` reconciliation:** schema defines `dependencies`, hosting reads `modules`. Keep `dependencies` as the canonical name; add `modules` to schema as a temporary alias with a deprecation note, and update hosting to read `dependencies` first, falling back to `modules`. Does not change runtime behavior for existing configs.

## Data model (web backend)

Add to `backend/Api.Data/ProjectEnvironment.cs`:

```csharp
public EnvironmentHealthStatus HealthStatus { get; set; } = EnvironmentHealthStatus.Unknown;
public DateTimeOffset? LastHealthCheckAtUtc { get; set; }
public int ConsecutiveHealthFailures { get; set; }  // for debounce, server-side only
```

New enum:

```csharp
public enum EnvironmentHealthStatus { Unknown = 0, Healthy = 1, Unhealthy = 2 }
```

Add a new value to the project-level display enum `ProjectState`:

```csharp
public enum ProjectState { Unknown, Running, Starting, Stopped, Deleting, Unhealthy }
```

`Unhealthy` is a **derived display state**, not a stored value. Computed at read time:

> If `Project.State == Running` AND any non-preview `ProjectEnvironment` (Type != Preview) has `HealthStatus == Unhealthy`, the project's displayed state is `Unhealthy`. Otherwise use the stored `Project.State`.

Preview environments track their own `HealthStatus` (visible on the env detail page) but don't pull the project-level badge to `Unhealthy` — a broken short-lived preview shouldn't make a healthy prod project look red.

**Migration:** one EF migration adds the two columns + the debounce counter. Initial values: `HealthStatus = Unknown`, `LastHealthCheckAtUtc = null`, `ConsecutiveHealthFailures = 0`.

## Blue-green selector (hosting)

In `manager/services/deployment-queue-service.ts::tryBlueGreenDeployAsync` (currently starts at line 479):

1. Read `config.healthcheck.service` from the compose VM (already available via `CompozerrConfigService.getConfigFromVmAsync`).
2. **If unset** → log `"healthcheck.service not configured — zero-downtime deploy skipped. Set healthcheck.service in compozerr.json to enable."` and return `false` to fall through to `docker compose up -d --force-recreate`. **The current first-with-any-port heuristic is removed.**
3. **If set** → find the container in `psJsonOutput` where `Service === config.healthcheck.service`. If not found, log `"healthcheck.service '<name>' not found among running services — skipping blue-green"` and return `false`.
4. Use that container's first publisher (`PublishedPort`, `TargetPort`) instead of the first-with-port result.
5. When calling `waitForHealthAsync(vm, tempPort)` (line 578) and for the post-recreate check, pass the configured `path` and `timeout`. Update the signature:
   ```ts
   private async waitForHealthAsync(vm: VMWithIp, port: number, path: string, timeoutMs: number): Promise<boolean>
   ```
   The curl becomes `curl -sf -o /dev/null --connect-timeout 2 --max-time <timeoutMs/1000> http://localhost:<port><path>`.

### Behavior change for existing projects

Projects without `healthcheck.service` set lose zero-downtime deploys until they declare one. This is the deliberate trade: the current heuristic is the source of the original bug and fails silently. Force-recreate is a correct, if less smooth, fallback.

Main Compozerr app, blog, and any other repos that had coincidentally-working blue-green will be updated in a follow-up by adding `healthcheck.service` to their own compozerr.json files.

## Periodic health check (web backend, Hangfire)

One recurring Hangfire job: `ProjectEnvironmentHealthCheckJob`, scheduled every 30 seconds.

Each tick:

1. Query active `ProjectEnvironment` rows with a deployed VM (join through `Deployments` → `Servers`, or re-use whatever the existing domain routing path does).
2. For each env, call the hosting manager's new endpoint `GET /environments/:envId/health` (see below). The hosting side decides whether `healthcheck.service` is configured — unconfigured envs come back as `"unknown"` and are treated as no-ops.
3. Write the result back:
   - Response `"healthy"` → `HealthStatus = Healthy`, `ConsecutiveHealthFailures = 0`, `LastHealthCheckAtUtc = now`.
   - Response `"unhealthy"` → increment `ConsecutiveHealthFailures`, set `LastHealthCheckAtUtc = now`. Only flip `HealthStatus = Unhealthy` once the counter reaches **2** (debounce — prevents flapping during container restarts).
   - Response `"unknown"` (`healthcheck.service` not configured) → leave `HealthStatus` unchanged, do not bump the failure counter, update `LastHealthCheckAtUtc`.
   - VM unreachable / HTTP error to hosting → do nothing; next tick retries.

### Where does "does this project have healthcheck.service" live on the web side?

Two options; **picking (A)** for the first cut:

- **(A) Ask the hosting manager.** The health endpoint on hosting reads `compozerr.json` via `CompozerrConfigService`. If `healthcheck.service` is unset it returns `"unknown"` and the web job treats it as a no-op. No new DB field needed; the source of truth stays in the repo.
- (B) Mirror the config to the web DB on each deploy. Faster Hangfire ticks (avoids N hosting calls for envs that aren't configured) but adds a sync path. Can be added later if needed.

### Hosting-side endpoint

New route `GET /environments/:envId/health` in `manager/routes/projects.ts` (or a new `environments.ts` route file).

Implementation:
1. Look up the VM for the environment (existing pattern; same as `/projects/:projectId/health`).
2. Run `CompozerrConfigService.getConfigFromVmAsync(vm)`.
3. If `config.healthcheck?.service` unset → return `{ status: "unknown", reason: "not_configured" }`.
4. Run `docker compose ps --format json` on the VM, find the container where `Service === config.healthcheck.service`, extract its first `PublishedPort`.
5. From the VM, run `curl -sf -o /dev/null --connect-timeout 2 --max-time <timeout> http://localhost:<port><path>`. Exit code 0 → `{ status: "healthy" }`, else → `{ status: "unhealthy", httpCode: <code or null> }`.

## UI changes (web frontend)

`frontend/src/routes/_auth/_dashboard/projects/$projectId/index.tsx:51-75`:

- Status pill renders `Unhealthy` with a new visual style — red background, distinct icon from `Stopped` (e.g. `AlertTriangle` vs `CircleOff`). Tooltip: *"Containers are running, but the primary service is not responding."*
- Show `HealthStatus` + `LastHealthCheckAtUtc` ("last checked 12s ago") on each environment card / list row on the project detail page.

Projects list (`projects/index.tsx`):

- Status column displays `Unhealthy` using the same style as the detail page.
- No new filter added for now (can be follow-up if useful).

API surface:

- Extend whatever endpoint returns `Project` for the detail/list pages to include the derived display state and the environment-level `HealthStatus` + `LastHealthCheckAtUtc`. Regenerate the frontend OpenAPI client and use the generated types (no raw fetch / manual typing).

## Rollout / migration

1. **Schema-only** PR first (compozerr.schema.json + scripts/types/compozerr-file.ts) — unblocks declaring the new fields in downstream projects.
2. **Hosting-side** PR — new endpoint, updated blue-green selector, skip-when-unset behavior. Safe to ship before web-side consumption lands.
3. **Web-side** PR — EF migration, Hangfire job, API/UI changes.
4. **Config rollout** — add `healthcheck.service` to kurvo-backend (port 8080, `web` service) + Compozerr's own compozerr.json + blog. Verify blue-green works and periodic health shows Healthy.

## Open design decisions already settled

| Decision | Choice |
|----------|--------|
| Project-level state model | New `Unhealthy` display state, derived from env-level `HealthStatus` |
| Health state location | Per-`ProjectEnvironment` |
| Config shape | Extend existing `healthcheck` block with `service` |
| Periodic loop owner | Hangfire on web backend |
| Behavior when `healthcheck.service` unset | Skip blue-green entirely (safer than current heuristic); periodic check returns `unknown` |
| Project-level rollup from envs | Non-preview envs only (preview `Unhealthy` visible on env page but doesn't flip project badge) |
| Debounce | 2 consecutive failures before flipping to `Unhealthy`; immediate flip back to `Healthy` on first success |

## Files to touch

**Web repo (`/Users/milo/milodev/gits/web`)**
- `compozerr.schema.json` — add `healthcheck`, `cwd`, `build`, `modules`, `zeroDowntime`, `docker` blocks
- `scripts/types/compozerr-file.ts` — mirror schema additions in Zod
- `backend/Api.Data/ProjectEnvironment.cs` — new fields
- `backend/Api.Data/` new `EnvironmentHealthStatus.cs` + enum addition to `ProjectState.cs`
- `backend/Api.Data/Migrations/` new EF migration
- `backend/Api/` new Hangfire job `ProjectEnvironmentHealthCheckJob`
- `backend/Api/` projects endpoint response DTO — add health fields
- `frontend/src/generated/` — regenerate after backend changes
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/index.tsx` — status pill + env cards
- `frontend/src/routes/_auth/_dashboard/projects/index.tsx` — list status column

**Hosting repo (`/Users/milo/milodev/gits/hosting`)**
- `manager/services/compozerr-config-service.ts` — add `healthcheck.service` to `CompozerrConfig` interface
- `manager/services/deployment-queue-service.ts` — new blue-green selector, parameterized `waitForHealthAsync`
- `manager/routes/projects.ts` (or new `environments.ts`) — new `/environments/:envId/health` endpoint
- `manager/services/health-check-service.ts` — extract shared "HTTP probe from VM" helper used by both the new endpoint and any existing callers
- `manager/services/__tests__/blue-green-deploy.test.ts` — update cases to cover the new selector path, skip-when-unset, service-not-found fallback

## Risks

- **Existing projects lose blue-green.** Mitigation: (a) ship schema + hosting changes simultaneously so a follow-up CLI hint can nudge users to configure it; (b) short docs note in `docs/` describing the new field; (c) update first-party repos (Compozerr, blog, kurvo) as part of rollout.
- **Hangfire job load.** At ~30s interval across all envs, each tick makes one HTTP call per env to the hosting manager. Bound by number of active envs; small today. If it grows, batch per hosting manager (one `POST /environments/health-batch` call with envIds).
- **Flapping on cold start.** Debounce counter (2 failures ~= 60s) should cover container restarts. Monitor after rollout; if still noisy, bump to 3 or add a post-deploy grace period.
