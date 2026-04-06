# Task 18: GitHub Webhook Auto-Sync — Detailed Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** When a developer pushes to their project repo, the Compozerr backend detects changes in `modules/` directories and automatically syncs those changes to each module's source repo.

**Architecture:** Hooks into the existing GitHub webhook pipeline. A new background job (`ModuleSyncProcessorJob`) runs alongside the existing `PushWebhookProcessorJob`. It uses the GitHub API (via Octokit) to compare commits, identify module-scoped changes, and apply them to module source repos using the Git Data API (tree/blob/commit).

**Tech Stack:** .NET 9, MediatR/Carter, Octokit, Hangfire, Entity Framework Core, PostgreSQL

**Repo:** `/Users/milo/milodev/gits/web`

---

## Context: Existing Infrastructure

The webhook pipeline already handles push events end-to-end:

```
GitHub push → /github/webhooks (Octokit.Webhooks.AspNetCore)
  → DefaultWebhookEventProcessor.ProcessPushWebhookAsync()
    → Stores PushEvent as JSONB in github.PushWebhookEvents
    → Enqueues PushWebhookProcessorJob (Hangfire)
      → Looks up Project by RepoUri
      → Deploys if conditions met
```

**Key existing components we'll use:**
- `IGithubService` — GitHub App JWT/installation token management (`modules/github/backend/Github/Services/GithubService.cs`)
- `PushWebhookEvent` — JSONB-stored push event with full Octokit `PushEvent` payload (`modules/github/backend/Github/Models/PushWebhookEvent.cs`)
- `DefaultWebhookEventProcessor` — Entry point that stores events and enqueues jobs (`modules/github/backend/Github/Services/DefaultWebhookEventProcessor.cs`)
- `JobBase<T, TArg>` — Hangfire background job base class with distributed locking (`modules/jobs/backend/Jobs/JobBase.cs`)
- `IModuleRepository` — Access to `Module` entity (Name + RepoUri) (`backend/Api.Data/Repositories/ModuleRepository.cs`)
- `IProjectRepository` — Access to `Project` entity (RepoUri + GithubInstallationId) (`backend/Api.Data/Repositories/ProjectRepository.cs`)
- `IPushWebhookEventRepository` — Push event storage + project lookup by git URL (`modules/github/backend/Github/Repositories/PushWebhookEventRepository.cs`)

**Database entities:**
- `Module` → `{ Name: string, RepoUri: Uri }` — maps module names to source repos
- `Project` → `{ RepoUri: Uri, GithubInstallationId: long? }` — maps projects to GitHub repos

---

## High-Level Flow

```
1. Push webhook arrives (already handled)
2. DefaultWebhookEventProcessor stores event and enqueues BOTH:
   a. PushWebhookProcessorJob (existing — deploys)
   b. ModuleSyncProcessorJob (NEW — syncs modules)

3. ModuleSyncProcessorJob:
   a. Get project from RepoUri
   b. Get GitHub installation client for the project
   c. Compare before/after commits to get changed files
   d. Group changed files by modules/ prefix
   e. For each changed module:
      i.   Look up Module.RepoUri in database
      ii.  Skip if no source repo registered (local-only module)
      iii. Get installation client for the module source repo
      iv.  Fetch current tree from module source repo (main branch)
      v.   Build new tree with the changed files (remap paths)
      vi.  Create commit on module source repo
      vii. Update main branch ref
   f. Record sync result (success/failure per module)
```

---

## Sub-Tasks

### Task 18.1: Create ModuleSyncEvent entity and migration

**Files:**
- Create: `modules/github/backend/Github/Models/ModuleSyncEvent.cs`
- Create: `modules/github/backend/Github/Abstractions/ModuleSyncEventId.cs` (in Github.Abstractions project)
- Modify: `modules/github/backend/Github/Data/GithubDbContext.cs` — add DbSet
- Create: Migration via `dotnet ef migrations add AddModuleSyncEvent`

**Entity:**
```csharp
// modules/github/backend/Github/Models/ModuleSyncEvent.cs
using System.ComponentModel.DataAnnotations.Schema;
using Database.Models;
using Github.Abstractions;

namespace Github.Models;

public sealed class ModuleSyncEvent : BaseEntityWithId<ModuleSyncEventId>
{
    /// <summary>The push webhook event that triggered this sync</summary>
    public required PushWebhookEventId PushWebhookEventId { get; set; }

    /// <summary>The project that was pushed to</summary>
    public required ProjectId ProjectId { get; set; }

    /// <summary>Module name (e.g., "stripe")</summary>
    public required string ModuleName { get; set; }

    /// <summary>Source repo URL for the module</summary>
    public required Uri ModuleRepoUri { get; set; }

    /// <summary>Commit hash on the project repo that triggered this sync</summary>
    public required string SourceCommitHash { get; set; }

    /// <summary>Commit hash created on the module source repo (null if not yet synced)</summary>
    public string? TargetCommitHash { get; set; }

    /// <summary>Number of files changed in this module</summary>
    public int FilesChanged { get; set; }

    public ModuleSyncStatus Status { get; set; } = ModuleSyncStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum ModuleSyncStatus
{
    Pending,
    Syncing,
    Succeeded,
    Failed,
    Skipped // e.g., no source repo, [skip sync], no changes
}
```

**ID type:**
```csharp
// modules/github/backend/Github.Abstractions/ModuleSyncEventId.cs (or add to existing file)
using Database.Models;

namespace Github.Abstractions;

public sealed class ModuleSyncEventId : IdBase<ModuleSyncEventId>;
```

**Step 1:** Create `ModuleSyncEventId` in Github.Abstractions project
**Step 2:** Create `ModuleSyncEvent` entity
**Step 3:** Add `DbSet<ModuleSyncEvent>` to `GithubDbContext`
**Step 4:** Run migration:
```bash
cd /Users/milo/milodev/gits/web
dotnet ef migrations add AddModuleSyncEvent \
  --project modules/github/backend/Github/Github.csproj \
  --startup-project backend/Api/Api.csproj \
  --context GithubDbContext
```
**Step 5:** Verify build: `cd backend && dotnet build`
**Step 6:** Commit:
```bash
git add modules/github/backend/
git commit -m "feat: add ModuleSyncEvent entity and migration"
```

---

### Task 18.2: Create ModuleSyncEvent repository

**Files:**
- Create: `modules/github/backend/Github/Repositories/ModuleSyncEventRepository.cs`
- Modify: `modules/github/backend/Github/GithubFeature.cs` — register in DI

**Repository:**
```csharp
// modules/github/backend/Github/Repositories/ModuleSyncEventRepository.cs
using Database.Repositories;
using Github.Abstractions;
using Github.Data;
using Github.Models;

namespace Github.Repositories;

public interface IModuleSyncEventRepository
    : IGenericRepository<ModuleSyncEvent, ModuleSyncEventId, GithubDbContext>
{
    Task<IReadOnlyList<ModuleSyncEvent>> GetByPushWebhookEventIdAsync(
        PushWebhookEventId pushWebhookEventId);
}

public sealed class ModuleSyncEventRepository(
    GithubDbContext context)
    : GenericRepository<ModuleSyncEvent, ModuleSyncEventId, GithubDbContext>(context),
      IModuleSyncEventRepository
{
    public async Task<IReadOnlyList<ModuleSyncEvent>> GetByPushWebhookEventIdAsync(
        PushWebhookEventId pushWebhookEventId)
    {
        return await GetFilteredAsync(x => x.PushWebhookEventId == pushWebhookEventId);
    }
}
```

**Step 1:** Create repository file
**Step 2:** Register `IModuleSyncEventRepository` → `ModuleSyncEventRepository` in `GithubFeature.ConfigureServices`
**Step 3:** Build: `cd backend && dotnet build`
**Step 4:** Commit:
```bash
git add modules/github/backend/
git commit -m "feat: add ModuleSyncEvent repository"
```

---

### Task 18.3: Create ModuleSyncService (core sync logic)

This is the core service that takes a push event's changed files and syncs them to module source repos using the GitHub Git Data API.

**Files:**
- Create: `modules/github/backend/Github/Services/ModuleSyncService.cs`
- Modify: `modules/github/backend/Github/GithubFeature.cs` — register in DI

**Service interface:**
```csharp
public interface IModuleSyncService
{
    /// <summary>
    /// Analyze a push event to determine which modules have changes.
    /// Returns list of (moduleName, changedFiles) tuples.
    /// </summary>
    Task<IReadOnlyList<ModuleChangeSet>> DetectModuleChangesAsync(
        IGitHubClient client,
        string repoOwner,
        string repoName,
        string beforeSha,
        string afterSha);

    /// <summary>
    /// Sync changes for a single module to its source repo.
    /// Creates a new commit on the module source repo's main branch.
    /// </summary>
    Task<string> SyncModuleToSourceRepoAsync(
        IGitHubClient projectClient,
        string projectOwner,
        string projectName,
        string afterSha,
        string moduleName,
        IReadOnlyList<string> changedFiles,
        IGitHubClient moduleClient,
        string moduleOwner,
        string moduleRepoName);
}

public record ModuleChangeSet(
    string ModuleName,
    IReadOnlyList<string> ChangedFiles);
```

**Implementation approach for `DetectModuleChangesAsync`:**
1. Use `client.Repository.Commit.Compare(owner, repo, beforeSha, afterSha)` to get `CompareResult`
2. Filter `CompareResult.Files` where `Filename.StartsWith("modules/")`
3. Group by module name (extract from path: `modules/{name}/...`)
4. Return list of `ModuleChangeSet`

**Implementation approach for `SyncModuleToSourceRepoAsync`:**
1. Get current `main` branch ref on module source repo: `moduleClient.Git.Reference.Get(moduleOwner, moduleRepoName, "heads/main")`
2. Get the current tree: `moduleClient.Git.Tree.Get(moduleOwner, moduleRepoName, currentSha)`
3. For each changed file in this module:
   a. Get file content from project repo at `afterSha`: `projectClient.Repository.Content.GetRawContentByRef(projectOwner, projectName, filePath, afterSha)`
   b. Remap path: `modules/{moduleName}/path/to/file` → `path/to/file`
   c. Create blob: `moduleClient.Git.Blob.Create(moduleOwner, moduleRepoName, newBlob)`
4. Handle deleted files: check `CompareResult.Files[].Status == "removed"` → omit from new tree
5. Create new tree with all changes: `moduleClient.Git.Tree.Create(moduleOwner, moduleRepoName, newTree)` using `BaseTree` = current tree SHA
6. Create commit: `moduleClient.Git.Commit.Create(moduleOwner, moduleRepoName, newCommit)` with parent = current SHA, message = `"sync: {original commit message} (from {projectName})"`
7. Update ref: `moduleClient.Git.Reference.Update(moduleOwner, moduleRepoName, "heads/main", refUpdate)`
8. Return new commit SHA

**Edge cases to handle:**
- `beforeSha` is all zeros (first push / new branch) → compare against parent of `afterSha`
- Binary files → use blob API with base64 encoding
- File deletions → omit from tree
- File renames → treated as delete + add
- Empty module changeset after filtering → skip

**Step 1:** Create `IModuleSyncService` interface and `ModuleSyncService` implementation
**Step 2:** Register in DI
**Step 3:** Build: `cd backend && dotnet build`
**Step 4:** Commit:
```bash
git add modules/github/backend/
git commit -m "feat: add ModuleSyncService for GitHub Git Data API sync"
```

---

### Task 18.4: Create ModuleSyncProcessorJob (background job)

**Files:**
- Create: `modules/github/backend/Github/Jobs/ModuleSyncProcessorJob.cs`

**Job:**
```csharp
public sealed class ModuleSyncProcessorJob(
    IPushWebhookEventRepository pushWebhookEventRepository,
    IModuleSyncEventRepository moduleSyncEventRepository,
    IModuleRepository moduleRepository,
    IProjectRepository projectRepository,
    IGithubService githubService,
    IModuleSyncService moduleSyncService)
    : JobBase<ModuleSyncProcessorJob, PushWebhookEventId>
{
    public override async Task ExecuteAsync(PushWebhookEventId pushWebhookEventId) { ... }
}
```

**Flow:**
1. Load `PushWebhookEvent` by ID
2. Skip if already handled/errored
3. Extract `RepoUri` from push event
4. Look up `ProjectId` from `RepoUri`
5. Load `Project` to get `GithubInstallationId`
6. Skip if commit message contains `[skip sync]`
7. Skip if not pushing to `main` branch
8. Get installation client: `githubService.GetInstallationClientByInstallationIdAsync(installationId)`
9. Parse owner/repo from RepoUri
10. Call `moduleSyncService.DetectModuleChangesAsync()` with `beforeSha` and `afterSha` from push event
11. For each changed module:
    a. Look up `Module` by name in database
    b. If no Module record → create `ModuleSyncEvent` with `Status = Skipped`
    c. Parse module source repo owner/name from `Module.RepoUri`
    d. Get installation client for module source repo (might be same installation, might be different)
    e. Create `ModuleSyncEvent` with `Status = Syncing`
    f. Call `moduleSyncService.SyncModuleToSourceRepoAsync()`
    g. Update `ModuleSyncEvent` with result (Succeeded + target commit SHA, or Failed + error)

**Important: Installation token for module source repos**
- The project's GitHub App installation might not have access to module source repos
- Module source repos (e.g., `compozerr/stripe`) are under a different organization
- Solution: The Compozerr GitHub App must be installed on both the project org AND the module source org
- For now: Use the same installation client. If it fails with 403, log a clear error explaining the user needs to install the app on the module org too.
- Future: Add `GithubInstallationId` to the `Module` entity for explicit installation mapping.

**Step 1:** Create job file
**Step 2:** Build: `cd backend && dotnet build`
**Step 3:** Commit:
```bash
git add modules/github/backend/
git commit -m "feat: add ModuleSyncProcessorJob background job"
```

---

### Task 18.5: Wire ModuleSyncProcessorJob into webhook pipeline

**Files:**
- Modify: `modules/github/backend/Github/Services/DefaultWebhookEventProcessor.cs`

**Change:** Add `ModuleSyncProcessorJob.Enqueue(entity.Id)` alongside the existing `PushWebhookProcessorJob.Enqueue(entity.Id)`:

```csharp
public sealed class DefaultWebhookEventProcessor(
    IPushWebhookEventRepository pushWebhookEventRepository) : WebhookEventProcessor
{
    protected override async Task ProcessPushWebhookAsync(
        WebhookHeaders headers, PushEvent pushEvent)
    {
        var entity = await pushWebhookEventRepository.AddAsync(
            new() { Event = pushEvent });

        // Deploy pipeline (existing)
        PushWebhookProcessorJob.Enqueue(entity.Id);

        // Module sync pipeline (new)
        ModuleSyncProcessorJob.Enqueue(entity.Id);
    }
}
```

Both jobs run independently. Deploy doesn't wait for sync. Sync doesn't wait for deploy.

**Step 1:** Add the `ModuleSyncProcessorJob.Enqueue` call
**Step 2:** Add using for `Github.Jobs`
**Step 3:** Build: `cd backend && dotnet build`
**Step 4:** Commit:
```bash
git add modules/github/backend/
git commit -m "feat: wire module sync job into webhook pipeline"
```

---

### Task 18.6: Add module sync status endpoint

**Files:**
- Create: `modules/cli/backend/Cli/Endpoints/Modules/SyncStatus/GetModuleSyncStatusCommand.cs`
- Create: `modules/cli/backend/Cli/Endpoints/Modules/SyncStatus/GetModuleSyncStatusResponse.cs`
- Create: `modules/cli/backend/Cli/Endpoints/Modules/SyncStatus/GetModuleSyncStatusCommandHandler.cs`
- Create: `modules/cli/backend/Cli/Endpoints/Modules/SyncStatus/GetModuleSyncStatusRoute.cs`
- Modify: `modules/cli/backend/Cli/Endpoints/Modules/ModulesGroup.cs` — register route

This gives `compozerr module status` the ability to show sync status from the server.

**Endpoint:** `GET /v1/cli/modules/sync-status?projectId={projectId}`

**Response:**
```json
{
  "syncs": [
    {
      "moduleName": "stripe",
      "status": "Succeeded",
      "sourceCommitHash": "abc123",
      "targetCommitHash": "def456",
      "filesChanged": 3,
      "completedAt": "2026-02-20T12:00:00Z"
    }
  ]
}
```

**Step 1:** Create the 4 endpoint files following existing MediatR/Carter pattern
**Step 2:** Register in ModulesGroup
**Step 3:** Build: `cd backend && dotnet build`
**Step 4:** Commit:
```bash
git add modules/cli/backend/
git commit -m "feat: add module sync status endpoint"
```

---

### Task 18.7: Write integration tests

**Files:**
- Create: `modules/github/backend/Github.Tests/ModuleSyncServiceTests.cs`

Test the core sync detection logic:

1. **Test: DetectModuleChanges filters correctly**
   - Mock `CompareResult` with files in and outside `modules/`
   - Assert only module files are returned, grouped correctly

2. **Test: DetectModuleChanges handles empty diff**
   - Mock empty `CompareResult`
   - Assert returns empty list

3. **Test: Skip sync when commit message contains [skip sync]**
   - Verify job exits early

4. **Test: Skip sync for non-main branches**
   - Verify job exits early

5. **Test: Skipped status when module has no source repo**
   - Module not in database
   - Assert `ModuleSyncEvent.Status = Skipped`

For the actual Git Data API calls (SyncModuleToSourceRepoAsync), those are best tested with a real webhook against a test repo. Document manual test steps:

```
Manual test:
1. Set up ngrok to tunnel to local backend
2. Configure GitHub webhook on a test project repo
3. Make a change in modules/template/ directory
4. Push to test repo
5. Verify: ModuleSyncEvent created with Status = Succeeded
6. Verify: Module source repo has new commit with the changes
```

**Step 1:** Create test file with unit tests for DetectModuleChanges and job logic
**Step 2:** Run tests: `dotnet test modules/github/backend/Github.Tests/`
**Step 3:** Commit:
```bash
git add modules/github/backend/
git commit -m "test: add ModuleSyncService unit tests"
```

---

## Dependency Order

```
18.1 (Entity + Migration)
  → 18.2 (Repository)
    → 18.3 (SyncService - core logic)
      → 18.4 (Background job)
        → 18.5 (Wire into webhook)
        → 18.6 (Status endpoint)
    → 18.7 (Tests - can start after 18.3, independent of 18.5/18.6)
```

Tasks 18.5 and 18.6 are independent of each other and can be done in parallel after 18.4.

---

## Risk Areas & Mitigations

### 1. GitHub API Rate Limits
- **Risk:** High-frequency pushes could exhaust rate limits
- **Mitigation:** Installation tokens get 5,000 req/hour per installation. Module sync uses ~5 API calls per module per push. At 100 pushes/hour with 5 modules each, that's 2,500 calls — within limits.
- **Future:** Add rate limit tracking and backoff

### 2. Large Diffs
- **Risk:** A push that changes 100+ files across modules could be slow
- **Mitigation:** Process modules sequentially within a job; each module sync is atomic. If the job times out, it can be retried. Add a configurable max-files-per-sync limit.

### 3. Merge Conflicts on Module Source Repo
- **Risk:** If someone pushes directly to the module source repo while a sync is happening
- **Mitigation:** The Git Data API creates commits at a specific parent SHA. If the ref has moved, the ref update will fail. The job should catch this, log it, and mark the sync as Failed with a clear message. The developer can then run `compozerr module update` to pull the latest from the source repo.

### 4. Cross-Organization Access
- **Risk:** The GitHub App installed on `user-org` may not have access to `compozerr-lib` repos
- **Mitigation:** Initially log clear errors. Future: Add per-module installation ID configuration.

### 5. Binary Files
- **Risk:** Binary files need base64 encoding in blobs
- **Mitigation:** Use `Encoding = EncodingType.Base64` when creating blobs for non-text content. Detect via file extension or content type.

---

## What This Does NOT Cover (Future Work)

- **Module source repo → project sync** (reverse direction). This is handled by `compozerr module update` CLI command.
- **Cross-project sync** (push to project A → sync to module → notify project B). Future marketplace feature.
- **Conflict resolution UI**. For now, conflicts surface as errors in sync status.
- **Per-module installation ID configuration**. For now, uses project's installation.
- **Webhook secret rotation / management UI**.
