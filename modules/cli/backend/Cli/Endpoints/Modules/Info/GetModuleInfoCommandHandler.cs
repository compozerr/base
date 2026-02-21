using Core.MediatR;

namespace Cli.Endpoints.Modules.Info;

public sealed class GetModuleInfoCommandHandler :
    ICommandHandler<GetModuleInfoCommand, GetModuleInfoResponse>
{
    public Task<GetModuleInfoResponse> Handle(
        GetModuleInfoCommand command,
        CancellationToken cancellationToken = default)
    {
        // TODO: Look up from marketplace registry
        return Task.FromResult(new GetModuleInfoResponse(
            Found: false,
            Name: null,
            Description: null,
            LatestVersion: null,
            RepoUrl: null,
            Tags: null,
            Versions: null,
            InstallCount: 0));
    }
}
