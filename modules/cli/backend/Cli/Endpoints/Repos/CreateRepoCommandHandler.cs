using Auth.Services;
using Core.MediatR;
using Github.Services;

namespace Cli.Endpoints.Repos;

public sealed record CreateRepoCommandHandler(
    IGithubService GithubService,
    ICurrentUserAccessor CurrentUserAccessor) : ICommandHandler<CreateRepoCommand, CreateRepoResponse>
{
    public async Task<CreateRepoResponse> Handle(CreateRepoCommand command, CancellationToken cancellationToken = default)
    {
        var (installationClient, _) = await GithubService.GetInstallationClientByUserDefaultAsync(
            CurrentUserAccessor.CurrentUserId!,
            command.Type);

        var response = await installationClient.Repository.Create(new(command.Name) { Private = true });

        return new CreateRepoResponse(response.Url);
    }
}