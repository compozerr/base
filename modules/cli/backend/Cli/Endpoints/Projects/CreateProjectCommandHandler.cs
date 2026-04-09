using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Auth.Services;
using Cli.Abstractions;
using Core.MediatR;
using Database.Extensions;
using Organizations.Services;

namespace Cli.Endpoints.Projects;

public sealed record CreateProjectCommandHandler(
    IProjectRepository ProjectRepository,
    ICurrentUserAccessor CurrentUserAccessor,
    ILocationRepository LocationRepository,
    IOrganizationContextAccessor OrganizationContextAccessor) : ICommandHandler<CreateProjectCommand, CreateProjectResponse>
{
    public async Task<CreateProjectResponse> Handle(CreateProjectCommand command, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserAccessor.CurrentUserId!;
        var organizationId = await OrganizationContextAccessor.GetCurrentOrganizationIdAsync();

        var location = await LocationRepository.GetLocationByIso(command.LocationIso);

        // Note: GithubInstallationId is intentionally NOT persisted. The deploy/
        // clone paths resolve the installation dynamically from the repo URL via
        // GetInstallationClientForRepoAsync, so a stamped id would only go stale.
        var newProject = new Project
        {
            Name = command.RepoName,
            RepoUri = new Uri(command.RepoUrl),
            UserId = userId,
            LocationId = location.Id,
            ServerTierId = ServerTiers.GetById(new ServerTierId(command.Tier)).Id,
            State = ProjectState.Stopped,
            Type = ProjectType.Compozerr,
            OrganizationId = organizationId
        };

        newProject.QueueDomainEvent<ProjectCreatedEvent>();

        var project = await ProjectRepository.AddAsync(newProject, cancellationToken);

        return new CreateProjectResponse(project.Id);
    }
}