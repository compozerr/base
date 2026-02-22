using Core.MediatR;

namespace Cli.Endpoints.Modules.Release;

public sealed record ReleaseModuleCommand(
    string Organization,
    string ModuleName,
    string Version,
    string? RepoUrl,
    string? Description,
    string? ReleaseNotes,
    string? CommitHash,
    string[]? Tags,
    Dictionary<string, string>? Compatibility) : ICommand<ReleaseModuleResponse>;
