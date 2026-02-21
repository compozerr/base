namespace Cli.Endpoints.Modules.Info;

public sealed record GetModuleInfoResponse(
    bool Found,
    string? Name,
    string? Description,
    string? LatestVersion,
    string? RepoUrl,
    string[]? Tags,
    string[]? Versions,
    int InstallCount);
