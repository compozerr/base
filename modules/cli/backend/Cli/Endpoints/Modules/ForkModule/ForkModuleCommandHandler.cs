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

		var newOrg = currentInstallation.Name;

		foreach (var module in command.ModulesToFork)
		{
			if (!(await ExistsAsync(
					module,
					clientResponse.InstallationClient)).RepoExists)
			{
				throw new InvalidOperationException($"Repo {module.Value} does not exist");
			}
		}

		await command.ModulesToFork.ApplyAsync(
			m => UpsertForkAsync(
				m,
				clientResponse.InstallationClient,
				newOrg,
				project.Name));

		return new ForkModuleResponse(
			ForkedModules: [.. command.ModulesToFork.Select(m => m with { Value = ModuleDto.CreateValue(newOrg, m.ModuleName) })],
			SharedBranchName: project.Name);
	}

	private async Task UpsertForkAsync(ModuleDto module, IGitHubClient client, string newOrg, string projectName)
	{
		var branchName = projectName;

		var existsResult = await ExistsAsync(
			new ModuleDto(
				ModuleDto.CreateValue(
					newOrg,
					module.ModuleName),
				module.Hash),
			client,
			branchName);

		// Only fork if the module is not already in the new organization
		if (!existsResult.RepoExists)
		{
			var (_, waitUntilExistsAsync) = await GithubService.ForkRepositoryAsync(
				client,
				module.Organization,
				module.ModuleName,
				newOrg,
				module.ModuleName);

			// Wait for the repository to be available after forking
			await waitUntilExistsAsync;
		}

		if (!existsResult.BranchExists)
		{
			await GithubService.CreateBranchAsync(
				client,
				newOrg,
				module.ModuleName,
				projectName);
		}
	}

	private sealed record ExistsResponse(bool RepoExists, bool BranchExists);

	private async static Task<ExistsResponse> ExistsAsync(
		ModuleDto module,
		IGitHubClient client,
		string? branchName = null)
	{
		try
		{
			bool branchExists = false;

			var repo = await client.Repository.Get(
				module.Organization,
				module.ModuleName);

			if (branchName is not null)
			{
				var branches = await client.Repository.Branch.GetAll(
					module.Organization,
					module.ModuleName);

				branchExists = branches.Any(b => b.Name.Equals(branchName, StringComparison.OrdinalIgnoreCase));
			}

			return new ExistsResponse(true, branchExists);
		}
		catch (ApiException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
		{
			return new(false, false);
		}
		catch (Exception e)
		{
			throw new InvalidOperationException($"Error checking repo existence: {e.Message}", e);
		}
	}
}
