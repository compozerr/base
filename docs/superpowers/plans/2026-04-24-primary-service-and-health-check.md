# Primary HTTP Service + Health State Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let projects declare a primary HTTP service in `compozerr.json`, use it to target the correct container during blue-green deploy, run a periodic HTTP health check, and expose an `Unhealthy` state in the Compozerr project UI.

**Architecture:** Two-repo feature. Config lives in `compozerr.json` on the user's project repo; hosting manager reads it via existing `CompozerrConfigService`; blue-green uses the declared service name to select the container; a new hosting endpoint probes the service over HTTP; a Hangfire job on the web backend calls the endpoint per environment and writes `HealthStatus` back to `ProjectEnvironment`. Project badge derives `Unhealthy` at read-time from env-level health.

**Tech Stack:** .NET 9 + EF Core + Hangfire + React/TypeScript (web) · Node + TypeScript + Vitest (hosting)

**Spec:** `docs/superpowers/specs/2026-04-24-primary-service-and-health-check-design.md`

**Worktrees:**
- Web: `/Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health` (branch `feature/primary-service-health`)
- Hosting: `/Users/milo/milodev/gits/hosting/.worktrees/feature-primary-service-health` (branch `feature/primary-service-health`)

Tasks are labeled `[web]` or `[hosting]` to show which worktree to work in. All absolute paths in code blocks refer to those worktrees.

---

## File structure

**Web worktree:**
- `compozerr.schema.json` — add `healthcheck`, `cwd`, `build`, `modules`, `zeroDowntime`, `docker`
- `scripts/types/compozerr-file.ts` — mirror schema in Zod
- `backend/Api.Data/EnvironmentHealthStatus.cs` — new enum (create)
- `backend/Api.Data/ProjectEnvironment.cs` — add `HealthStatus`, `LastHealthCheckAtUtc`, `ConsecutiveHealthFailures`
- `backend/Api.Data/ProjectState.cs` — add `Unhealthy = 5`
- `backend/Api.Data/Migrations/…_AddProjectEnvironmentHealth.cs` — EF migration (generated)
- `backend/Api.Hosting/Services/HostingApi.cs` — add `GetEnvironmentHealthAsync`
- `backend/Api.Hosting/Services/EnvironmentHealthResponse.cs` — response DTO (create)
- `backend/Api.Hosting/Jobs/ProjectEnvironmentHealthCheckJob.cs` — Hangfire job (create)
- `backend/Api.Hosting/ApiHostingFeature.cs` — register job
- `backend/Api.Hosting/Endpoints/Projects/…` — extend project/env response DTO with health fields
- `frontend/src/generated/*` — regenerated
- `frontend/src/routes/_auth/_dashboard/projects/$projectId/index.tsx` — status pill + env cards
- `frontend/src/routes/_auth/_dashboard/projects/index.tsx` — list status column

**Hosting worktree:**
- `manager/services/compozerr-config-service.ts` — add `healthcheck.service` field
- `manager/services/http-probe.ts` — new shared HTTP probe helper (create)
- `manager/services/deployment-queue-service.ts` — new blue-green selector, parameterized health check
- `manager/services/__tests__/blue-green-deploy.test.ts` — new cases
- `manager/routes/projects.ts` — register new environment health endpoint
- `manager/routes/environments.ts` — new route file for env-scoped endpoints (create)
- `manager/services/__tests__/compozerr-config-service.test.ts` — test for new field parsing

---

## Task 1: [web] Extend `compozerr.schema.json`

**Files:**
- Modify: `compozerr.schema.json`

- [ ] **Step 1: Add new top-level blocks to the schema**

Replace the `properties` block additions: after the existing `services` entry (ends at line 42), insert new fields, and update the `modules` vs `dependencies` reconciliation. Final schema snippet (replace the whole file content between the existing `"properties"` open brace and the `"required"` block):

```json
{
  "type": "object",
  "properties": {
    "id": { "type": "string" },
    "type": { "type": "string", "enum": ["project", "module"] },
    "name": { "type": "string" },
    "dependencies": {
      "type": "object",
      "additionalProperties": { "type": "string" }
    },
    "modules": {
      "type": "object",
      "description": "Alias of `dependencies`. Prefer `dependencies` in new configs.",
      "additionalProperties": { "type": "string" }
    },
    "services": {
      "type": "object",
      "properties": {
        "start": { "type": "string" },
        "stop": { "type": "string" },
        "readyCheck": { "type": "string" },
        "startupTimeoutMs": { "type": "number" },
        "port": { "type": "string" }
      },
      "additionalProperties": false
    },
    "start": { "type": "string" },
    "startupTimeoutMs": { "type": "number" },
    "readyMessage": { "type": "string" },
    "end": { "type": "string" },
    "port": { "type": "string" },
    "cwd": { "type": "string" },
    "build": { "type": "boolean" },
    "zeroDowntime": {
      "type": "boolean",
      "description": "Opt out of zero-downtime deploys. Defaults to true when `healthcheck.service` is set."
    },
    "healthcheck": {
      "type": "object",
      "properties": {
        "service": {
          "type": "string",
          "description": "docker-compose service name that is the primary HTTP entrypoint. Required to enable zero-downtime deploys and periodic HTTP health checks."
        },
        "path": {
          "type": "string",
          "description": "URL path hit by the health check. Default '/'."
        },
        "interval": {
          "type": "string",
          "description": "Polling interval as a duration string (e.g. '30s'). Default '30s'."
        },
        "timeout": {
          "type": "string",
          "description": "Per-request timeout as a duration string (e.g. '5s'). Default '5s'."
        }
      },
      "additionalProperties": false
    },
    "docker": {
      "type": "object",
      "properties": {
        "composeCommand": {
          "type": "string",
          "description": "Override the compose command binary (default 'docker compose')."
        }
      },
      "additionalProperties": false
    },
    "backend": {
      "type": "object",
      "properties": {
        "defaultEnvProperties": {
          "type": "object",
          "additionalProperties": { "type": "string" }
        }
      },
      "additionalProperties": false
    },
    "frontend": {
      "type": "object",
      "properties": {
        "srcDir": { "type": "string", "default": "frontend/src" },
        "routesDir": { "type": "string" },
        "routePrefix": { "type": "string" },
        "defaultEnvProperties": {
          "type": "object",
          "additionalProperties": { "type": "string" }
        }
      },
      "additionalProperties": false,
      "default": { "alias": "@/*", "srcDir": "frontend/src" }
    },
    "dockerComposeFile": { "type": "string" }
  },
  "required": ["type", "name"],
  "additionalProperties": false,
  "$schema": "http://json-schema.org/draft-07/schema#"
}
```

- [ ] **Step 2: Validate every existing compozerr.json still passes**

Run from the web worktree root:

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health
npx ajv validate -s compozerr.schema.json -d "compozerr.json" -d "modules/*/compozerr.json" --strict=false
```

Expected: each file prints `<path> valid`. If any fail, they're using undocumented fields — either add them to the schema or fix the config.

- [ ] **Step 3: Commit**

```bash
git add compozerr.schema.json
git commit -m "feat(schema): declare healthcheck block + missing hosting fields in compozerr.schema.json"
```

---

## Task 2: [web] Mirror schema additions in Zod type

**Files:**
- Modify: `scripts/types/compozerr-file.ts`

- [ ] **Step 1: Extend the Zod schema**

Replace the whole content of `scripts/types/compozerr-file.ts`:

```ts
import { z } from "npm:zod";
import { Config } from "../config.ts";

export const CompozerrFile = z.object({
    id: z.string().optional(),
    type: z.enum(["project", "module"]),
    name: z.string(),
    dependencies: z.record(z.string()).optional(),
    /** Alias of `dependencies`. Prefer `dependencies` in new configs. */
    modules: z.record(z.string()).optional(),
    services: z.object({
        start: z.string().optional(),
        stop: z.string().optional(),
        readyCheck: z.string().optional(),
        startupTimeoutMs: z.number().optional(),
        port: z.string().optional(),
    }).optional(),
    /** @deprecated Use `services.start` instead */
    start: z.string().optional(),
    startupTimeoutMs: z.number().optional(),
    /** @deprecated Use `services.readyCheck` instead */
    readyMessage: z.string().optional(),
    /** @deprecated Use `services.stop` instead */
    end: z.string().optional(),
    port: z.string().optional(),
    cwd: z.string().optional(),
    build: z.boolean().optional(),
    zeroDowntime: z.boolean().optional(),
    healthcheck: z.object({
        service: z.string().optional(),
        path: z.string().optional(),
        interval: z.string().optional(),
        timeout: z.string().optional(),
    }).optional(),
    docker: z.object({
        composeCommand: z.string().optional(),
    }).optional(),
    backend: z.object({
        defaultEnvProperties: z.record(z.string()).optional(),
    }).optional(),
    frontend: z.object({
        srcDir: z.string().optional().default(Config.defaults.frontend.srcDir),
        routesDir: z.string().optional(),
        routePrefix: z.string().optional(),
        defaultEnvProperties: z.record(z.string()).optional(),
    }).optional().default(Config.defaults.frontend),
    dockerComposeFile: z.string().optional(),
}).superRefine((data, ctx) => {
    if (data.type === "project" && !data.id) {
        ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: "Project must have an id",
            path: ["id"],
        });
    }
});

export type CompozerrFile = z.infer<typeof CompozerrFile>;
```

- [ ] **Step 2: Run type check on dev scripts**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health
deno check scripts/types/compozerr-file.ts
```

Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add scripts/types/compozerr-file.ts
git commit -m "feat(types): mirror compozerr.schema.json additions in Zod schema"
```

---

## Task 3: [hosting] Add `healthcheck.service` to CompozerrConfig interface

**Files:**
- Modify: `manager/services/compozerr-config-service.ts`
- Test: `manager/services/__tests__/compozerr-config-service.test.ts`

- [ ] **Step 1: Write the failing test**

Append to `manager/services/__tests__/compozerr-config-service.test.ts`:

```ts
describe("healthcheck.service", () => {
    it("parses healthcheck.service when present", async () => {
        const vm = {
            executeCommandAsync: vi.fn().mockResolvedValue({ exitcode: 0, "out-data": "" }),
        } as any;

        const service = new CompozerrConfigService();
        const config = await service.getConfigFromVmAsync(vm, "main", async (cmd) => {
            if (cmd.includes("find")) return { exitcode: 0, "out-data": "main/compozerr.json\n" };
            if (cmd.startsWith("cat ")) return {
                exitcode: 0,
                "out-data": JSON.stringify({
                    type: "project",
                    name: "demo",
                    healthcheck: { service: "web", path: "/health", interval: "30s", timeout: "5s" },
                }),
            };
            return { exitcode: 1, "out-data": "" };
        });

        expect(config.healthcheck).toEqual({ service: "web", path: "/health", interval: "30s", timeout: "5s" });
    });

    it("leaves healthcheck undefined when not set", async () => {
        const vm = {} as any;
        const service = new CompozerrConfigService();
        const config = await service.getConfigFromVmAsync(vm, "main", async (cmd) => {
            if (cmd.includes("find")) return { exitcode: 0, "out-data": "main/compozerr.json\n" };
            return { exitcode: 0, "out-data": JSON.stringify({ type: "project", name: "demo" }) };
        });

        expect(config.healthcheck).toBeUndefined();
    });
});
```

- [ ] **Step 2: Run test to verify first case fails (unknown property)**

```bash
cd /Users/milo/milodev/gits/hosting/.worktrees/feature-primary-service-health
npx vitest run manager/services/__tests__/compozerr-config-service.test.ts -t "healthcheck.service"
```

Expected: first test FAILS (TypeScript complains `service` is not in `healthcheck` type, or runtime returns undefined because interface doesn't declare it).

If Node/Vitest silently passes (since interface is structural and JSON spreads any extra key through `{...config}`), still proceed to Step 3 to make the type declaration honest.

- [ ] **Step 3: Add `service` to the CompozerrConfig interface**

In `manager/services/compozerr-config-service.ts`, replace the `healthcheck` field inside the `CompozerrConfig` interface (currently lines 21-25):

```ts
    healthcheck?: {
        service?: string;    // docker-compose service name; the primary HTTP entrypoint
        path?: string;       // default: "/"
        interval?: string;   // default: "30s"
        timeout?: string;    // default: "5s"
    };
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
npx vitest run manager/services/__tests__/compozerr-config-service.test.ts
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add manager/services/compozerr-config-service.ts manager/services/__tests__/compozerr-config-service.test.ts
git commit -m "feat(config): add healthcheck.service to CompozerrConfig interface"
```

---

## Task 4: [hosting] Extract shared HTTP probe helper

The current `waitForHealthAsync` in `deployment-queue-service.ts:578` curls `/` with a hardcoded timeout. Extract a reusable helper that accepts path + timeout so both blue-green and the new env-health endpoint can use it.

**Files:**
- Create: `manager/services/http-probe.ts`
- Create: `manager/services/__tests__/http-probe.test.ts`

- [ ] **Step 1: Write the failing test**

Create `manager/services/__tests__/http-probe.test.ts`:

```ts
import { describe, it, expect, vi } from "vitest";
import { probeHttpOnVmAsync, waitForHttpHealthyAsync } from "../http-probe";

describe("probeHttpOnVmAsync", () => {
    it("issues a single curl with the given path and timeout", async () => {
        const exec = vi.fn().mockResolvedValue({ exitcode: 0, "out-data": "", "err-data": "" });
        const vm = { executeCommandAsync: exec } as any;

        const healthy = await probeHttpOnVmAsync(vm, 8080, "/health", 5000);

        expect(healthy).toBe(true);
        expect(exec).toHaveBeenCalledTimes(1);
        const cmd = exec.mock.calls[0][0] as string;
        expect(cmd).toContain("http://localhost:8080/health");
        expect(cmd).toContain("--max-time 5");
    });

    it("returns false when curl exits non-zero", async () => {
        const exec = vi.fn().mockResolvedValue({ exitcode: 22, "out-data": "", "err-data": "" });
        const vm = { executeCommandAsync: exec } as any;
        expect(await probeHttpOnVmAsync(vm, 80, "/", 5000)).toBe(false);
    });
});

describe("waitForHttpHealthyAsync", () => {
    it("returns true on first success", async () => {
        const exec = vi.fn().mockResolvedValue({ exitcode: 0 });
        const vm = { executeCommandAsync: exec } as any;
        expect(await waitForHttpHealthyAsync(vm, 8080, "/", 5000, 1)).toBe(true);
        expect(exec).toHaveBeenCalledTimes(1);
    });

    it("retries up to maxAttempts then returns false", async () => {
        const exec = vi.fn().mockResolvedValue({ exitcode: 7 });
        const vm = { executeCommandAsync: exec } as any;
        const result = await waitForHttpHealthyAsync(vm, 8080, "/", 5000, 3, 0);
        expect(result).toBe(false);
        expect(exec).toHaveBeenCalledTimes(3);
    });
});
```

- [ ] **Step 2: Run test to verify it fails**

```bash
cd /Users/milo/milodev/gits/hosting/.worktrees/feature-primary-service-health
npx vitest run manager/services/__tests__/http-probe.test.ts
```

Expected: FAIL — module not found.

- [ ] **Step 3: Implement `manager/services/http-probe.ts`**

```ts
import type { VMWithIp } from "./vms-service";

/**
 * Issues a single curl from the VM to localhost:<port><path>.
 * Returns true if HTTP status is 2xx or 3xx (curl -f exit 0), false otherwise.
 */
export async function probeHttpOnVmAsync(
    vm: VMWithIp,
    port: number,
    path: string,
    timeoutMs: number,
): Promise<boolean> {
    const timeoutSec = Math.max(1, Math.ceil(timeoutMs / 1000));
    const normalizedPath = path.startsWith("/") ? path : `/${path}`;
    const cmd = `curl -sf -o /dev/null --connect-timeout 2 --max-time ${timeoutSec} http://localhost:${port}${normalizedPath}`;
    const result = await vm.executeCommandAsync(cmd, true);
    return result.exitcode === 0;
}

/**
 * Repeatedly probes the same HTTP endpoint until it returns 2xx/3xx or attempts are exhausted.
 */
export async function waitForHttpHealthyAsync(
    vm: VMWithIp,
    port: number,
    path: string,
    timeoutMs: number,
    maxAttempts: number,
    delayBetweenAttemptsMs: number = 1000,
): Promise<boolean> {
    for (let i = 0; i < maxAttempts; i++) {
        const healthy = await probeHttpOnVmAsync(vm, port, path, timeoutMs);
        if (healthy) return true;
        if (i < maxAttempts - 1) {
            await new Promise(resolve => setTimeout(resolve, delayBetweenAttemptsMs));
        }
    }
    return false;
}
```

- [ ] **Step 4: Run test to verify it passes**

```bash
npx vitest run manager/services/__tests__/http-probe.test.ts
```

Expected: all 4 cases PASS.

- [ ] **Step 5: Commit**

```bash
git add manager/services/http-probe.ts manager/services/__tests__/http-probe.test.ts
git commit -m "feat(hosting): add shared HTTP probe helper for VM health checks"
```

---

## Task 5: [hosting] Update blue-green selector + use configured path/timeout

**Files:**
- Modify: `manager/services/deployment-queue-service.ts`

- [ ] **Step 1: Replace `tryBlueGreenDeployAsync` and `waitForHealthAsync`**

In `manager/services/deployment-queue-service.ts`:

1. Add an import near the top (beside other service imports):

```ts
import { probeHttpOnVmAsync, waitForHttpHealthyAsync } from "./http-probe";
import { CompozerrConfigService } from "./compozerr-config-service";
```

2. Replace the body of `tryBlueGreenDeployAsync` starting at the current line 479 through the end of the method (up to the matching closing brace before `waitForHealthAsync`) with:

```ts
    private async tryBlueGreenDeployAsync(vm: VMWithIp, buildLogsService: BuildLogsService, psJsonOutput: string): Promise<boolean> {
        try {
            await buildLogsService.appendLogAsync("Attempting zero-downtime blue-green deploy", "info");

            const configService = new CompozerrConfigService();
            const config = await configService.getConfigFromVmAsync(vm);
            const primaryService = config.healthcheck?.service;
            const healthPath = config.healthcheck?.path ?? "/";
            const healthTimeoutMs = this.parseDurationMs(config.healthcheck?.timeout, 5000);

            if (!primaryService) {
                await buildLogsService.appendLogAsync(
                    "healthcheck.service not configured — zero-downtime deploy skipped. Set healthcheck.service in compozerr.json to enable.",
                    "info",
                );
                return false;
            }

            const containers: ComposeContainer[] = psJsonOutput.trim().split("\n")
                .map(line => { try { return JSON.parse(line); } catch { return null; } })
                .filter(Boolean);

            const primaryContainer = containers.find(c => c.Service === primaryService);
            if (!primaryContainer) {
                await buildLogsService.appendLogAsync(
                    `healthcheck.service '${primaryService}' not found among running services — skipping blue-green`,
                    "warning",
                );
                return false;
            }

            const publisher = (primaryContainer.Publishers ?? []).find(p => p.PublishedPort > 0);
            if (!publisher) {
                await buildLogsService.appendLogAsync(
                    `healthcheck.service '${primaryService}' has no published port — skipping blue-green`,
                    "warning",
                );
                return false;
            }

            const currentPort = publisher.PublishedPort;
            const containerPort = publisher.TargetPort;
            const tempPort = currentPort + BLUE_GREEN_PORT_OFFSET;

            if (tempPort > 65535) {
                await buildLogsService.appendLogAsync(
                    `Temp port ${tempPort} exceeds max (65535), skipping blue-green`, "warning",
                );
                return false;
            }

            const configResult = await vm.executeCommandInWorkingDirectoryAsync(
                `sudo docker compose config --images ${primaryService} 2>/dev/null | head -1`, true,
            );
            const imageName = configResult["out-data"]?.trim().split("\n")[0];
            if (!imageName) {
                await buildLogsService.appendLogAsync(
                    `Could not resolve image for service '${primaryService}' — skipping blue-green`, "warning",
                );
                return false;
            }

            await buildLogsService.appendLogAsync(
                `Blue-green: service '${primaryService}' port ${currentPort} → temp ${tempPort}, image: ${imageName}`, "info",
            );

            const runResult = await vm.executeCommandAsync(
                `sudo docker run -d --name rollout-temp -p ${tempPort}:${containerPort} ${imageName}`, true,
            );
            if (runResult.exitcode !== 0) {
                await buildLogsService.appendLogAsync("Failed to start temp container", "warning");
                return false;
            }

            const tempHealthy = await waitForHttpHealthyAsync(vm, tempPort, healthPath, healthTimeoutMs, 30);
            if (!tempHealthy) {
                await vm.executeCommandAsync("sudo docker rm -f rollout-temp", true);
                await buildLogsService.appendLogAsync("Temp container failed health check", "warning");
                return false;
            }
            await buildLogsService.appendLogAsync("Temp container healthy", "info");

            const domainService = ServiceProvider.get("domainService") as DomainService;
            domainService.switchPortForVmIp(vm.ip, currentPort, tempPort);
            await buildLogsService.appendLogAsync("Traefik switched to temp port", "info");

            await new Promise(resolve => setTimeout(resolve, TRAEFIK_CONFIG_RELOAD_WAIT_MS));

            const recreateResult = await vm.executeCommandInWorkingDirectoryAsync(
                "sudo docker compose up -d --force-recreate --remove-orphans", true,
            );
            if (recreateResult.exitcode !== 0) {
                await buildLogsService.appendLogAsync(
                    "Main recreate failed — temp container still serving traffic", "error",
                );
                return true;
            }

            const mainHealthy = await waitForHttpHealthyAsync(vm, currentPort, healthPath, healthTimeoutMs, 30);
            if (!mainHealthy) {
                await buildLogsService.appendLogAsync(
                    "Main service failed health check — keeping traffic on temp port", "warning",
                );
                return true;
            }
            await buildLogsService.appendLogAsync("Main service healthy", "info");

            domainService.switchPortForVmIp(vm.ip, tempPort, currentPort);
            await buildLogsService.appendLogAsync("Traefik switched back to original port", "info");

            await vm.executeCommandAsync("sudo docker rm -f rollout-temp", true);

            await buildLogsService.appendLogAsync("Zero-downtime blue-green deploy completed", "success");
            return true;
        } catch (error) {
            await vm.executeCommandAsync("sudo docker rm -f rollout-temp 2>/dev/null", true);
            return false;
        }
    }

    private parseDurationMs(input: string | undefined, defaultMs: number): number {
        if (!input) return defaultMs;
        const match = input.trim().match(/^(\d+)(ms|s|m)?$/);
        if (!match) return defaultMs;
        const n = parseInt(match[1], 10);
        switch (match[2]) {
            case "ms": return n;
            case "m": return n * 60_000;
            case "s":
            default: return n * 1000;
        }
    }
```

3. Delete the old `waitForHealthAsync` method (lines 578-587 in the original file) — it is replaced by `waitForHttpHealthyAsync` from `http-probe.ts`.

- [ ] **Step 2: Type-check the hosting package**

```bash
cd /Users/milo/milodev/gits/hosting/.worktrees/feature-primary-service-health
npx tsc --noEmit
```

Expected: no errors. If an error says `waitForHealthAsync` is referenced elsewhere, grep for remaining callers and convert them to `waitForHttpHealthyAsync`.

- [ ] **Step 3: Commit**

```bash
git add manager/services/deployment-queue-service.ts
git commit -m "feat(deploy): select blue-green target by healthcheck.service; skip when unset"
```

---

## Task 6: [hosting] Update blue-green tests

**Files:**
- Modify: `manager/services/__tests__/blue-green-deploy.test.ts`

- [ ] **Step 1: Add test cases for the new selector behavior**

Read the current test file first to understand how it constructs fixtures, then append these cases inside the existing `describe` block (or create a new `describe("primary-service selector")` block at the end):

```ts
describe("primary-service selector", () => {
    // Re-use whatever mock scaffolding the existing tests use for tryBlueGreenDeployAsync.
    // If the existing tests mock CompozerrConfigService at module scope, extend that mock.

    it("skips blue-green when healthcheck.service is not configured", async () => {
        // Arrange: mock CompozerrConfigService.getConfigFromVmAsync to return {} (no healthcheck)
        // Act: invoke tryBlueGreenDeployAsync with a psJsonOutput containing one container
        // Assert: returns false, no `docker run rollout-temp` command executed, log contains
        //   "healthcheck.service not configured"
    });

    it("skips blue-green when configured service is not among running containers", async () => {
        // Arrange: config returns { healthcheck: { service: "missing" } }, psJsonOutput
        //   contains services { "web", "db" }.
        // Assert: returns false, log contains "'missing' not found among running services"
    });

    it("selects container matching healthcheck.service (not first-with-port)", async () => {
        // Arrange: config.healthcheck.service = "web", psJsonOutput has two services:
        //   "db" (first, port 5432) and "web" (second, port 8080).
        // Assert: the `docker run rollout-temp` command published port 8080 + 10000 (temp offset),
        //   NOT 5432 + 10000.
    });

    it("uses configured healthcheck.path for temp container health probe", async () => {
        // Arrange: config.healthcheck = { service: "web", path: "/ready", timeout: "3s" }
        // Assert: the curl command executed by waitForHttpHealthyAsync contains
        //   "http://localhost:<tempPort>/ready" and "--max-time 3".
    });
});
```

Fill in the arrangement/mocking details to match the existing test file's style. If the existing file doesn't mock `CompozerrConfigService`, mock it at the top of the file with `vi.mock("../compozerr-config-service", …)`.

- [ ] **Step 2: Run the updated tests**

```bash
cd /Users/milo/milodev/gits/hosting/.worktrees/feature-primary-service-health
npx vitest run manager/services/__tests__/blue-green-deploy.test.ts
```

Expected: all new cases PASS.

- [ ] **Step 3: Commit**

```bash
git add manager/services/__tests__/blue-green-deploy.test.ts
git commit -m "test(deploy): cover blue-green primary-service selector paths"
```

---

## Task 7: [hosting] Add `GET /environments/:envId/health` endpoint

**Files:**
- Create: `manager/routes/environments.ts`
- Modify: `manager/index.ts` (or wherever routes are mounted — find via grep)
- Create: `manager/services/environment-health-service.ts`

- [ ] **Step 1: Find where routes are mounted**

```bash
cd /Users/milo/milodev/gits/hosting/.worktrees/feature-primary-service-health
grep -rn "routes/projects\|registerProjectRoutes\|app.use.*projects" manager --include="*.ts" | head -5
```

Note the file + line (likely `manager/index.ts`) where `projects` routes are registered. Use the same pattern for the new `environments` routes.

- [ ] **Step 2: Implement the service**

Create `manager/services/environment-health-service.ts`:

```ts
import { logger } from "./logger";
import { CompozerrConfigService } from "./compozerr-config-service";
import { probeHttpOnVmAsync } from "./http-probe";
import type { VMWithIp } from "./vms-service";

export type EnvHealthStatus = "healthy" | "unhealthy" | "unknown";

export interface EnvHealthResult {
    status: EnvHealthStatus;
    reason?: string;
    checkedPort?: number;
    checkedPath?: string;
}

interface ComposePsEntry {
    Service: string;
    Publishers: { PublishedPort: number; TargetPort: number }[] | null;
}

function parseDurationMs(input: string | undefined, defaultMs: number): number {
    if (!input) return defaultMs;
    const match = input.trim().match(/^(\d+)(ms|s|m)?$/);
    if (!match) return defaultMs;
    const n = parseInt(match[1], 10);
    switch (match[2]) {
        case "ms": return n;
        case "m": return n * 60_000;
        case "s":
        default: return n * 1000;
    }
}

export class EnvironmentHealthService {
    async checkAsync(vm: VMWithIp): Promise<EnvHealthResult> {
        const configService = new CompozerrConfigService();
        const config = await configService.getConfigFromVmAsync(vm);

        const primaryService = config.healthcheck?.service;
        if (!primaryService) {
            return { status: "unknown", reason: "not_configured" };
        }

        const healthPath = config.healthcheck?.path ?? "/";
        const timeoutMs = parseDurationMs(config.healthcheck?.timeout, 5000);

        const psResult = await vm.executeCommandAsync(
            "sudo docker compose ps --format json 2>/dev/null", true,
        );
        if (psResult.exitcode !== 0 || !psResult["out-data"]?.trim()) {
            return { status: "unhealthy", reason: "compose_ps_failed" };
        }

        const containers: ComposePsEntry[] = psResult["out-data"].trim().split("\n")
            .map(line => { try { return JSON.parse(line); } catch { return null; } })
            .filter(Boolean);

        const primary = containers.find(c => c.Service === primaryService);
        if (!primary) {
            return { status: "unhealthy", reason: "service_not_running" };
        }

        const publisher = (primary.Publishers ?? []).find(p => p.PublishedPort > 0);
        if (!publisher) {
            return { status: "unhealthy", reason: "no_published_port" };
        }

        const healthy = await probeHttpOnVmAsync(vm, publisher.PublishedPort, healthPath, timeoutMs);
        logger
            .forContext("service", primaryService)
            .forContext("port", publisher.PublishedPort)
            .forContext("path", healthPath)
            .forContext("healthy", healthy)
            .information("Env health probe complete");

        return {
            status: healthy ? "healthy" : "unhealthy",
            reason: healthy ? undefined : "probe_failed",
            checkedPort: publisher.PublishedPort,
            checkedPath: healthPath,
        };
    }
}
```

- [ ] **Step 3: Implement the route file**

Create `manager/routes/environments.ts`. Match the patterns used by `manager/routes/projects.ts` (same Express or hono framework, same VM lookup, same auth). Template:

```ts
import { Router } from "express"; // or whatever framework projects.ts uses
import { logger } from "../services/logger";
import { VmsService } from "../services/vms-service";
import { EnvironmentHealthService } from "../services/environment-health-service";

export function registerEnvironmentRoutes(router: Router) {
    router.get("/environments/:envId/health", async (req, res) => {
        const envId = req.params.envId;
        try {
            // Look up the VM for this environment. If projects.ts looks up by projectId,
            // we likely need an env→vm mapping. If VMs are keyed by envId already, use it directly.
            // NOTE: Check how deployment-queue-service resolves the VM for a given deployment/env
            // and mirror that here.
            const vmsService = new VmsService(undefined as any); // no buildLogs needed
            const vm = await vmsService.findVmByEnvironmentIdAsync(envId);
            if (!vm) {
                return res.json({ status: "unknown", reason: "vm_not_found" });
            }

            const service = new EnvironmentHealthService();
            const result = await service.checkAsync(vm);
            return res.json(result);
        } catch (err) {
            logger.forContext("error", err).warning("Env health check failed");
            return res.json({ status: "unknown", reason: "check_error" });
        }
    });
}
```

**Note for implementer:** `VmsService.findVmByEnvironmentIdAsync` probably doesn't exist today — search for existing env/vm lookups:

```bash
grep -rn "environmentId\|envId" manager/services/vms-service.ts manager/routes/projects.ts
```

Use the same query path as the existing `/projects/:projectId/health` endpoint (which looks up by projectId). If env-scoped lookup doesn't exist, change the route signature to `GET /projects/:projectId/environments/:envId/health` and resolve via the existing path, or accept projectId as a query param and pair it with envId internally. Pick whatever's least invasive for this diff.

- [ ] **Step 4: Mount the new route**

In wherever routes are registered (from Step 1), add:

```ts
import { registerEnvironmentRoutes } from "./routes/environments";
// ...
registerEnvironmentRoutes(router);
```

- [ ] **Step 5: Write a smoke test**

Create `manager/services/__tests__/environment-health-service.test.ts`:

```ts
import { describe, it, expect, vi } from "vitest";
import { EnvironmentHealthService } from "../environment-health-service";

vi.mock("../compozerr-config-service", () => ({
    CompozerrConfigService: class {
        async getConfigFromVmAsync() {
            return { healthcheck: { service: "web", path: "/ok", timeout: "2s" } };
        }
    },
}));

vi.mock("../logger", () => {
    const createChainable = (): any => ({
        forContext: () => createChainable(),
        information: vi.fn(),
        warning: vi.fn(),
    });
    return { logger: createChainable() };
});

describe("EnvironmentHealthService.checkAsync", () => {
    it("returns healthy when probe succeeds against the configured service port", async () => {
        const vm = {
            executeCommandAsync: vi.fn().mockImplementation(async (cmd: string) => {
                if (cmd.includes("docker compose ps")) {
                    return {
                        exitcode: 0,
                        "out-data": JSON.stringify({
                            Service: "web",
                            Publishers: [{ PublishedPort: 8080, TargetPort: 8080 }],
                        }),
                    };
                }
                if (cmd.includes("curl")) return { exitcode: 0 };
                return { exitcode: 1, "out-data": "" };
            }),
        } as any;

        const result = await new EnvironmentHealthService().checkAsync(vm);
        expect(result.status).toBe("healthy");
        expect(result.checkedPort).toBe(8080);
        expect(result.checkedPath).toBe("/ok");
    });

    it("returns unhealthy with reason service_not_running when service missing from ps", async () => {
        const vm = {
            executeCommandAsync: vi.fn().mockImplementation(async (cmd: string) => {
                if (cmd.includes("docker compose ps")) return { exitcode: 0, "out-data": JSON.stringify({ Service: "db", Publishers: [] }) };
                return { exitcode: 1 };
            }),
        } as any;

        const result = await new EnvironmentHealthService().checkAsync(vm);
        expect(result).toMatchObject({ status: "unhealthy", reason: "service_not_running" });
    });
});
```

- [ ] **Step 6: Run tests and type check**

```bash
npx vitest run manager/services/__tests__/environment-health-service.test.ts
npx tsc --noEmit
```

Expected: tests PASS, tsc clean.

- [ ] **Step 7: Commit**

```bash
git add manager/routes/environments.ts manager/services/environment-health-service.ts manager/services/__tests__/environment-health-service.test.ts manager/index.ts
git commit -m "feat(hosting): add GET /environments/:envId/health endpoint"
```

---

## Task 8: [web] Add `EnvironmentHealthStatus` enum + fields to ProjectEnvironment

**Files:**
- Create: `backend/Api.Data/EnvironmentHealthStatus.cs`
- Modify: `backend/Api.Data/ProjectEnvironment.cs`

- [ ] **Step 1: Create the enum**

`backend/Api.Data/EnvironmentHealthStatus.cs`:

```csharp
using System.Text.Json.Serialization;

namespace Api.Data;

[JsonConverter(typeof(JsonStringEnumConverter<EnvironmentHealthStatus>))]
public enum EnvironmentHealthStatus
{
    Unknown = 0,
    Healthy = 1,
    Unhealthy = 2,
}
```

- [ ] **Step 2: Add fields to `ProjectEnvironment`**

In `backend/Api.Data/ProjectEnvironment.cs`, insert these three properties just below `BillingStatus` (around line 36):

```csharp
    public EnvironmentHealthStatus HealthStatus { get; set; } = EnvironmentHealthStatus.Unknown;
    public DateTimeOffset? LastHealthCheckAtUtc { get; set; }
    public int ConsecutiveHealthFailures { get; set; }
```

- [ ] **Step 3: Build**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health
dotnet build backend/Api.Data/Api.Data.csproj
```

Expected: success.

- [ ] **Step 4: Commit**

```bash
git add backend/Api.Data/EnvironmentHealthStatus.cs backend/Api.Data/ProjectEnvironment.cs
git commit -m "feat(data): add EnvironmentHealthStatus enum + health fields to ProjectEnvironment"
```

---

## Task 9: [web] Add `Unhealthy` display state to `ProjectState`

**Files:**
- Modify: `backend/Api.Data/ProjectState.cs`

- [ ] **Step 1: Add enum value**

Replace `backend/Api.Data/ProjectState.cs` with:

```csharp
using System.Text.Json.Serialization;

namespace Api.Data;

[JsonConverter(typeof(JsonStringEnumConverter<ProjectState>))]
public enum ProjectState
{
    Unknown = 0,
    Running = 1,
    Starting = 2,
    Stopped = 3,
    Deleting = 4,
    /// <summary>
    /// Derived display state: containers are up (Running) but the primary HTTP service
    /// is not responding to health checks. Never written to the DB directly — computed
    /// at read time from Project.State + any non-preview ProjectEnvironment.HealthStatus.
    /// </summary>
    Unhealthy = 5,
}
```

- [ ] **Step 2: Build**

```bash
dotnet build backend/Api.Data/Api.Data.csproj
```

Expected: success.

- [ ] **Step 3: Commit**

```bash
git add backend/Api.Data/ProjectState.cs
git commit -m "feat(data): add ProjectState.Unhealthy display value"
```

---

## Task 10: [web] EF migration for ProjectEnvironment health columns

**Files:**
- Create: `backend/Api.Data/Migrations/<timestamp>_AddProjectEnvironmentHealth.cs` (auto-generated)
- Modify: `backend/Api.Data/Migrations/ApiDbContextModelSnapshot.cs` (auto-updated)

- [ ] **Step 1: Find how migrations are typically generated here**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health
ls backend/Api.Data/Migrations/ | tail -5
grep -rn "dotnet ef" backend/Api.Data/*.csproj docs README.md 2>/dev/null | head -5
```

Note the EF CLI pattern used by other migrations (startup project, DbContext name).

- [ ] **Step 2: Generate the migration**

Assuming the typical pattern (adjust startup/project/context names if the repo uses different ones):

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health
dotnet ef migrations add AddProjectEnvironmentHealth \
    --project backend/Api.Data/Api.Data.csproj \
    --startup-project backend/Api/Api.csproj \
    --context ApiDbContext
```

Expected: new migration file in `backend/Api.Data/Migrations/`.

- [ ] **Step 3: Verify the migration only touches the three new columns**

Open the generated migration `Up` method. It must contain exactly:
- `AddColumn<int>("HealthStatus", schema: "api", table: "ProjectEnvironments", nullable: false, defaultValue: 0)`
- `AddColumn<DateTimeOffset>("LastHealthCheckAtUtc", schema: "api", table: "ProjectEnvironments", nullable: true)`
- `AddColumn<int>("ConsecutiveHealthFailures", schema: "api", table: "ProjectEnvironments", nullable: false, defaultValue: 0)`

and the matching `DropColumn` in `Down`. If anything else shows up (renamed fields, index changes), investigate before proceeding — the snapshot may be stale.

- [ ] **Step 4: Apply and verify locally**

```bash
# Apply via compozerr dev — the feature's own ConfigureApp migrates on boot
compozerr dev &
# Wait for backend to boot
until curl -s http://localhost:1235/swagger/v1/swagger.json > /dev/null 2>&1; do sleep 1; done

# Inspect the new columns
docker exec main-db-1 psql -U postgres2 -d myapp -c '\d api."ProjectEnvironments"' | grep -E "HealthStatus|LastHealthCheckAtUtc|ConsecutiveHealthFailures"
```

Expected output: all three columns listed with correct types.

- [ ] **Step 5: Commit**

```bash
git add backend/Api.Data/Migrations
git commit -m "feat(data): migration for ProjectEnvironment health columns"
```

---

## Task 11: [web] Add `GetEnvironmentHealthAsync` to HostingApi

**Files:**
- Create: `backend/Api.Hosting/Services/EnvironmentHealthResponse.cs`
- Modify: `backend/Api.Hosting/Services/HostingApi.cs`

- [ ] **Step 1: Create the response DTO**

`backend/Api.Hosting/Services/EnvironmentHealthResponse.cs`:

The hosting endpoint returns lowercase status strings (`"healthy"`, `"unhealthy"`, `"unknown"`) — we must deserialize case-insensitively. A custom `JsonStringEnumConverter` with `JsonNamingPolicy.CamelCase` handles both lower-case wire values and case-insensitive matching.

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Hosting.Services;

[JsonConverter(typeof(LowercaseEnvHealthStatusConverter))]
public enum EnvironmentHealthReportStatus
{
    Unknown,
    Healthy,
    Unhealthy,
}

internal sealed class LowercaseEnvHealthStatusConverter : JsonStringEnumConverter<EnvironmentHealthReportStatus>
{
    public LowercaseEnvHealthStatusConverter()
        : base(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    { }
}

public sealed class EnvironmentHealthResponse
{
    public EnvironmentHealthReportStatus Status { get; set; }
    public string? Reason { get; set; }
    public int? CheckedPort { get; set; }
    public string? CheckedPath { get; set; }
}
```

Sanity-check during Step 3: round-trip deserialize `{ "status": "healthy" }` in a scratch test or via `dotnet fsi` if available — should produce `EnvironmentHealthReportStatus.Healthy`.

- [ ] **Step 2: Add the method to HostingApi**

In `backend/Api.Hosting/Services/HostingApi.cs`, add this method near the other `HttpClient.GetAsync` methods (e.g. right after `GetServerUsageAsync`):

```csharp
    public async Task<EnvironmentHealthResponse?> GetEnvironmentHealthAsync(Guid environmentId)
    {
        try
        {
            var response = await HttpClient.GetAsync($"/environments/{environmentId}/health");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<EnvironmentHealthResponse>();
        }
        catch (Exception ex)
        {
            Serilog.Log.Logger
                .ForContext("environmentId", environmentId)
                .ForContext("error", ex)
                .Warning("Failed to fetch environment health from hosting");
            return null;
        }
    }
```

- [ ] **Step 3: Build**

```bash
dotnet build backend/Api.Hosting/Api.Hosting.csproj
```

Expected: success.

- [ ] **Step 4: Commit**

```bash
git add backend/Api.Hosting/Services/EnvironmentHealthResponse.cs backend/Api.Hosting/Services/HostingApi.cs
git commit -m "feat(hosting-client): add GetEnvironmentHealthAsync to HostingApi"
```

---

## Task 12: [web] Create `ProjectEnvironmentHealthCheckJob` + register it

**Files:**
- Create: `backend/Api.Hosting/Jobs/ProjectEnvironmentHealthCheckJob.cs`
- Modify: `backend/Api.Hosting/ApiHostingFeature.cs`
- Modify: `backend/Api.Data/Repositories/ProjectEnvironmentRepository.cs` — add a method to load active envs grouped by server

- [ ] **Step 1: Add repository method**

In `backend/Api.Data/Repositories/ProjectEnvironmentRepository.cs`, extend `IProjectEnvironmentRepository` with:

```csharp
    public Task<List<ProjectEnvironment>> GetAllActiveWithProjectAndServerAsync(CancellationToken cancellationToken = default);
```

Implementation (inside the sealed class):

```csharp
    public Task<List<ProjectEnvironment>> GetAllActiveWithProjectAndServerAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.ProjectEnvironments
            .Include(e => e.Project)
            .Where(e => e.Project != null && e.Project.DeletedAtUtc == null && e.Project.ServerId != null)
            .Where(e => e.BillingStatus == EnvironmentBillingStatus.Active)
            .ToListAsync(cancellationToken);
    }
```

(If `dbContext` is named differently in the class, use the actual field name. Open the file to check.)

- [ ] **Step 2: Create the job**

`backend/Api.Hosting/Jobs/ProjectEnvironmentHealthCheckJob.cs`:

```csharp
using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Jobs;
using Serilog;

namespace Api.Hosting.Jobs;

public class ProjectEnvironmentHealthCheckJob(
    IProjectEnvironmentRepository environmentRepository,
    IHostingApiFactory hostingApiFactory) : JobBase<ProjectEnvironmentHealthCheckJob>
{
    private const int UnhealthyFlipThreshold = 2;

    public override async Task ExecuteAsync()
    {
        var environments = await environmentRepository.GetAllActiveWithProjectAndServerAsync();

        foreach (var env in environments)
        {
            var serverId = env.Project?.ServerId;
            if (serverId is null) continue;

            HostingApi hostingApi;
            try
            {
                hostingApi = await hostingApiFactory.GetHostingApiAsync(serverId);
            }
            catch (Exception ex)
            {
                Log.Logger.ForContext("environmentId", env.Id).ForContext("error", ex)
                    .Warning("Could not resolve hosting API for env; skipping");
                continue;
            }

            var response = await hostingApi.GetEnvironmentHealthAsync(env.Id.Value);
            if (response is null)
            {
                // HTTP error from hosting; do nothing, next tick retries.
                continue;
            }

            env.LastHealthCheckAtUtc = DateTimeOffset.UtcNow;

            switch (response.Status)
            {
                case EnvironmentHealthReportStatus.Healthy:
                    env.HealthStatus = EnvironmentHealthStatus.Healthy;
                    env.ConsecutiveHealthFailures = 0;
                    break;

                case EnvironmentHealthReportStatus.Unhealthy:
                    env.ConsecutiveHealthFailures += 1;
                    if (env.ConsecutiveHealthFailures >= UnhealthyFlipThreshold)
                    {
                        env.HealthStatus = EnvironmentHealthStatus.Unhealthy;
                    }
                    break;

                case EnvironmentHealthReportStatus.Unknown:
                    // Not configured — leave HealthStatus alone, don't bump failure counter.
                    break;
            }

            await environmentRepository.UpdateAsync(env);
        }
    }
}
```

- [ ] **Step 3: Register the recurring job**

In `backend/Api.Hosting/ApiHostingFeature.cs`, inside `ConfigureApp` after the existing `app.AddRecurringJob<…>` calls:

```csharp
        app.AddRecurringJob<ProjectEnvironmentHealthCheckJob>(new Cron("* * * * *")); // every minute
```

**Note:** Hangfire's standard cron resolution is per-minute, not per-30s. We run this at 1-minute cadence; the `healthcheck.interval` field in compozerr.json is still meaningful as the *expected* probe spacing, and can tighten in a follow-up if Hangfire is configured for sub-minute schedules. Log this as a follow-up if it matters.

- [ ] **Step 4: Build and run existing tests**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health
dotnet build backend/Base.sln
dotnet test backend/Api.Tests/Api.Tests.csproj
```

Expected: build succeeds, tests pass.

- [ ] **Step 5: Commit**

```bash
git add backend/Api.Hosting/Jobs/ProjectEnvironmentHealthCheckJob.cs backend/Api.Hosting/ApiHostingFeature.cs backend/Api.Data/Repositories/ProjectEnvironmentRepository.cs
git commit -m "feat(health): add recurring ProjectEnvironmentHealthCheckJob"
```

---

## Task 13: [web] Extend project/environment DTO with health fields + regenerate client

**Files:**
- Modify: whichever endpoint returns `Project` / `ProjectEnvironment` for the detail and list pages. Find via:
  ```bash
  grep -rln "api.v1.getProjects\|api.v1.getProjectById\|/v1/projects" frontend/src
  ```
  then follow to the backend endpoint (likely under `backend/Api.Hosting/Endpoints/Projects/` or `backend/Api/Endpoints/Projects/`). Identify the response DTO class.
- Modify: `frontend/src/generated/*` (via regeneration command)

- [ ] **Step 1: Find the response DTOs**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health
grep -rln "class.*ProjectResponse\|record.*ProjectResponse\|class.*ProjectDetail" backend/Api.Hosting/Endpoints backend/Api/Endpoints 2>/dev/null | head -5
```

Open those files. There will typically be two DTOs: one for the project list and one for the project detail (with environments nested).

- [ ] **Step 2: Add health fields to the environment DTO**

Wherever the environment DTO is defined (likely alongside the project detail DTO), add:

```csharp
public EnvironmentHealthStatus HealthStatus { get; set; }
public DateTimeOffset? LastHealthCheckAtUtc { get; set; }
```

In the mapping code that populates the DTO from `ProjectEnvironment`, copy those fields across.

- [ ] **Step 3: Add derived `DisplayState` to the project DTO**

On the project-level DTO, add a `DisplayState` (or similarly named) field of type `ProjectState`. In the mapping code:

```csharp
DisplayState = (project.State == ProjectState.Running &&
                project.ProjectEnvironments != null &&
                project.ProjectEnvironments.Any(e => e.Type != EnvironmentType.Preview &&
                                                     e.HealthStatus == EnvironmentHealthStatus.Unhealthy))
               ? ProjectState.Unhealthy
               : project.State,
```

Do this for both the list and detail responses. If the list query doesn't eagerly load `ProjectEnvironments`, update the query to `.Include(p => p.ProjectEnvironments)` in the repository.

- [ ] **Step 4: Regenerate the OpenAPI frontend client**

Start the backend first and wait for swagger:

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health
compozerr dev &
until curl -sf http://localhost:1235/swagger/v1/swagger.json > /dev/null; do sleep 1; done

cd frontend
npx openapi-qraft \
    --plugin tanstack-query-react \
    --plugin openapi-typescript \
    http://localhost:1235/swagger/v1/swagger.json \
    --clean -o src/generated \
    --export-type --properties-required-by-default --default-non-nullable
```

Verify: `grep -r "HealthStatus\|DisplayState" src/generated | head` — both should appear in the generated types.

- [ ] **Step 5: Commit**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health
git add backend frontend/src/generated
git commit -m "feat(api): expose env health + derived project DisplayState"
```

---

## Task 14: [web] Update status pill + env cards in the frontend

**Files:**
- Modify: `frontend/src/routes/_auth/_dashboard/projects/$projectId/index.tsx`
- Modify: `frontend/src/routes/_auth/_dashboard/projects/index.tsx`

- [ ] **Step 1: Read the existing status-rendering code**

Open `frontend/src/routes/_auth/_dashboard/projects/$projectId/index.tsx`, locate the status pill around line 51-75. Note the icon import, color classes, and tooltip.

- [ ] **Step 2: Add the `Unhealthy` branch**

Extend the status pill's switch/map (actual syntax depends on how the existing file does it — match the pattern). Example shape:

```tsx
// In the icon + color map used to render the pill:
case "Unhealthy":
    return {
        label: "Unhealthy",
        icon: AlertTriangle,
        className: "bg-red-100 text-red-700 border-red-300",
        tooltip: "Containers are running, but the primary service is not responding.",
    };
```

Import `AlertTriangle` from `lucide-react`.

Consume `project.displayState` (or whatever name Task 13 used) instead of `project.state` here.

- [ ] **Step 3: Show per-env health on the detail page**

Where environments are listed on the project detail page, add a small indicator per env:

```tsx
{env.healthStatus === "Unhealthy" && (
    <span className="inline-flex items-center gap-1 text-xs text-red-700">
        <AlertTriangle className="h-3 w-3" />
        Not responding
        {env.lastHealthCheckAtUtc && (
            <span className="text-muted-foreground">
                · checked {formatDistanceToNow(new Date(env.lastHealthCheckAtUtc))} ago
            </span>
        )}
    </span>
)}
```

Import `formatDistanceToNow` from `date-fns` (already in the project per package.json typical).

- [ ] **Step 4: Update the projects list view**

In `frontend/src/routes/_auth/_dashboard/projects/index.tsx`, the same status-pill render logic applies — reuse the same status map (extract to a shared helper if duplicated, but only if it's duplicated; otherwise copy the case). Show `Unhealthy` in the status column. Don't add a new filter for now.

- [ ] **Step 5: Type-check**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health/frontend
npm run typecheck
```

Expected: no errors.

- [ ] **Step 6: Manual UI test**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health
compozerr dev
```

Open http://localhost:1234. Verify:
- Existing projects still render their status badges correctly (Running, Stopped, etc.).
- If you can simulate `Unhealthy` locally (e.g., set `HealthStatus = Unhealthy` on a dev-seeded env directly in the DB, then refresh the list), the badge renders with the new red `AlertTriangle` icon and tooltip.

Per user memory: always verify in browser before claiming done.

- [ ] **Step 7: Commit**

```bash
git add frontend/src/routes/_auth/_dashboard/projects
git commit -m "feat(ui): render Unhealthy status on project badge + env cards"
```

---

## Task 15: [web] End-to-end smoke test

**Files:** none

- [ ] **Step 1: Start both services fresh**

```bash
cd /Users/milo/milodev/gits/web/.worktrees/feature-primary-service-health
compozerr dev
```

Wait for backend + frontend + migrations.

- [ ] **Step 2: Manually verify healthy path**

In a SQL shell, pick an active environment and set its project's compozerr.json (on the deployed VM, via the hosting's VM if this is against production — otherwise dev-seed):

If you're testing locally without real VMs, you can short-circuit by mocking `HostingApi.GetEnvironmentHealthAsync` for a dev environment or by writing `HealthStatus = Healthy` manually to confirm the UI changes. A full end-to-end test against a real deployed project is part of the rollout step below, not this task.

For a DB-only smoke test:

```sql
-- Pick any active env
UPDATE api."ProjectEnvironments"
SET "HealthStatus" = 2, "LastHealthCheckAtUtc" = NOW(), "ConsecutiveHealthFailures" = 2
WHERE "Id" = '<pick-one>';
```

Refresh the project list — the project should show `Unhealthy` (assuming Project.State == Running and env is non-preview).

Revert:
```sql
UPDATE api."ProjectEnvironments"
SET "HealthStatus" = 0, "ConsecutiveHealthFailures" = 0
WHERE "Id" = '<same>';
```

- [ ] **Step 3: No commit — this is a verification task**

---

## Post-implementation rollout (NOT part of this plan, but mentioned for handoff)

After the two PRs (web + hosting) are merged:

1. Add `healthcheck.service: "web"`, `healthcheck.path: "/health"` (or whatever 2xx endpoint) to kurvo-backend's compozerr.json.
2. Trigger a redeploy; the next deploy should blue-green correctly (no more `Temp container failed health check` warning).
3. The periodic job picks up the env within ~1 minute and should read `Healthy` against the real service.
4. Do the same for Compozerr's own compozerr.json and blog.

---

## Self-review notes

- Tasks are ordered so each builds on state in earlier tasks; there are no forward references to undefined types.
- Task 5 and Task 7 both touch `http-probe.ts` (Task 4 prerequisite). Order is preserved.
- Task 13's DTO additions are consumed in Task 14 — the UI task explicitly uses `project.displayState` / `env.healthStatus`. Generated types (Task 13 Step 4) must exist before Task 14.
- Task 10 (migration) can run independently of Tasks 8/9 in principle, but both entity changes must be committed first so the migration generator sees them.
- No Hangfire sub-minute dependency: the job runs at 1-minute cadence; `healthcheck.interval` is currently informational.
- Blue-green "skip when unset" is the documented behavior change; rollout step adds `healthcheck.service` to first-party configs so they don't lose zero-downtime deploys.
