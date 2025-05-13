using Api.Data.Repositories;
using Auth.Services;
using Cli.Endpoints.Modules.Add;
using Core.Extensions;
using Core.MediatR;
using Github.Services;
using Octokit;
using GE = Github.Endpoints.SetDefaultInstallationId;

namespace Cli.Endpoints.Modules.ForkModule;

public sealed class ForkModuleCommandHandler(
	IGithubService GithubService,
	ICurrentUserAccessor CurrentUserAccessor,
	IProjectRepository ProjectRepository) : ICommandHandler<ForkModuleCommand, ForkModuleResponse>
{
	public async Task<ForkModuleResponse> Handle(ForkModuleCommand command, CancellationToken cancellationToken = default)
	{
		var clientResponse = await GithubService.GetInstallationClientByUserDefaultAsync(
			CurrentUserAccessor.CurrentUserId ?? throw new InvalidOperationException("User not found"),
			GE.DefaultInstallationIdSelectionType.Modules);

		var organizationsForUser = await GithubService.GetInstallationsForUserAsync(CurrentUserAccessor.CurrentUserId!);
		var currentInstallation = organizationsForUser.Single(
			userInstallation => userInstallation.InstallationId == clientResponse.InstallationId);

		var project = (await ProjectRepository.GetByIdAsync(
			command.ProjectId,
			cancellationToken))!;

		var installationName = currentInstallation.Name;

		if (command.ModulesToFork.Any(m => OwnsRepo(m, installationName)))
		{
			throw new InvalidOperationException("You already own one of the repos");
		}

		foreach (var module in command.ModulesToFork)
		{
			if (!await ExistsAsync(module, clientResponse.InstallationClient))
			{
				throw new InvalidOperationException($"Repo {module.Name} does not exist");
			}
		}

		await command.ModulesToFork.ApplyAsync(
		    m => ForkAsync(
		        m,
		        clientResponse.InstallationClient,
		        installationName,
		        project.Name));

		return new ForkModuleResponse();
	}

	private async Task ForkAsync(ModuleDto module, IGitHubClient client, string newOrg, string projectName)
	{
		await GithubService.ForkRepositoryAsync(
			client,
			module.Organization,
			module.Name,
			newOrg,
			module.Name);

		await GithubService.CreateBranchAsync(
			client,
			newOrg,
			module.Name,
			projectName);
	}

	private async static Task<bool> ExistsAsync(
		ModuleDto module,
		IGitHubClient client)
	{
		try
		{
			await client.Repository.Get(module.Organization, module.Name);
			return true;
		}
		catch (ApiException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
		{
			return false;
		}
		catch (Exception e)
		{
			throw new InvalidOperationException($"Error checking repo existence: {e.Message}", e);
		}
	}

	private static bool OwnsRepo(ModuleDto module, string selectedModulesOrganizationName)
		=> module.Organization == selectedModulesOrganizationName;
}
