using Core.MediatR;

namespace Cli.Endpoints.Modules.Info;

public sealed record GetModuleInfoCommand(
    string Organization,
    string ModuleName) : ICommand<GetModuleInfoResponse>;
