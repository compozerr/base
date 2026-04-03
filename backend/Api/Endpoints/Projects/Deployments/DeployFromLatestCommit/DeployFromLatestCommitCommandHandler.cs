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

		// If an environment is specified, get latest commit from that environment's branch
		string? branch = null;
		if (command.EnvironmentId is not null)
		{
			var environment = await environmentRepository.GetByIdAsync(command.EnvironmentId, cancellationToken);
			branch = environment?.Branches.FirstOrDefault();
		}

		var latestCommit = branch is not null && project.GithubInstallationId is { } installationId
			? await GetLatestCommitFromBranchAsync(project, branch, installationId)
			: await githubService.GetLatestCommitAsync(project.RepoUri, project.UserId, cancellationToken)
			  ?? throw new InvalidOperationException("No commits found in the repository.");

		var deployProjectCommand = new DeployProjectCommand(
				project.Id,
				latestCommit.Sha,
				latestCommit.Commit.Message,
				latestCommit.Commit.Author.Name,
				latestCommit.Commit.Committer.Name,
				latestCommit.Commit.Author.Email,
				EnvironmentId: command.EnvironmentId);

		await sender.Send(
			deployProjectCommand,
			cancellationToken);

		return new();
	}

	private async Task<Octokit.GitHubCommit> GetLatestCommitFromBranchAsync(
		Data.Project project, string branch, long installationId)
	{
		var (owner, repo) = GitHubRepoUrl.Parse(project.RepoUri);
		var clientResponse = await githubService.GetInstallationClientByInstallationIdAsync(
			installationId.ToString())
			?? throw new InvalidOperationException("Failed to get GitHub installation client.");

		var commits = await clientResponse.InstallationClient.Repository.Commit.GetAll(
			owner, repo, new Octokit.CommitRequest { Sha = branch },
			new Octokit.ApiOptions { PageCount = 1, PageSize = 1 });

		return commits.Count > 0
			? commits[0]
			: throw new InvalidOperationException($"No commits found on branch '{branch}'.");
	}
}
