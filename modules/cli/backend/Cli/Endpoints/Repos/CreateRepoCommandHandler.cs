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
        var userId = CurrentUserAccessor.CurrentUserId!;

        var (installationClient, installationId) = await GithubService.GetInstallationClientByUserDefaultAsync(
            userId,
            command.Type);

        var userInstallations = await GithubService.GetInstallationsForUserAsync(userId);

        var currentInstallation = userInstallations.Single(userInstallation => userInstallation.InstallationId == installationId);

        var response = await installationClient!.Repository.Generate(
            "compozerr",
            "base",
            new(command.Name)
            {
                Private = true,
                Description = "Created by compozerr.com",
                Owner = currentInstallation.Name
            });

        return new CreateRepoResponse(response.HtmlUrl);
    }
}