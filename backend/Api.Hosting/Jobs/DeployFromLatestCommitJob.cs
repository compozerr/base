using Api.Abstractions;
using Api.Data.Repositories;
using Github.Services;
using Jobs;
using MediatR;

namespace Api.Hosting.Jobs;

public class DeployFromLatestCommitJob(
    IGithubService githubService,
    IProjectRepository projectRepository,
    ISender sender) : JobBase<DeployFromLatestCommitJob, ProjectId>
{
    public override async Task ExecuteAsync(ProjectId projectId)
    {
        var project = await projectRepository.GetByIdAsync(projectId) ??
            throw new InvalidOperationException($"Project with ID {projectId} not found.");

        var latestCommit = await githubService.GetLatestCommitAsync(
            project.RepoUri,
            project.UserId) ?? throw new InvalidOperationException("No commits found in the repository.");

        var deployProjectCommand = new DeployProjectCommand(
            project.Id,
            latestCommit.Sha,
            latestCommit.Commit.Message,
            latestCommit.Commit.Author.Name,
            latestCommit.Commit.Committer.Name,
            latestCommit.Commit.Author.Email);

        await sender.Send(deployProjectCommand);
    }
}
