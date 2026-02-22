using Github.Models;

namespace Cli.Endpoints.Modules.SyncStatus;

public sealed record GetModuleSyncStatusResponse(
    IReadOnlyList<ModuleSyncStatusItem> Syncs);

public sealed record ModuleSyncStatusItem(
    string ModuleName,
    ModuleSyncStatus Status,
    string SourceCommitHash,
    string? TargetCommitHash,
    int FilesChanged,
    DateTime? CompletedAt,
    string? ErrorMessage);
