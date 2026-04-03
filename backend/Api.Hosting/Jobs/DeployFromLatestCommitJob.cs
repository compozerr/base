using Api.Abstractions;
using Api.Data.Repositories;
using Github.Services;
using Jobs;
using MediatR;

namespace Api.Hosting.Jobs;

public sealed record DeployFromLatestCommitJobArgs(
    ProjectId ProjectId,
    bool OverrideAuthorization = false);

public class DeployFromLatestCommitJob(
    IGithubService githubService,
    IProjectRepository projectRepository,
    ISender sender) : JobBase<DeployFromLatestCommitJob, DeployFromLatestCommitJobArgs>
{
    public override async Task ExecuteAsync(DeployFromLatestCommitJobArgs args)
    {
        var project = await projectRepository.GetByIdAsync(args.ProjectId) ??
            throw new InvalidOperationException($"Project with ID {args.ProjectId} not found.");

        var latestCommit = await githubService.GetLatestCommitAsync(
            project.RepoUri,
            project.UserId) ?? throw new InvalidOperationException("No commits found in the repository.");

        var deployProjectCommand = new DeployProjectCommand(
            project.Id,
            latestCommit.Sha,
            latestCommit.Commit.Message,
            latestCommit.Commit.Author.Name,
            "main",
            latestCommit.Commit.Author.Email,
            OverrideAuthorization: args.OverrideAuthorization);

        await sender.Send(deployProjectCommand);
    }
}
