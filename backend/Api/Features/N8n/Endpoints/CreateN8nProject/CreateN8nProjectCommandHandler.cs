using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Api.Features.N8n.Events;
using Auth.Services;
using Cli.Abstractions;
using Core.MediatR;
using Database.Extensions;

namespace Api.Features.N8n.Endpoints.CreateN8nProject;

public sealed record CreateN8nProjectCommandHandler(
    IProjectRepository ProjectRepository,
    ICurrentUserAccessor CurrentUserAccessor,
    ILocationRepository LocationRepository) : ICommandHandler<CreateN8nProjectCommand, CreateN8nProjectResponse>
{
    private const string N8nTemplateRepoUrl = "https://github.com/compozerr/n8n-template";

    public async Task<CreateN8nProjectResponse> Handle(CreateN8nProjectCommand command, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserAccessor.CurrentUserId!;

        var location = await LocationRepository.GetLocationByIso(command.LocationIso);

        var newProject = new Project
        {
            Name = command.ProjectName,
            RepoUri = new Uri(N8nTemplateRepoUrl),
            UserId = userId,
            LocationId = location.Id,
            ServerTierId = ServerTiers.GetById(new ServerTierId(command.Tier)).Id,
            State = ProjectState.Stopped,
            Type = ProjectType.N8n
        };

        newProject.QueueDomainEvent<ProjectCreatedEvent>();
        newProject.QueueDomainEvent<N8nProjectCreatedEvent>();

        var project = await ProjectRepository.AddAsync(newProject, cancellationToken);

        return new CreateN8nProjectResponse(project.Id);
    }
}
