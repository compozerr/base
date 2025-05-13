using Auth.Services;
using Cli.Endpoints.Modules.Add;
using Core.Extensions;
using Core.MediatR;
using Github.Services;
using GE = Github.Endpoints.SetDefaultInstallationId;

namespace Cli.Endpoints.Modules.ForkModule;

public sealed class ForkModuleCommandHandler(
	IGithubService GithubService,
	ICurrentUserAccessor CurrentUserAccessor) : ICommandHandler<ForkModuleCommand, ForkModuleResponse>
{
	public async Task<ForkModuleResponse> Handle(ForkModuleCommand command, CancellationToken cancellationToken = default)
	{
		var clientResponse = await GithubService.GetInstallationClientByUserDefaultAsync(
			CurrentUserAccessor.CurrentUserId ?? throw new InvalidOperationException("User not found"),
			GE.DefaultInstallationIdSelectionType.Modules);

		var organizationsForUser = await GithubService.GetInstallationsForUserAsync(CurrentUserAccessor.CurrentUserId!);
		var currentInstallation = organizationsForUser.Single(
			userInstallation => userInstallation.InstallationId == clientResponse.InstallationId);

		var installationName = currentInstallation.Name;

		if (command.ModulesToFork.Any(m => OwnsRepo(m, installationName)))
		{
			throw new InvalidOperationException("You already own one of the repos");
		}

		//TODO implement forking

		return new ForkModuleResponse();
	}

	private static bool OwnsRepo(ModuleDto module, string selectedModulesOrganizationName)
		=> module.Organization == selectedModulesOrganizationName;
}
