using Api.Abstractions;
using Api.Data.Repositories;
using Core.MediatR;
using Github.Services;
using MediatR;

namespace Api.Endpoints.Projects.Deployments.DeployFromLatestCommit;

public sealed class DeployFromLatestCommitCommandHandler(
	IGithubService githubService,
	IProjectRepository projectRepository,
	ISender sender) : ICommandHandler<DeployFromLatestCommitCommand, DeployFromLatestCommitResponse>
{
	public async Task<DeployFromLatestCommitResponse> Handle(DeployFromLatestCommitCommand command, CancellationToken cancellationToken = default)
	{
		var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken) ??
			throw new InvalidOperationException($"Project with ID {command.ProjectId} not found.");

		var latestCommit = await githubService.GetLatestCommitAsync(
			project.RepoUri,
			cancellationToken) ?? throw new InvalidOperationException("No commits found in the repository.");
			
        var deployProjectCommand = new DeployProjectCommand(
				project.Id,
				latestCommit.Sha,
				latestCommit.Commit.Message,
				latestCommit.Commit.Author.Name,
				latestCommit.Commit.Committer.Name,
				latestCommit.Commit.Author.Email);

		await sender.Send(
			deployProjectCommand,
			cancellationToken);

		return new();
	}
}
