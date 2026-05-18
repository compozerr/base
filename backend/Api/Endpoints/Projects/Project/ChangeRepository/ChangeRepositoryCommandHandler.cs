using Api.Data.Repositories;
using Auth.Services;
using Core.MediatR;
using Github.Services;

namespace Api.Endpoints.Projects.Project.ChangeRepository;

public sealed class ChangeRepositoryCommandHandler(
    IProjectRepository projectRepository,
    IGithubService githubService,
    ICurrentUserAccessor currentUserAccessor) : ICommandHandler<ChangeRepositoryCommand, ChangeRepositoryResponse>
{
    public async Task<ChangeRepositoryResponse> Handle(ChangeRepositoryCommand command, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var userId = currentUserAccessor.CurrentUserId!;
        var newRepoUrl = $"https://github.com/{command.RepoFullName}";

        var hasAccess = await githubService.HasAccessToRepositoryAsync(newRepoUrl, userId);
        if (!hasAccess)
        {
            throw new InvalidOperationException(
                $"You don't have access to {command.RepoFullName} via the Compozerr GitHub App. " +
                "Install the app on the new repo's owner and try again.");
        }

        project.RepoUri = new Uri(newRepoUrl);
        await projectRepository.UpdateAsync(project, cancellationToken);

        return new ChangeRepositoryResponse(command.RepoFullName);
    }
}
