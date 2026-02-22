using Api.Abstractions;
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
    Skipped
}
