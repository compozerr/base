using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;
using Github.Services;
using Github.Utils;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Deployments.DeployFromLatestCommit;

public sealed class DeployFromLatestCommitCommandHandler(
	IGithubService githubService,
	IProjectRepository projectRepository,
	IProjectEnvironmentRepository environmentRepository,
	ISender sender) : ICommandHandler<DeployFromLatestCommitCommand, DeployFromLatestCommitResponse>
{
	public async Task<DeployFromLatestCommitResponse> Handle(DeployFromLatestCommitCommand command, CancellationToken cancellationToken = default)
	{
		var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken) ??
			throw new InvalidOperationException($"Project with ID {command.ProjectId} not found.");

		// Resolve environment and branch
		ProjectEnvironmentId? environmentId = command.EnvironmentId;
		string? branch = null;

		if (environmentId is not null)
		{
			var environment = await environmentRepository.GetByIdAsync(environmentId, cancellationToken);
			branch = environment?.Branches.FirstOrDefault();
		}
		else
		{
			// No environment specified — default to production environment
			var environments = await environmentRepository.GetByProjectIdWithLatestDeploymentAsync(
				command.ProjectId, cancellationToken);
			var productionEnv = environments
				.FirstOrDefault(e => e.Type == EnvironmentType.Production);
			branch = productionEnv?.Branches.FirstOrDefault();
			environmentId = productionEnv?.Id;
		}

		var latestCommit = branch is not null
			? await GetLatestCommitFromBranchAsync(project, branch)
			: await githubService.GetLatestCommitAsync(project.RepoUri, project.UserId, cancellationToken)
			  ?? throw new InvalidOperationException("No commits found in the repository.");

		var deployProjectCommand = new DeployProjectCommand(
				project.Id,
				latestCommit.Sha,
				latestCommit.Commit.Message,
				latestCommit.Commit.Author.Name,
				branch ?? "main",
				latestCommit.Commit.Author.Email,
				EnvironmentId: environmentId);

		await sender.Send(
			deployProjectCommand,
			cancellationToken);

		return new();
	}

	private async Task<Octokit.GitHubCommit> GetLatestCommitFromBranchAsync(
		Data.Project project, string branch)
	{
		var (owner, repo) = GitHubRepoUrl.Parse(project.RepoUri);
		var clientResponse = await githubService.GetInstallationClientForRepoAsync(project.RepoUri);

		var commits = await clientResponse.InstallationClient.Repository.Commit.GetAll(
			owner, repo, new Octokit.CommitRequest { Sha = branch },
			new Octokit.ApiOptions { PageCount = 1, PageSize = 1 });

		return commits.Count > 0
			? commits[0]
			: throw new InvalidOperationException($"No commits found on branch '{branch}'.");
	}
}
