using Core.MediatR;

namespace Cli.Endpoints.Modules.Add;

public sealed record AddModuleCommand(
    string Organization,
    string ModuleName,
    string? CommitHash
    ) : ICommand<AddModuleCommandResponse>;
