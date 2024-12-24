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

        var clientResponse = await GithubService.GetInstallationClientByUserDefaultAsync(
            userId,
            command.Type);

        var userInstallations = await GithubService.GetInstallationsForUserAsync(userId);

        var currentInstallation = userInstallations.Single(
            userInstallation => userInstallation.InstallationId == clientResponse.InstallationId);

        var response = await clientResponse.InstallationClient.Repository.Generate(
            "compozerr",
            "base",
            new(command.Name)
            {
                Private = true,
                Description = "Created by compozerr.com",
                Owner = currentInstallation.Name
            });

        var cloneUrl = $"https://x-access-token:{clientResponse.InstallationToken}@github.com/{response.FullName}.git";
        var gitUrl = $"https://github.com/{response.FullName}.git";

        return new CreateRepoResponse(
            cloneUrl,
            gitUrl,
            response.Name);
    }
}