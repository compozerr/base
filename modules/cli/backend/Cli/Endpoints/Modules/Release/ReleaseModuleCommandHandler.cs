using Core.MediatR;
using Auth.Services;
using Github.Services;

namespace Cli.Endpoints.Modules.Release;

public sealed class ReleaseModuleCommandHandler(
    IGithubService GithubService,
    ICurrentUserAccessor CurrentUserAccessor) :
        ICommandHandler<ReleaseModuleCommand, ReleaseModuleResponse>
{
    public async Task<ReleaseModuleResponse> Handle(
        ReleaseModuleCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserAccessor.CurrentUserId
            ?? throw new InvalidOperationException("User not authenticated");

        try
        {
            // Get user's GitHub client
            var client = await GithubService.GetUserClient(userId)
                ?? throw new InvalidOperationException("GitHub not connected");

            // The module repo is expected to exist under the user's default installation
            // For now, create a git tag on the module's source repo
            // The CLI provides the module name, and the backend resolves the repo

            // TODO: Look up module source repo from registry
            // For now, return success stub
            return new ReleaseModuleResponse(
                Success: true,
                TagName: $"v{command.Version}",
                ErrorMessage: null);
        }
        catch (Exception ex)
        {
            return new ReleaseModuleResponse(
                Success: false,
                TagName: null,
                ErrorMessage: ex.Message);
        }
    }
}
