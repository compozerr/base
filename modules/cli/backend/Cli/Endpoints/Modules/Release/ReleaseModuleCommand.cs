using Core.MediatR;

namespace Cli.Endpoints.Modules.Release;

public sealed record ReleaseModuleCommand(
    string ModuleName,
    string Version,
    string? ReleaseNotes) : ICommand<ReleaseModuleResponse>;
