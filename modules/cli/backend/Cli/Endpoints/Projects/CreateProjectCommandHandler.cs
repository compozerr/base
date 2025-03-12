using Api.Data;
using Api.Data.Repositories;
using Auth.Services;
using Core.MediatR;
using Database.Extensions;

namespace Cli.Endpoints.Projects;

public sealed record CreateProjectCommandHandler(
    IProjectRepository ProjectRepository,
    ICurrentUserAccessor CurrentUserAccessor,
    ILocationRepository LocationRepository) : ICommandHandler<CreateProjectCommand, CreateProjectResponse>
{
    public async Task<CreateProjectResponse> Handle(CreateProjectCommand command, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserAccessor.CurrentUserId!;

        var location = await LocationRepository.GetLocationByIso(command.LocationIso);

        var newProject = new Project
        {
            Name = command.RepoName,
            RepoUri = new Uri(command.RepoUrl),
            UserId = userId,
            LocationId = location.Id
        };

        newProject.QueueDomainEvent<ProjectCreatedEvent>();

        var project = await ProjectRepository.AddAsync(newProject, cancellationToken);

        return new CreateProjectResponse(project.Id);
    }
}