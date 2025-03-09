using Api.Data;
using Api.Data.Repositories;
using Auth.Services;
using Core.MediatR;

namespace Cli.Endpoints.Projects;

public sealed record CreateProjectCommandHandler(
    IProjectRepository ProjectRepository,
    ICurrentUserAccessor CurrentUserAccessor) : ICommandHandler<CreateProjectCommand, CreateProjectResponse>
{
    public async Task<CreateProjectResponse> Handle(CreateProjectCommand command, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserAccessor.CurrentUserId!;

        var project = await ProjectRepository.AddAsync(new Api.Data.Project
        {
            Name = command.RepoName,
            RepoUri = new Uri(command.RepoUrl),
            UserId = userId,
        }, cancellationToken);

        return new CreateProjectResponse(project.Id);
    }
}