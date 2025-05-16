using Auth.Services;
using Core.MediatR;
using Github.Services;
using GE = Github.Endpoints.SetDefaultInstallationId;

namespace Cli.Endpoints.Modules.Add;

public sealed class AddModuleCommandHandler(
    IGithubService GithubService,
    ICurrentUserAccessor CurrentUserAccessor) : ICommandHandler<AddModuleCommand, AddModuleResponse>
{
    public async Task<AddModuleResponse> Handle(
        AddModuleCommand command,
        CancellationToken cancellationToken = default)
    {
        var clientResponse = await GithubService.GetInstallationClientByUserDefaultAsync(
            CurrentUserAccessor.CurrentUserId ?? throw new InvalidOperationException("User not found"),
            GE.DefaultInstallationIdSelectionType.Modules);

        var organizationsForUser = await GithubService.GetInstallationsForUserAsync(CurrentUserAccessor.CurrentUserId!);

        var currentInstallation = organizationsForUser.Single(
            userInstallation => userInstallation.InstallationId == clientResponse.InstallationId);

        var modules = await ModulesGetter.GetModulesAsync(
            command.Organization,
            command.ModuleName,
            command.CommitHash,
            clientId: clientResponse.InstallationId,
            clientSecret: clientResponse.InstallationToken,
            cancellationToken: cancellationToken);

        return new AddModuleResponse(modules.WithIsOwner(currentInstallation.Name));
    }
}
