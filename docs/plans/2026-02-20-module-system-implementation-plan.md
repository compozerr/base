# Module System Redesign — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace git submodule-based module system with git subtree-based system where modules are just directories, managed via `compozerr module` CLI subcommands.

**Architecture:** CLI wraps `git subtree` with `--squash` for add/update/push. compozerr.json is the single source of truth for dependencies. A lockfile (compozerr.lock) tracks exact source repos and commit hashes. Server-side auto-sync via GitHub webhooks pushes module changes to source repos.

**Tech Stack:** Bun + TypeScript CLI, Ink/React for TUI, Zod for validation, git subtree for VCS, .NET 9 backend (MediatR/Carter), PostgreSQL.

**Repos:**
- CLI: `/Users/milo/milodev/gits/cli`
- Web: `/Users/milo/milodev/gits/web`

---

## Phase 1: Foundation (CLI Types & Utilities)

All work in CLI repo: `/Users/milo/milodev/gits/cli`

---

### Task 1: Update compozerr-file Zod schema

**Files:**
- Modify: `src/types/compozerr-file.ts`
- Create: `src/tests/compozerr-file.test.ts`

**Step 1: Write tests for new schema**

```typescript
// src/tests/compozerr-file.test.ts
import { describe, test, expect } from "bun:test";
import { CompozerrFile, CompozerrProjectFile, CompozerrModuleFile } from "../types/compozerr-file";

describe("CompozerrFile", () => {
  test("parses project file with modules", () => {
    const input = {
      type: "project",
      name: "web",
      id: "0198a99a-d92f-70ff-b90d-201bc173963a",
      modules: {
        "stripe": "compozerr/stripe@^1.0.0",
        "auth": "compozerr-lib/auth@latest",
      },
    };
    const result = CompozerrProjectFile.safeParse(input);
    expect(result.success).toBe(true);
    expect(result.data!.modules).toEqual(input.modules);
  });

  test("parses module file with origin and backend", () => {
    const input = {
      type: "module",
      name: "stripe",
      version: "1.2.0",
      description: "Stripe billing",
      origin: {
        repo: "https://github.com/compozerr/stripe",
        commit: "abc123",
        addedAt: "2026-02-20T10:30:00Z",
      },
      modules: { "auth": "compozerr-lib/auth@^2.0.0" },
      backend: {
        projects: ["Stripe/Stripe.csproj"],
        envDefaults: { "STRIPE_API_KEY": "" },
      },
      frontend: {
        routesDir: "frontend/src/routes",
        package: "@repo/stripe",
      },
      services: {
        start: "docker compose up -d",
        stop: "docker compose down",
      },
    };
    const result = CompozerrModuleFile.safeParse(input);
    expect(result.success).toBe(true);
  });

  test("parses minimal module file", () => {
    const input = { type: "module", name: "simple" };
    const result = CompozerrModuleFile.safeParse(input);
    expect(result.success).toBe(true);
  });

  test("rejects module file with invalid type", () => {
    const input = { type: "invalid", name: "test" };
    const result = CompozerrFile.safeParse(input);
    expect(result.success).toBe(false);
  });

  // Backward compat: old "dependencies" field still works
  test("parses legacy project file with dependencies field", () => {
    const input = {
      type: "project",
      name: "web",
      dependencies: { "template": "latest" },
    };
    const result = CompozerrProjectFile.safeParse(input);
    expect(result.success).toBe(true);
  });
});
```

**Step 2: Run test to verify it fails**

Run: `cd /Users/milo/milodev/gits/cli && bun test src/tests/compozerr-file.test.ts`
Expected: FAIL — `CompozerrProjectFile` and `CompozerrModuleFile` don't exist yet.

**Step 3: Implement the new schema**

```typescript
// src/types/compozerr-file.ts
import { z } from "zod";

export const TemplateType = z.enum([
  "dockerfile",
  "docker-compose",
  "compozerr-standard"
]);
export type TemplateType = z.infer<typeof TemplateType>;

// --- Origin (set by CLI when adding/updating a module) ---
const Origin = z.object({
  repo: z.string(),
  commit: z.string(),
  addedAt: z.string(),
});

// --- Backend config ---
const BackendConfig = z.object({
  projects: z.array(z.string()).optional(),
  envDefaults: z.record(z.string()).optional(),
}).optional();

// --- Frontend config ---
const FrontendConfig = z.object({
  routesDir: z.string().optional(),
  srcDir: z.string().optional(),
  routePrefix: z.string().optional(),
  package: z.string().optional(),
  defaultEnvProperties: z.record(z.string()).optional(),
}).optional();

// --- Services config ---
const ServicesConfig = z.object({
  start: z.string().optional(),
  stop: z.string().optional(),
  readyCheck: z.string().optional(),
}).optional();

// --- Marketplace metadata ---
const MarketplaceConfig = z.object({
  tags: z.array(z.string()).optional(),
  screenshots: z.array(z.string()).optional(),
  compatibility: z.record(z.string()).optional(),
}).optional();

// --- Project-level compozerr.json ---
export const CompozerrProjectFile = z.object({
  type: z.literal("project"),
  name: z.string(),
  id: z.string().optional(),
  modules: z.record(z.string()).optional(),
  // Backward compat
  dependencies: z.record(z.string()).optional(),
  // Legacy fields
  start: z.string().optional(),
  dev: z.string().optional(),
  templateType: TemplateType.optional(),
  templateFile: z.string().optional(),
});

// --- Module-level compozerr.json ---
export const CompozerrModuleFile = z.object({
  type: z.literal("module"),
  name: z.string(),
  version: z.string().optional(),
  description: z.string().optional(),
  origin: Origin.optional(),
  modules: z.record(z.string()).optional(),
  // Backward compat
  dependencies: z.record(z.string()).optional(),
  backend: BackendConfig,
  frontend: FrontendConfig,
  services: ServicesConfig,
  marketplace: MarketplaceConfig,
  // Legacy fields
  start: z.string().optional(),
  startupTimeoutMs: z.number().optional(),
  readyMessage: z.string().optional(),
  end: z.string().optional(),
  port: z.string().optional(),
  dockerComposeFile: z.string().optional(),
  dev: z.string().optional(),
  templateType: TemplateType.optional(),
  templateFile: z.string().optional(),
});

// --- Union type (either project or module) ---
export const CompozerrFile = z.discriminatedUnion("type", [
  CompozerrProjectFile,
  CompozerrModuleFile,
]);

export type CompozerrFile = z.infer<typeof CompozerrFile>;
export type CompozerrProjectFile = z.infer<typeof CompozerrProjectFile>;
export type CompozerrModuleFile = z.infer<typeof CompozerrModuleFile>;
```

Note: Keep all legacy fields so existing compozerr.json files still parse. The new `modules` field is preferred over `dependencies`.

**Step 4: Run test to verify it passes**

Run: `cd /Users/milo/milodev/gits/cli && bun test src/tests/compozerr-file.test.ts`
Expected: All PASS

**Step 5: Commit**

```bash
cd /Users/milo/milodev/gits/cli
git add src/types/compozerr-file.ts src/tests/compozerr-file.test.ts
git commit -m "feat: update compozerr-file schema with modules, origin, backend, frontend, services fields"
```

---

### Task 2: Create lockfile utility

**Files:**
- Create: `src/utils/lockfile.ts`
- Create: `src/tests/lockfile.test.ts`

**Step 1: Write tests for lockfile read/write/update**

```typescript
// src/tests/lockfile.test.ts
import { describe, test, expect, beforeEach, afterEach } from "bun:test";
import { mkdtempSync, writeFileSync, rmSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { tmpdir } from "node:os";
import { Lockfile, type LockfileData, type LockfileModuleEntry } from "../utils/lockfile";

describe("Lockfile", () => {
  let tempDir: string;

  beforeEach(() => {
    tempDir = mkdtempSync(join(tmpdir(), "lockfile-test-"));
  });

  afterEach(() => {
    rmSync(tempDir, { recursive: true, force: true });
  });

  test("read returns empty lockfile when file does not exist", () => {
    const lockfile = Lockfile.read(join(tempDir, "compozerr.lock"));
    expect(lockfile.lockVersion).toBe(1);
    expect(Object.keys(lockfile.modules)).toHaveLength(0);
  });

  test("write and read roundtrip", () => {
    const filePath = join(tempDir, "compozerr.lock");
    const data: LockfileData = {
      lockVersion: 1,
      modules: {
        stripe: {
          source: "https://github.com/compozerr/stripe",
          version: "1.2.0",
          resolvedCommit: "abc123",
          installedAt: "2026-02-20T10:00:00Z",
          updatedAt: "2026-02-20T10:00:00Z",
          integrity: "sha256-test",
          dependencies: { auth: "^2.0.0" },
        },
      },
    };

    Lockfile.write(filePath, data);
    const result = Lockfile.read(filePath);
    expect(result).toEqual(data);
  });

  test("upsertModule adds new module", () => {
    const filePath = join(tempDir, "compozerr.lock");
    Lockfile.write(filePath, { lockVersion: 1, modules: {} });

    const entry: LockfileModuleEntry = {
      source: "https://github.com/compozerr/stripe",
      version: "1.0.0",
      resolvedCommit: "abc",
      installedAt: "2026-02-20T10:00:00Z",
      updatedAt: "2026-02-20T10:00:00Z",
      integrity: "sha256-x",
      dependencies: {},
    };

    Lockfile.upsertModule(filePath, "stripe", entry);
    const result = Lockfile.read(filePath);
    expect(result.modules.stripe).toEqual(entry);
  });

  test("upsertModule updates existing module", () => {
    const filePath = join(tempDir, "compozerr.lock");
    Lockfile.write(filePath, {
      lockVersion: 1,
      modules: {
        stripe: {
          source: "https://github.com/compozerr/stripe",
          version: "1.0.0",
          resolvedCommit: "old",
          installedAt: "2026-01-01T00:00:00Z",
          updatedAt: "2026-01-01T00:00:00Z",
          integrity: "sha256-old",
          dependencies: {},
        },
      },
    });

    Lockfile.upsertModule(filePath, "stripe", {
      source: "https://github.com/compozerr/stripe",
      version: "1.1.0",
      resolvedCommit: "new",
      installedAt: "2026-01-01T00:00:00Z",
      updatedAt: "2026-02-20T12:00:00Z",
      integrity: "sha256-new",
      dependencies: {},
    });

    const result = Lockfile.read(filePath);
    expect(result.modules.stripe.version).toBe("1.1.0");
    expect(result.modules.stripe.resolvedCommit).toBe("new");
  });

  test("removeModule deletes module entry", () => {
    const filePath = join(tempDir, "compozerr.lock");
    Lockfile.write(filePath, {
      lockVersion: 1,
      modules: {
        stripe: {
          source: "x", version: "1.0.0", resolvedCommit: "a",
          installedAt: "x", updatedAt: "x", integrity: "x", dependencies: {},
        },
        auth: {
          source: "y", version: "2.0.0", resolvedCommit: "b",
          installedAt: "y", updatedAt: "y", integrity: "y", dependencies: {},
        },
      },
    });

    Lockfile.removeModule(filePath, "stripe");
    const result = Lockfile.read(filePath);
    expect(result.modules.stripe).toBeUndefined();
    expect(result.modules.auth).toBeDefined();
  });

  test("getModule returns entry or undefined", () => {
    const filePath = join(tempDir, "compozerr.lock");
    Lockfile.write(filePath, {
      lockVersion: 1,
      modules: {
        stripe: {
          source: "x", version: "1.0.0", resolvedCommit: "a",
          installedAt: "x", updatedAt: "x", integrity: "x", dependencies: {},
        },
      },
    });

    expect(Lockfile.getModule(filePath, "stripe")).toBeDefined();
    expect(Lockfile.getModule(filePath, "nonexistent")).toBeUndefined();
  });
});
```

**Step 2: Run test to verify it fails**

Run: `cd /Users/milo/milodev/gits/cli && bun test src/tests/lockfile.test.ts`
Expected: FAIL — `../utils/lockfile` doesn't exist.

**Step 3: Implement lockfile utility**

```typescript
// src/utils/lockfile.ts
import { readFileSync, writeFileSync, existsSync } from "node:fs";

export interface LockfileModuleEntry {
  source: string;
  version: string;
  resolvedCommit: string;
  installedAt: string;
  updatedAt: string;
  integrity: string;
  dependencies: Record<string, string>;
}

export interface LockfileData {
  lockVersion: number;
  modules: Record<string, LockfileModuleEntry>;
}

const EMPTY_LOCKFILE: LockfileData = { lockVersion: 1, modules: {} };

export class Lockfile {
  static read(filePath: string): LockfileData {
    if (!existsSync(filePath)) return { ...EMPTY_LOCKFILE, modules: {} };
    const raw = readFileSync(filePath, "utf-8");
    return JSON.parse(raw) as LockfileData;
  }

  static write(filePath: string, data: LockfileData): void {
    writeFileSync(filePath, JSON.stringify(data, null, 2) + "\n", "utf-8");
  }

  static getModule(filePath: string, name: string): LockfileModuleEntry | undefined {
    const data = this.read(filePath);
    return data.modules[name];
  }

  static upsertModule(filePath: string, name: string, entry: LockfileModuleEntry): void {
    const data = this.read(filePath);
    data.modules[name] = entry;
    this.write(filePath, data);
  }

  static removeModule(filePath: string, name: string): void {
    const data = this.read(filePath);
    delete data.modules[name];
    this.write(filePath, data);
  }
}
```

**Step 4: Run test to verify it passes**

Run: `cd /Users/milo/milodev/gits/cli && bun test src/tests/lockfile.test.ts`
Expected: All PASS

**Step 5: Commit**

```bash
cd /Users/milo/milodev/gits/cli
git add src/utils/lockfile.ts src/tests/lockfile.test.ts
git commit -m "feat: add lockfile utility for compozerr.lock management"
```

---

### Task 3: Add git subtree methods to Git utility

**Files:**
- Modify: `src/utils/git.ts`
- Create: `src/tests/git-subtree.test.ts`

**Step 1: Write tests for subtree methods**

Note: Subtree operations require a real git repo. These are integration tests.

```typescript
// src/tests/git-subtree.test.ts
import { describe, test, expect, beforeEach, afterEach } from "bun:test";
import { mkdtempSync, rmSync, mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { tmpdir } from "node:os";

// We test the parsing/arg-building helpers, not actual git subtree
// (those are integration tests that need real repos)
import { Git } from "../utils/git";

describe("Git.parseModuleRef", () => {
  test("parses org/module@version", () => {
    const result = Git.parseModuleRef("compozerr/stripe@^1.0.0");
    expect(result.org).toBe("compozerr");
    expect(result.module).toBe("stripe");
    expect(result.ref).toBe("^1.0.0");
  });

  test("parses org/module@commitHash", () => {
    const result = Git.parseModuleRef("compozerr/stripe@abc123def");
    expect(result.org).toBe("compozerr");
    expect(result.module).toBe("stripe");
    expect(result.ref).toBe("abc123def");
  });

  test("parses org/module without version (defaults to latest)", () => {
    const result = Git.parseModuleRef("compozerr/stripe");
    expect(result.org).toBe("compozerr");
    expect(result.module).toBe("stripe");
    expect(result.ref).toBe("latest");
  });

  test("parses module@version without org", () => {
    const result = Git.parseModuleRef("stripe@1.0.0");
    expect(result.org).toBeUndefined();
    expect(result.module).toBe("stripe");
    expect(result.ref).toBe("1.0.0");
  });
});

describe("Git.buildSubtreeAddCommand", () => {
  test("builds correct command with squash", () => {
    const cmd = Git.buildSubtreeAddCommand({
      prefix: "modules/stripe",
      repo: "https://github.com/compozerr/stripe",
      ref: "main",
    });
    expect(cmd).toBe(
      'git subtree add --prefix="modules/stripe" "https://github.com/compozerr/stripe" main --squash'
    );
  });
});

describe("Git.buildSubtreePullCommand", () => {
  test("builds correct command", () => {
    const cmd = Git.buildSubtreePullCommand({
      prefix: "modules/stripe",
      repo: "https://github.com/compozerr/stripe",
      ref: "main",
    });
    expect(cmd).toBe(
      'git subtree pull --prefix="modules/stripe" "https://github.com/compozerr/stripe" main --squash'
    );
  });
});
```

**Step 2: Run test to verify it fails**

Run: `cd /Users/milo/milodev/gits/cli && bun test src/tests/git-subtree.test.ts`
Expected: FAIL — `Git.parseModuleRef`, `Git.buildSubtreeAddCommand`, etc. don't exist.

**Step 3: Add subtree methods to Git class**

Add the following methods to the existing `Git` class in `src/utils/git.ts`:

```typescript
// Add to class Git in src/utils/git.ts:

  static parseModuleRef(ref: string): { org?: string; module: string; ref: string } {
    const [namePart, versionPart] = ref.split("@");
    const parts = namePart.split("/");
    if (parts.length === 2) {
      return { org: parts[0], module: parts[1], ref: versionPart || "latest" };
    }
    return { module: parts[0], ref: versionPart || "latest" };
  }

  static buildSubtreeAddCommand(opts: { prefix: string; repo: string; ref: string }): string {
    return `git subtree add --prefix="${opts.prefix}" "${opts.repo}" ${opts.ref} --squash`;
  }

  static buildSubtreePullCommand(opts: { prefix: string; repo: string; ref: string }): string {
    return `git subtree pull --prefix="${opts.prefix}" "${opts.repo}" ${opts.ref} --squash`;
  }

  static buildSubtreePushCommand(opts: { prefix: string; repo: string; ref: string }): string {
    return `git subtree push --prefix="${opts.prefix}" "${opts.repo}" ${opts.ref}`;
  }

  static async subtreeAddAsync(projectRoot: string, prefix: string, repo: string, ref: string): Promise<void> {
    const cmd = this.buildSubtreeAddCommand({ prefix, repo, ref });
    await this.executeGitCommandAsync(`cd "${projectRoot}" && ${cmd}`);
    Logger.verbose(`Subtree added: ${prefix} from ${repo}@${ref}`);
  }

  static async subtreePullAsync(projectRoot: string, prefix: string, repo: string, ref: string): Promise<void> {
    const cmd = this.buildSubtreePullCommand({ prefix, repo, ref });
    await this.executeGitCommandAsync(`cd "${projectRoot}" && ${cmd}`);
    Logger.verbose(`Subtree pulled: ${prefix} from ${repo}@${ref}`);
  }

  static async subtreePushAsync(projectRoot: string, prefix: string, repo: string, ref: string): Promise<void> {
    const cmd = this.buildSubtreePushCommand({ prefix, repo, ref });
    await this.executeGitCommandAsync(`cd "${projectRoot}" && ${cmd}`);
    Logger.verbose(`Subtree pushed: ${prefix} to ${repo}@${ref}`);
  }
```

Note: `executeGitCommandAsync` is private — it needs to be changed to `protected` or keep the subtree methods inside the class.

**Step 4: Run test to verify it passes**

Run: `cd /Users/milo/milodev/gits/cli && bun test src/tests/git-subtree.test.ts`
Expected: All PASS

**Step 5: Commit**

```bash
cd /Users/milo/milodev/gits/cli
git add src/utils/git.ts src/tests/git-subtree.test.ts
git commit -m "feat: add git subtree methods (add, pull, push, parseModuleRef)"
```

---

### Task 4: Create auto-wirer utility

**Files:**
- Create: `src/utils/auto-wirer.ts`
- Create: `src/tests/auto-wirer.test.ts`

**Step 1: Write tests**

```typescript
// src/tests/auto-wirer.test.ts
import { describe, test, expect } from "bun:test";
import { AutoWirer } from "../utils/auto-wirer";

describe("AutoWirer.buildSolutionAddCommands", () => {
  test("generates dotnet sln commands from backend.projects", () => {
    const commands = AutoWirer.buildSolutionAddCommands("stripe", {
      projects: ["Stripe/Stripe.csproj"],
    });
    expect(commands).toHaveLength(1);
    expect(commands[0]).toContain("dotnet sln");
    expect(commands[0]).toContain("Stripe/Stripe.csproj");
  });

  test("returns empty array when no backend projects", () => {
    const commands = AutoWirer.buildSolutionAddCommands("stripe", undefined);
    expect(commands).toHaveLength(0);
  });

  test("handles multiple csproj files", () => {
    const commands = AutoWirer.buildSolutionAddCommands("stripe", {
      projects: ["Stripe/Stripe.csproj", "Stripe.Abstractions/Stripe.Abstractions.csproj"],
    });
    expect(commands).toHaveLength(2);
  });
});

describe("AutoWirer.buildProjectReferenceCommands", () => {
  test("generates dotnet add reference commands", () => {
    const commands = AutoWirer.buildProjectReferenceCommands("stripe", {
      projects: ["Stripe/Stripe.csproj"],
    });
    expect(commands).toHaveLength(1);
    expect(commands[0]).toContain("dotnet add");
    expect(commands[0]).toContain("reference");
  });
});
```

**Step 2: Run test to verify it fails**

Run: `cd /Users/milo/milodev/gits/cli && bun test src/tests/auto-wirer.test.ts`
Expected: FAIL

**Step 3: Implement auto-wirer**

```typescript
// src/utils/auto-wirer.ts
import { join } from "node:path";
import { ShCommand } from "./sh-command";
import { CompozerrProjectService } from "./compozerr-project-service";
import { Logger } from "./logger";

interface BackendConfig {
  projects?: string[];
  envDefaults?: Record<string, string>;
}

export class AutoWirer {
  static buildSolutionAddCommands(moduleName: string, backend: BackendConfig | undefined): string[] {
    if (!backend?.projects?.length) return [];
    const root = CompozerrProjectService.getRootFolder();
    return backend.projects.map(proj => {
      const csprojPath = join(root, "modules", moduleName, "backend", proj);
      return `cd "${root}/backend" && dotnet sln *.sln add "${csprojPath}"`;
    });
  }

  static buildProjectReferenceCommands(moduleName: string, backend: BackendConfig | undefined): string[] {
    if (!backend?.projects?.length) return [];
    const root = CompozerrProjectService.getRootFolder();
    return backend.projects.map(proj => {
      const csprojPath = join(root, "modules", moduleName, "backend", proj);
      return `cd "${root}/backend" && dotnet add Api/Api.csproj reference "${csprojPath}"`;
    });
  }

  static async wireBackendAsync(moduleName: string, backend: BackendConfig | undefined): Promise<void> {
    const slnCommands = this.buildSolutionAddCommands(moduleName, backend);
    const refCommands = this.buildProjectReferenceCommands(moduleName, backend);

    for (const cmd of [...slnCommands, ...refCommands]) {
      try {
        await new ShCommand(cmd).executeAsync();
      } catch (err) {
        Logger.warning(`Auto-wire command failed: ${cmd}`);
        Logger.verbose(`Error: ${(err as Error).message}`);
      }
    }
  }

  static async unwireBackendAsync(moduleName: string, backend: BackendConfig | undefined): Promise<void> {
    if (!backend?.projects?.length) return;
    const root = CompozerrProjectService.getRootFolder();

    for (const proj of backend.projects) {
      const csprojPath = join(root, "modules", moduleName, "backend", proj);
      try {
        await new ShCommand(
          `cd "${root}/backend" && dotnet remove Api/Api.csproj reference "${csprojPath}"`
        ).executeAsync();
      } catch {
        Logger.verbose(`Could not remove reference to ${proj}`);
      }
      try {
        await new ShCommand(
          `cd "${root}/backend" && dotnet sln *.sln remove "${csprojPath}"`
        ).executeAsync();
      } catch {
        Logger.verbose(`Could not remove ${proj} from solution`);
      }
    }
  }
}
```

Note: The `buildSolutionAddCommands` and `buildProjectReferenceCommands` static methods are pure (for testing). The `wireBackendAsync` / `unwireBackendAsync` methods actually execute commands. Tests that call `CompozerrProjectService.getRootFolder()` will need to be run inside a project or mocked; the unit tests above only test the command-building methods which can be tested in isolation if we refactor to accept root as a param. Adjust accordingly during implementation — the key is that the logic for building commands is testable.

**Step 4: Run test to verify it passes**

Run: `cd /Users/milo/milodev/gits/cli && bun test src/tests/auto-wirer.test.ts`
Expected: PASS (may need to mock `CompozerrProjectService.getRootFolder()` — if so, make `buildSolutionAddCommands` accept a `rootFolder` param instead)

**Step 5: Commit**

```bash
cd /Users/milo/milodev/gits/cli
git add src/utils/auto-wirer.ts src/tests/auto-wirer.test.ts
git commit -m "feat: add auto-wirer utility for .sln and .csproj management"
```

---

### Task 5: Create module version parser utility

**Files:**
- Create: `src/utils/module-ref.ts`
- Create: `src/tests/module-ref.test.ts`

**Step 1: Write tests**

```typescript
// src/tests/module-ref.test.ts
import { describe, test, expect } from "bun:test";
import { parseModuleRef, buildGitUrl } from "../utils/module-ref";

describe("parseModuleRef", () => {
  test("parses org/module@semver", () => {
    const r = parseModuleRef("compozerr/stripe@^1.0.0");
    expect(r).toEqual({ org: "compozerr", name: "stripe", ref: "^1.0.0" });
  });

  test("parses org/module@latest", () => {
    const r = parseModuleRef("compozerr/stripe@latest");
    expect(r).toEqual({ org: "compozerr", name: "stripe", ref: "latest" });
  });

  test("parses org/module@commitHash", () => {
    const r = parseModuleRef("compozerr/stripe@abc123def456");
    expect(r).toEqual({ org: "compozerr", name: "stripe", ref: "abc123def456" });
  });

  test("parses org/module without ref defaults to latest", () => {
    const r = parseModuleRef("compozerr/stripe");
    expect(r).toEqual({ org: "compozerr", name: "stripe", ref: "latest" });
  });

  test("parses org/module@branch", () => {
    const r = parseModuleRef("your-org/github@main");
    expect(r).toEqual({ org: "your-org", name: "github", ref: "main" });
  });
});

describe("buildGitUrl", () => {
  test("builds https github url from org and name", () => {
    expect(buildGitUrl("compozerr", "stripe")).toBe(
      "https://github.com/compozerr/stripe"
    );
  });
});
```

**Step 2: Run test to verify it fails**

Run: `cd /Users/milo/milodev/gits/cli && bun test src/tests/module-ref.test.ts`
Expected: FAIL

**Step 3: Implement**

```typescript
// src/utils/module-ref.ts
export interface ModuleRef {
  org: string;
  name: string;
  ref: string;
}

export function parseModuleRef(input: string): ModuleRef {
  const [namePart, refPart] = input.split("@");
  const segments = namePart.split("/");

  if (segments.length < 2) {
    throw new Error(`Invalid module reference: "${input}". Expected format: org/module[@version]`);
  }

  return {
    org: segments[0],
    name: segments[1],
    ref: refPart || "latest",
  };
}

export function buildGitUrl(org: string, name: string): string {
  return `https://github.com/${org}/${name}`;
}
```

**Step 4: Run test to verify it passes**

Run: `cd /Users/milo/milodev/gits/cli && bun test src/tests/module-ref.test.ts`
Expected: All PASS

**Step 5: Commit**

```bash
cd /Users/milo/milodev/gits/cli
git add src/utils/module-ref.ts src/tests/module-ref.test.ts
git commit -m "feat: add module reference parser (org/module@version)"
```

---

## Phase 2: CLI Commands — Core CRUD

---

### Task 6: Create `compozerr module` parent command

**Files:**
- Create: `src/commands/module.command.tsx`
- Modify: `src/commands/index.ts`

**Step 1: Create the parent command**

```tsx
// src/commands/module.command.tsx
import { Command as CommanderCommand } from "commander";
import {
  Command,
  BaseCommand,
  CommandContext,
  Output,
  renderOnce,
} from "../core";
import { Box, Text } from "ink";
import React from "react";
import { theme } from "../ui/theme";

@Command({
  name: "module",
  description: "Manage modules in your project",
  group: "Module Commands",
})
export class ModuleCommand extends BaseCommand {
  setup(cmd: CommanderCommand): CommanderCommand {
    return cmd;
  }

  async execute(ctx: CommandContext): Promise<void> {
    const output = Output.from(ctx);

    if (output.isJsonMode()) {
      await output.error(
        new (await import("../core")).CliError(
          "Specify a subcommand: new, add, remove, update, status, release, publish, search, info",
          "MISSING_REQUIRED_ARG",
          true,
          {
            type: "PROVIDE_ARG",
            description: "Specify a module subcommand",
            command: "compozerr module status",
          }
        )
      );
      return;
    }

    renderOnce(
      <Box flexDirection="column">
        <Text bold color={theme.colors.primary}>
          Usage: compozerr module {"<command>"}
        </Text>
        <Box marginTop={1} flexDirection="column">
          <Text color={theme.colors.textMuted}>Available commands:</Text>
          <Box marginLeft={2} flexDirection="column">
            <Text><Text color={theme.colors.secondary}>new</Text><Text color={theme.colors.textDim}>      Create a new module from template</Text></Text>
            <Text><Text color={theme.colors.secondary}>add</Text><Text color={theme.colors.textDim}>      Add an existing module to your project</Text></Text>
            <Text><Text color={theme.colors.secondary}>remove</Text><Text color={theme.colors.textDim}>   Remove a module</Text></Text>
            <Text><Text color={theme.colors.secondary}>update</Text><Text color={theme.colors.textDim}>   Pull upstream changes</Text></Text>
            <Text><Text color={theme.colors.secondary}>status</Text><Text color={theme.colors.textDim}>   Show module status and warnings</Text></Text>
            <Text><Text color={theme.colors.secondary}>release</Text><Text color={theme.colors.textDim}>  Version and publish a module</Text></Text>
            <Text><Text color={theme.colors.secondary}>publish</Text><Text color={theme.colors.textDim}>  Create source repo from local module</Text></Text>
            <Text><Text color={theme.colors.secondary}>search</Text><Text color={theme.colors.textDim}>   Search the marketplace</Text></Text>
            <Text><Text color={theme.colors.secondary}>info</Text><Text color={theme.colors.textDim}>     Show module details</Text></Text>
          </Box>
        </Box>
      </Box>
    );
  }
}
```

**Step 2: Register in barrel file**

Add to `src/commands/index.ts`:
```typescript
import "./module.command";
```

**Step 3: Verify it loads**

Run: `cd /Users/milo/milodev/gits/cli && bun run src/main.ts module`
Expected: Shows the help text with available subcommands.

**Step 4: Commit**

```bash
cd /Users/milo/milodev/gits/cli
git add src/commands/module.command.tsx src/commands/index.ts
git commit -m "feat: add 'compozerr module' parent command"
```

---

### Task 7: Create `compozerr module status` command

**Files:**
- Create: `src/commands/module-status.command.tsx`
- Modify: `src/commands/index.ts`

This is the simplest command to start with and exercises lockfile + module discovery.

**Step 1: Implement the status command**

```tsx
// src/commands/module-status.command.tsx
import { Command as CommanderCommand } from "commander";
import {
  Command,
  BaseCommand,
  CommandContext,
  Output,
  renderOnce,
} from "../core";
import { Box, Text } from "ink";
import React from "react";
import { theme } from "../ui/theme";
import { CompozerrProjectService } from "../utils/compozerr-project-service";
import { Lockfile } from "../utils/lockfile";
import { readdirSync, existsSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { CompozerrModuleFile } from "../types/compozerr-file";

interface ModuleStatusEntry {
  name: string;
  localVersion: string;
  sourceVersion: string;
  status: "up_to_date" | "modified" | "update_available" | "unregistered";
  detail?: string;
}

@Command({
  name: "status",
  description: "Show module status and warnings",
  parent: "module",
  group: "Module Commands",
})
export class ModuleStatusCommand extends BaseCommand {
  setup(cmd: CommanderCommand): CommanderCommand {
    return cmd;
  }

  async execute(ctx: CommandContext): Promise<void> {
    const output = Output.from(ctx);
    const root = CompozerrProjectService.getRootFolder();
    const modulesDir = join(root, "modules");
    const lockfilePath = join(root, "compozerr.lock");

    if (!existsSync(modulesDir)) {
      output.log("No modules directory found.");
      return;
    }

    const lockfile = Lockfile.read(lockfilePath);
    const dirs = readdirSync(modulesDir, { withFileTypes: true })
      .filter(d => d.isDirectory() && !d.name.startsWith("."));

    const entries: ModuleStatusEntry[] = [];
    const warnings: string[] = [];

    for (const dir of dirs) {
      const compozerrPath = join(modulesDir, dir.name, "compozerr.json");
      const lockEntry = lockfile.modules[dir.name];

      if (!existsSync(compozerrPath)) {
        warnings.push(`modules/${dir.name}/ — no compozerr.json found`);
        continue;
      }

      const raw = JSON.parse(readFileSync(compozerrPath, "utf-8"));
      const parsed = CompozerrModuleFile.safeParse(raw);

      if (!parsed.success) {
        warnings.push(`modules/${dir.name}/compozerr.json — invalid schema`);
        continue;
      }

      const mod = parsed.data;

      if (!lockEntry) {
        entries.push({
          name: dir.name,
          localVersion: mod.version || "-",
          sourceVersion: "-",
          status: "unregistered",
        });
        continue;
      }

      // For now, just show version comparison
      // TODO: check actual file diffs against lockfile integrity
      const localVersion = mod.version || lockEntry.version;
      entries.push({
        name: dir.name,
        localVersion,
        sourceVersion: lockEntry.version,
        status: localVersion === lockEntry.version ? "up_to_date" : "modified",
      });
    }

    if (output.isJsonMode()) {
      await output.success({ modules: entries, warnings });
      return;
    }

    const hasModified = entries.some(e => e.status === "modified");
    const hasUnregistered = entries.some(e => e.status === "unregistered");

    renderOnce(
      <Box flexDirection="column">
        <Text bold color={theme.colors.primary}> Modules</Text>
        <Text color={theme.colors.textDim}> {"─".repeat(60)}</Text>

        {entries.filter(e => e.status !== "unregistered").map(e => (
          <Box key={e.name}>
            <Text> </Text>
            <Text color={theme.colors.text}>{e.name.padEnd(16)}</Text>
            <Text color={theme.colors.textMuted}>{e.localVersion.padEnd(10)}</Text>
            <Text color={theme.colors.textMuted}>{e.sourceVersion.padEnd(10)}</Text>
            {e.status === "up_to_date" && <Text color={theme.colors.success}>✓ Up to date</Text>}
            {e.status === "modified" && <Text color={theme.colors.warning}>● Modified locally</Text>}
            {e.status === "update_available" && <Text color={theme.colors.info}>↑ Update available</Text>}
          </Box>
        ))}

        {hasModified && (
          <Box marginTop={1}>
            <Text color={theme.colors.textDim}> Run </Text>
            <Text color={theme.colors.secondary}>compozerr module release</Text>
            <Text color={theme.colors.textDim}> to publish local changes</Text>
          </Box>
        )}

        {(hasUnregistered || warnings.length > 0) && (
          <Box marginTop={1} flexDirection="column">
            <Text bold color={theme.colors.warning}> ⚠ Warnings</Text>
            <Text color={theme.colors.textDim}> {"─".repeat(60)}</Text>

            {entries.filter(e => e.status === "unregistered").map(e => (
              <Box key={e.name} flexDirection="column">
                <Text color={theme.colors.warning}> ⚠ modules/{e.name}/  Not registered — not in compozerr.lock</Text>
                <Text color={theme.colors.textDim}>   Run 'compozerr module publish {e.name}' to create source repo</Text>
              </Box>
            ))}

            {warnings.map((w, i) => (
              <Text key={i} color={theme.colors.warning}> ⚠ {w}</Text>
            ))}
          </Box>
        )}
      </Box>
    );
  }
}
```

**Step 2: Register in barrel file**

Add to `src/commands/index.ts`:
```typescript
import "./module-status.command";
```

**Step 3: Verify manually**

Run: `cd /Users/milo/milodev/gits/web && compozerr module status`
Expected: Shows list of modules (all unregistered since no lockfile exists yet).

**Step 4: Commit**

```bash
cd /Users/milo/milodev/gits/cli
git add src/commands/module-status.command.tsx src/commands/index.ts
git commit -m "feat: add 'compozerr module status' command"
```

---

### Task 8: Rewrite `compozerr module add` command

**Files:**
- Create: `src/commands/module-add.command.tsx`
- Modify: `src/commands/index.ts`
- Modify (later remove): `src/commands/add.command.tsx`

This is the most complex command — it resolves deps, copies via subtree, and auto-wires.

**Step 1: Create the new add command**

```tsx
// src/commands/module-add.command.tsx
import { z } from "zod";
import { Command as CommanderCommand } from "commander";
import {
  Command,
  BaseCommand,
  CommandContext,
  RequireAuth,
  RequireCleanGit,
  Output,
  CliError,
  renderOnce,
} from "../core";
import { Box, Text } from "ink";
import React from "react";
import { Success } from "../ui/components";
import { theme } from "../ui/theme";
import { CompozerrProjectService } from "../utils/compozerr-project-service";
import { BackendApiClient } from "../utils/api/backend-api-client";
import { Git } from "../utils/git";
import { Lockfile, type LockfileModuleEntry } from "../utils/lockfile";
import { AutoWirer } from "../utils/auto-wirer";
import { parseModuleRef, buildGitUrl } from "../utils/module-ref";
import { existsSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { CompozerrModuleFile } from "../types/compozerr-file";

const AddModuleArgs = z.object({
  name: z.string().min(1).max(200),
});

@Command({
  name: "add",
  description: "Add a module to your project",
  args: AddModuleArgs,
  parent: "module",
  group: "Module Commands",
})
@RequireAuth()
@RequireCleanGit()
export class ModuleAddCommand extends BaseCommand<typeof AddModuleArgs> {
  setup(cmd: CommanderCommand): CommanderCommand {
    return cmd.argument("<name>", "Module reference (org/module[@version])");
  }

  async execute(ctx: CommandContext): Promise<void> {
    const { name } = this.getArgs(ctx);
    const output = Output.from(ctx);
    const root = CompozerrProjectService.getRootFolder();
    const lockfilePath = join(root, "compozerr.lock");

    const spinner = output.spinner(`Resolving ${name}...`);

    try {
      // 1. Parse module reference
      const ref = parseModuleRef(name);
      const repoUrl = buildGitUrl(ref.org, ref.name);

      // 2. Call backend to resolve dependencies
      const backendApiClient = await BackendApiClient.createAsync();
      const moduleResponse = await backendApiClient.getAddModuleAsync(name);

      if (moduleResponse.modules.some((m: any) => !m.success)) {
        spinner?.stop();
        const errors = moduleResponse.modules
          .filter((m: any) => !m.success)
          .map((m: any) => m.errorMessage)
          .join(", ");
        await output.error(new CliError(errors, "UNKNOWN_ERROR", true));
        return;
      }

      // 3. Fork modules via backend
      const projectId = await CompozerrProjectService.getProjectIdAsync();
      const forkResponse = await backendApiClient.forkModulesAsync(
        moduleResponse.modules
          .filter((x: any) => x.success)
          .map((m: any) => ({ value: m.module.fullName, hash: m.module.commitHash })),
        projectId
      );

      // 4. For each module, add via subtree + auto-wire
      for (const mod of forkResponse.forkedModules) {
        const prefix = `modules/${mod.moduleName}`;
        const moduleDir = join(root, prefix);

        // Skip if already exists
        if (existsSync(moduleDir)) {
          output.log(`Module ${mod.moduleName} already exists, skipping...`);
          continue;
        }

        spinner?.update(`Adding ${mod.moduleName}...`);

        // git subtree add
        const gitRef = mod.hash || forkResponse.sharedBranchName || "main";
        await Git.subtreeAddAsync(root, prefix, mod.cloneUrl, gitRef);

        // Read module's compozerr.json for auto-wiring
        const modConfigPath = join(moduleDir, "compozerr.json");
        if (existsSync(modConfigPath)) {
          const raw = JSON.parse(readFileSync(modConfigPath, "utf-8"));
          const parsed = CompozerrModuleFile.safeParse(raw);
          if (parsed.success) {
            spinner?.update(`Wiring ${mod.moduleName}...`);
            await AutoWirer.wireBackendAsync(mod.moduleName, parsed.data.backend ?? undefined);
          }
        }

        // Update lockfile
        const now = new Date().toISOString();
        const entry: LockfileModuleEntry = {
          source: mod.cloneUrl,
          version: "0.0.0", // Will be set properly when module has version field
          resolvedCommit: gitRef,
          installedAt: now,
          updatedAt: now,
          integrity: "", // TODO: compute hash
          dependencies: {},
        };
        Lockfile.upsertModule(lockfilePath, mod.moduleName, entry);
      }

      // 5. Commit the wiring changes
      await Git.commitChangesAsync(root, `Added module(s) and wired backend`, false);

      spinner?.stop();

      if (output.isJsonMode()) {
        await output.success(
          { modules: forkResponse.forkedModules.map((m: any) => m.moduleName) },
          `Module(s) added successfully`
        );
      } else {
        renderOnce(
          <Box flexDirection="column">
            {forkResponse.forkedModules.map((m: any) => (
              <Success key={m.moduleName}>
                Module <Text color={theme.colors.secondary}>{m.moduleName}</Text> added
              </Success>
            ))}
          </Box>
        );
      }
    } catch (error) {
      spinner?.stop();
      await output.error(
        new CliError(
          `Failed to add module: ${(error as Error).message}`,
          "UNKNOWN_ERROR",
          true,
          { type: "RETRY", description: "Check the module name and try again" }
        )
      );
    }
  }
}
```

**Step 2: Register in barrel file**

Add to `src/commands/index.ts`:
```typescript
import "./module-add.command";
```

**Step 3: Test manually with a real module**

This requires the backend running and a valid module. Test with `compozerr module add compozerr/template` on a test project.

**Step 4: Commit**

```bash
cd /Users/milo/milodev/gits/cli
git add src/commands/module-add.command.tsx src/commands/index.ts
git commit -m "feat: add 'compozerr module add' with subtree + auto-wiring"
```

---

### Task 9: Create `compozerr module remove` command

**Files:**
- Create: `src/commands/module-remove.command.tsx`
- Modify: `src/commands/index.ts`

**Step 1: Implement**

```tsx
// src/commands/module-remove.command.tsx
import { z } from "zod";
import { Command as CommanderCommand } from "commander";
import {
  Command,
  BaseCommand,
  CommandContext,
  RequireAuth,
  RequireCleanGit,
  Output,
  CliError,
  renderOnce,
} from "../core";
import { Text } from "ink";
import React from "react";
import { Success } from "../ui/components";
import { theme } from "../ui/theme";
import { CompozerrProjectService } from "../utils/compozerr-project-service";
import { Git } from "../utils/git";
import { Lockfile } from "../utils/lockfile";
import { AutoWirer } from "../utils/auto-wirer";
import { existsSync, readFileSync, rmSync } from "node:fs";
import { join } from "node:path";
import { CompozerrModuleFile } from "../types/compozerr-file";
import { Logger } from "../utils/logger";

const RemoveModuleArgs = z.object({
  name: z.string().min(1).max(100),
});

@Command({
  name: "remove",
  description: "Remove a module from your project",
  args: RemoveModuleArgs,
  parent: "module",
  group: "Module Commands",
})
@RequireAuth()
@RequireCleanGit()
export class ModuleRemoveCommand extends BaseCommand<typeof RemoveModuleArgs> {
  setup(cmd: CommanderCommand): CommanderCommand {
    return cmd.argument("<name>", "Module name to remove");
  }

  async execute(ctx: CommandContext): Promise<void> {
    const { name } = this.getArgs(ctx);
    const output = Output.from(ctx);
    const root = CompozerrProjectService.getRootFolder();
    const moduleDir = join(root, "modules", name);
    const lockfilePath = join(root, "compozerr.lock");

    if (!existsSync(moduleDir)) {
      await output.error(
        new CliError(`Module '${name}' not found in modules/`, "UNKNOWN_ERROR", true)
      );
      return;
    }

    const spinner = output.spinner(`Removing ${name}...`);

    try {
      // 1. Read module config for unwiring
      const modConfigPath = join(moduleDir, "compozerr.json");
      if (existsSync(modConfigPath)) {
        const raw = JSON.parse(readFileSync(modConfigPath, "utf-8"));
        const parsed = CompozerrModuleFile.safeParse(raw);
        if (parsed.success) {
          spinner?.update(`Unwiring ${name} from backend...`);
          await AutoWirer.unwireBackendAsync(name, parsed.data.backend ?? undefined);
        }
      }

      // 2. Remove the directory and git-track the removal
      spinner?.update(`Removing files...`);
      rmSync(moduleDir, { recursive: true, force: true });

      // 3. Remove from lockfile
      Lockfile.removeModule(lockfilePath, name);

      // 4. Commit
      await Git.commitChangesAsync(root, `Removed module ${name}`, false);

      spinner?.stop();

      if (output.isJsonMode()) {
        await output.success({ moduleName: name }, `Module ${name} removed`);
      } else {
        renderOnce(
          <Success>
            Module <Text color={theme.colors.secondary}>{name}</Text> removed
          </Success>
        );
      }
    } catch (error) {
      spinner?.stop();
      await output.error(
        new CliError(
          `Failed to remove module: ${(error as Error).message}`,
          "UNKNOWN_ERROR",
          true
        )
      );
    }
  }
}
```

**Step 2: Register and commit**

Add to `src/commands/index.ts`:
```typescript
import "./module-remove.command";
```

```bash
cd /Users/milo/milodev/gits/cli
git add src/commands/module-remove.command.tsx src/commands/index.ts
git commit -m "feat: add 'compozerr module remove' with auto-unwiring"
```

---

### Task 10: Create `compozerr module update` command

**Files:**
- Create: `src/commands/module-update.command.tsx`
- Modify: `src/commands/index.ts`

**Step 1: Implement**

```tsx
// src/commands/module-update.command.tsx
import { z } from "zod";
import { Command as CommanderCommand } from "commander";
import {
  Command,
  BaseCommand,
  CommandContext,
  RequireCleanGit,
  Output,
  CliError,
  renderOnce,
} from "../core";
import { Text } from "ink";
import React from "react";
import { Success } from "../ui/components";
import { theme } from "../ui/theme";
import { CompozerrProjectService } from "../utils/compozerr-project-service";
import { Git } from "../utils/git";
import { Lockfile } from "../utils/lockfile";
import { join } from "node:path";

const UpdateModuleArgs = z.object({
  name: z.string().optional(),
});

@Command({
  name: "update",
  description: "Pull upstream changes for a module",
  args: UpdateModuleArgs,
  parent: "module",
  group: "Module Commands",
})
@RequireCleanGit()
export class ModuleUpdateCommand extends BaseCommand<typeof UpdateModuleArgs> {
  setup(cmd: CommanderCommand): CommanderCommand {
    return cmd.argument("[name]", "Module name (or omit to check all)");
  }

  async execute(ctx: CommandContext): Promise<void> {
    const { name } = this.getArgs(ctx);
    const output = Output.from(ctx);
    const root = CompozerrProjectService.getRootFolder();
    const lockfilePath = join(root, "compozerr.lock");
    const lockfile = Lockfile.read(lockfilePath);

    const modulesToUpdate = name
      ? [name]
      : Object.keys(lockfile.modules);

    if (modulesToUpdate.length === 0) {
      output.log("No modules registered in lockfile. Run 'compozerr module add' first.");
      return;
    }

    const spinner = output.spinner("Updating modules...");

    const updated: string[] = [];
    const failed: string[] = [];

    for (const modName of modulesToUpdate) {
      const entry = lockfile.modules[modName];
      if (!entry) {
        output.log(`Module '${modName}' not in lockfile, skipping.`);
        continue;
      }

      spinner?.update(`Updating ${modName}...`);

      try {
        const prefix = `modules/${modName}`;
        // Use "main" as default branch for pull
        // TODO: store branch in lockfile or resolve from registry
        await Git.subtreePullAsync(root, prefix, entry.source, "main");

        // Update lockfile
        const now = new Date().toISOString();
        Lockfile.upsertModule(lockfilePath, modName, {
          ...entry,
          updatedAt: now,
        });

        updated.push(modName);
      } catch (err) {
        failed.push(modName);
        output.log(`Failed to update ${modName}: ${(err as Error).message}`);
      }
    }

    spinner?.stop();

    if (output.isJsonMode()) {
      await output.success({ updated, failed });
    } else {
      for (const m of updated) {
        renderOnce(
          <Success>
            <Text color={theme.colors.secondary}>{m}</Text> updated
          </Success>
        );
      }
      if (failed.length > 0) {
        output.log(`Failed to update: ${failed.join(", ")}`);
      }
    }
  }
}
```

**Step 2: Register and commit**

Add to `src/commands/index.ts`:
```typescript
import "./module-update.command";
```

```bash
cd /Users/milo/milodev/gits/cli
git add src/commands/module-update.command.tsx src/commands/index.ts
git commit -m "feat: add 'compozerr module update' using git subtree pull"
```

---

### Task 11: Move `compozerr new module` → `compozerr module new`

**Files:**
- Create: `src/commands/module-new.command.tsx`
- Modify: `src/commands/new.command.tsx` (remove NewModuleCommand class)
- Modify: `src/commands/index.ts`

**Step 1: Copy and adapt NewModuleCommand**

Take the existing `NewModuleCommand` from `new.command.tsx` and move it to its own file with `parent: "module"`. Replace `Git.addSubmoduleAsync` with `Git.subtreeAddAsync`. Replace `Git.commitAndPushSubmoduleChangesAsync` with standard `Git.commitChangesAsync`. Add lockfile entry creation.

```tsx
// src/commands/module-new.command.tsx
// (adapt from existing NewModuleCommand in new.command.tsx)
// Key changes:
// - @Command: parent: "module" instead of parent: "new"
// - Git.addSubmoduleAsync → Git.subtreeAddAsync
// - Add lockfile entry after creation
// - Add auto-wiring after scaffold
```

The implementation should follow the same pattern as the existing `NewModuleCommand` but replace all submodule calls. Refer to the existing code at `src/commands/new.command.tsx:306-440` for the full logic.

**Step 2: Remove `NewModuleCommand` from `new.command.tsx`**

Remove the `NewModuleCommand` class (lines 306-440) from `new.command.tsx`. Keep `NewCommand` (parent) and `NewProjectCommand`.

Update the help text in `NewCommand.execute()` to remove the "module" option.

**Step 3: Update barrel file**

Add to `src/commands/index.ts`:
```typescript
import "./module-new.command";
```

**Step 4: Verify both paths work**

- `compozerr module new mymod` — should work (new location)
- `compozerr new project myproject` — should still work (unchanged)

**Step 5: Commit**

```bash
cd /Users/milo/milodev/gits/cli
git add src/commands/module-new.command.tsx src/commands/new.command.tsx src/commands/index.ts
git commit -m "refactor: move 'new module' under 'compozerr module new', use subtree"
```

---

## Phase 3: CLI Commands — Versioning & Publishing

---

### Task 12: Create `compozerr module release` command

**Files:**
- Create: `src/commands/module-release.command.tsx`
- Modify: `src/commands/index.ts`

This is an interactive Ink command that shows modules with unpublished changes, lets the user pick one, enter a version, and publishes via the backend API.

**Step 1: Implement the release command**

The command should:
1. Read all modules from lockfile
2. For each, check if local files have changed (compare git status for `modules/<name>/`)
3. Show an interactive list of modified modules
4. Let user select one, enter version number
5. Call backend API to tag the version
6. Update lockfile + module's compozerr.json with new version

This requires a new backend API endpoint (Task 16). For now, implement the CLI side and stub the API call.

```tsx
// src/commands/module-release.command.tsx
// Interactive Ink UI using inquirer for selection and input
// Key flow:
// 1. Read lockfile, scan for changed modules
// 2. If no changes, show "All modules up to date"
// 3. Select module (inquirer list)
// 4. Enter version (inquirer input)
// 5. Call backendApiClient.releaseModuleAsync(moduleName, version)
// 6. Update lockfile + modules/<name>/compozerr.json
```

**Step 2: Register and commit**

```bash
cd /Users/milo/milodev/gits/cli
git add src/commands/module-release.command.tsx src/commands/index.ts
git commit -m "feat: add 'compozerr module release' interactive versioning command"
```

---

### Task 13: Create `compozerr module publish` command

**Files:**
- Create: `src/commands/module-publish.command.tsx`
- Modify: `src/commands/index.ts`

For local modules that don't have a source repo yet. Creates a new repo via the backend API and registers it.

**Step 1: Implement**

The command should:
1. Verify module exists locally and is NOT in the lockfile (or has no source)
2. Call `backendApiClient.createModuleRepo(projectId, name)` to create the repo
3. Run `git subtree push` to push the module's files to the new repo
4. Update lockfile with the new source
5. Update module's `compozerr.json` with origin

**Step 2: Register and commit**

```bash
cd /Users/milo/milodev/gits/cli
git add src/commands/module-publish.command.tsx src/commands/index.ts
git commit -m "feat: add 'compozerr module publish' for creating source repos"
```

---

### Task 14: Create `compozerr module search` and `compozerr module info` commands

**Files:**
- Create: `src/commands/module-search.command.tsx`
- Create: `src/commands/module-info.command.tsx`
- Modify: `src/commands/index.ts`

These are read-only commands that query the backend API for marketplace data.

**Step 1: Implement search**

```tsx
// src/commands/module-search.command.tsx
// Calls backend API: GET /v1/modules/search?q=<query>
// Displays table: name, description, version, installs, rating
```

**Step 2: Implement info**

```tsx
// src/commands/module-info.command.tsx
// Calls backend API: GET /v1/modules/<org>/<name>
// Displays full details: description, versions, dependencies, compatibility
```

**Step 3: Register and commit**

```bash
cd /Users/milo/milodev/gits/cli
git add src/commands/module-search.command.tsx src/commands/module-info.command.tsx src/commands/index.ts
git commit -m "feat: add 'compozerr module search' and 'compozerr module info' commands"
```

---

## Phase 4: Deprecate Old Commands

---

### Task 15: Deprecate old `add` and `remove` top-level commands

**Files:**
- Modify: `src/commands/add.command.tsx`
- Modify: `src/commands/remove.command.tsx`

**Step 1: Add deprecation warnings**

In both commands, at the start of `execute()`, add:

```typescript
output.log("⚠ 'compozerr add' is deprecated. Use 'compozerr module add' instead.");
```

Then delegate to the new command or keep the old behavior temporarily for backward compatibility.

**Step 2: Commit**

```bash
cd /Users/milo/milodev/gits/cli
git add src/commands/add.command.tsx src/commands/remove.command.tsx
git commit -m "deprecate: add deprecation notices to old add/remove commands"
```

---

## Phase 5: Backend API (Web Repo)

All work in Web repo: `/Users/milo/milodev/gits/web`

---

### Task 16: Add module release endpoint

**Files:**
- Create: `modules/cli/backend/Cli/Endpoints/Modules/ReleaseModule/`
  - `ReleaseModuleCommand.cs`
  - `ReleaseModuleEndpoint.cs`

This endpoint receives a module name + version from the CLI, verifies the commit state via GitHub, and tags the version on the module source repo.

**Step 1: Implement command + handler + endpoint**

Follow existing MediatR + Carter patterns. See `modules/cli/backend/Cli/Endpoints/` for examples.

```csharp
// ReleaseModuleCommand.cs
public sealed record ReleaseModuleCommand(
    string ModuleName,
    string Version,
    string? ReleaseNotes
) : ICommand<ReleaseModuleResponse>;

public sealed record ReleaseModuleResponse(
    bool Success,
    string? TagName,
    string? ErrorMessage
);
```

**Step 2: Implement handler**

The handler should:
1. Get current user's project
2. Verify module exists in the project's module registry
3. Use GitHub API to create a tag on the module's source repo
4. Update marketplace metadata

**Step 3: Add endpoint**

```csharp
// ReleaseModuleEndpoint.cs
public class ReleaseModuleEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/cli/modules/release", async (ReleaseModuleCommand cmd, IMediator mediator) =>
            await mediator.Send(cmd))
            .RequireAuthorization();
    }
}
```

**Step 4: Test**

Run: `dotnet test backend/Api.Tests/Api.Tests.csproj`

**Step 5: Commit**

```bash
cd /Users/milo/milodev/gits/web
git add modules/cli/backend/Cli/Endpoints/Modules/ReleaseModule/
git commit -m "feat: add module release API endpoint"
```

---

### Task 17: Add module search and info endpoints

**Files:**
- Create: `modules/cli/backend/Cli/Endpoints/Modules/SearchModules/`
- Create: `modules/cli/backend/Cli/Endpoints/Modules/GetModuleInfo/`

Follow the same MediatR + Carter pattern as Task 16.

**Step 1: Implement search endpoint**

```csharp
// GET /v1/modules/search?q=<query>&limit=20
// Returns: list of { name, description, version, installCount, rating }
```

**Step 2: Implement info endpoint**

```csharp
// GET /v1/modules/{org}/{name}
// Returns: full module details including versions, deps, compatibility
```

**Step 3: Commit**

```bash
cd /Users/milo/milodev/gits/web
git add modules/cli/backend/Cli/Endpoints/Modules/
git commit -m "feat: add module search and info API endpoints"
```

---

### Task 18: Add GitHub webhook handler for module auto-sync

**Files:**
- Create: `modules/cli/backend/Cli/Endpoints/Webhooks/ModuleSyncWebhook/`
  - `ModuleSyncWebhookCommand.cs`
  - `ModuleSyncWebhookEndpoint.cs`

This is the server-side auto-sync: when a push webhook fires, detect module changes and update source repos.

**Step 1: Implement webhook endpoint**

```csharp
// POST /v1/webhooks/github/module-sync
// Receives GitHub push webhook payload
// 1. Parse changed files from payload
// 2. Group by modules/ prefix
// 3. For each changed module, extract diff and apply to source repo
```

This is the most complex backend task. It needs:
- GitHub API access to read push diffs
- GitHub API access to commit to module source repos
- Mapping from project modules to their source repos (from lockfile or DB)

**Step 2: Test with a real webhook**

Use GitHub webhook tester or ngrok for local development.

**Step 3: Commit**

```bash
cd /Users/milo/milodev/gits/web
git add modules/cli/backend/Cli/Endpoints/Webhooks/
git commit -m "feat: add GitHub webhook handler for module auto-sync"
```

---

## Phase 6: Web Repo Schema Migration

---

### Task 19: Update compozerr.json files to new schema

**Files:**
- Modify: `compozerr.json` (root)
- Modify: `modules/*/compozerr.json` (all 14 modules)

**Step 1: Update root compozerr.json**

Add `modules` field alongside existing `dependencies` for backward compat:

```json
{
  "type": "project",
  "name": "web",
  "id": "0198a99a-d92f-70ff-b90d-201bc173963a",
  "modules": {
    "template": "compozerr/template@latest",
    "database": "compozerr-lib/database@latest",
    "auth": "compozerr-lib/auth@latest",
    "cli": "compozerr-lib/cli@latest",
    "github": "compozerr-lib/github@latest"
  },
  "dependencies": {
    "template": "latest",
    "database": "latest",
    "auth": "latest",
    "cli": "latest",
    "github": "latest"
  }
}
```

**Step 2: Update each module's compozerr.json**

For each module, add the new fields (`version`, `description`, `modules`, `backend`, `frontend`, `services`) while keeping existing fields for backward compat. This is a manual process — read each module's current config and augment it.

**Step 3: Commit**

```bash
cd /Users/milo/milodev/gits/web
git add compozerr.json modules/*/compozerr.json
git commit -m "feat: update compozerr.json files to new schema (backward compat)"
```

---

### Task 20: Update dev orchestration to read new services field

**Files:**
- Modify: `scripts/utils/added-modules.ts` (or equivalent)

**Step 1: Update module discovery**

The dev script currently reads `start`, `end`, `readyMessage`, `port`, `startupTimeoutMs` from compozerr.json. Update it to also check the new `services` field:

```typescript
// Prefer new services field, fall back to legacy
const startCmd = module.config.services?.start || module.config.start;
const stopCmd = module.config.services?.stop || module.config.end;
```

**Step 2: Test**

Run: `cd /Users/milo/milodev/gits/web && npm run dev`
Verify all module services still start correctly.

**Step 3: Commit**

```bash
cd /Users/milo/milodev/gits/web
git add scripts/
git commit -m "feat: support new services field in compozerr.json for dev orchestration"
```

---

## Implementation Order Summary

| # | Task | Repo | Phase | Depends On |
|---|------|------|-------|------------|
| 1 | Update Zod schema | CLI | Foundation | — |
| 2 | Lockfile utility | CLI | Foundation | — |
| 3 | Git subtree methods | CLI | Foundation | — |
| 4 | Auto-wirer utility | CLI | Foundation | — |
| 5 | Module ref parser | CLI | Foundation | — |
| 6 | `module` parent command | CLI | Core | 1 |
| 7 | `module status` | CLI | Core | 1, 2, 6 |
| 8 | `module add` | CLI | Core | 1-6 |
| 9 | `module remove` | CLI | Core | 1, 2, 4, 6 |
| 10 | `module update` | CLI | Core | 2, 3, 6 |
| 11 | Move `new module` | CLI | Core | 2, 3, 4, 6 |
| 12 | `module release` | CLI | Versioning | 2, 6, 16 |
| 13 | `module publish` | CLI | Versioning | 2, 3, 6 |
| 14 | `module search/info` | CLI | Versioning | 6, 17 |
| 15 | Deprecate old commands | CLI | Cleanup | 8, 9 |
| 16 | Release API endpoint | Web | Backend | — |
| 17 | Search/Info API endpoints | Web | Backend | — |
| 18 | Webhook auto-sync | Web | Backend | 16 |
| 19 | Update compozerr.json files | Web | Migration | 1 |
| 20 | Update dev orchestration | Web | Migration | 19 |

Tasks 1-5 can be done in parallel. Tasks 16-17 can be done in parallel with CLI work.
