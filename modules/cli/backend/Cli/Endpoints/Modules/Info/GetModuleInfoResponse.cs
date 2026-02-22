namespace Cli.Endpoints.Modules.Info;

public sealed record GetModuleInfoResponse(
    bool Found,
    string? Name,
    string? Organization,
    string? Description,
    string? LatestVersion,
    string? RepoUrl,
    string[]? Tags,
    ModuleVersionDto[]? Versions,
    int InstallCount,
    string? IconUrl,
    Dictionary<string, string>? Compatibility,
    DateTime? PublishedAt);

public sealed record ModuleVersionDto(
    string Version,
    string CommitHash,
    string? ReleaseNotes,
    DateTime CreatedAt);
