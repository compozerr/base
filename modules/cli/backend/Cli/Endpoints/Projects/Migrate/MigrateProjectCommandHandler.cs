using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Auth.Services;
using Cli.Abstractions;
using Core.MediatR;
using Database.Extensions;
using MediatR;

namespace Cli.Endpoints.Projects.Migrate;

public sealed class MigrateProjectCommandHandler(
    IProjectRepository projectRepository,
    ICurrentUserAccessor currentUserAccessor,
    ILocationRepository locationRepository,
    IMediator mediator) : ICommandHandler<MigrateProjectCommand, MigrateProjectResponse>
{
    public async Task<MigrateProjectResponse> Handle(
        MigrateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserAccessor.CurrentUserId!;
        var location = await locationRepository.GetLocationByIso(command.LocationIso);

        var newProject = new Project
        {
            Name = command.Name,
            RepoUri = new Uri(command.RepoUrl),
            UserId = userId,
            LocationId = location.Id,
            ServerTierId = ServerTiers.GetById(new ServerTierId(command.Tier)).Id,
            State = ProjectState.Stopped,
            Type = ProjectType.Compozerr
        };

        newProject.QueueDomainEvent<ProjectCreatedEvent>();

        var project = await projectRepository.AddAsync(newProject, cancellationToken);

        var config = new CompozerrConfig(
            Type: "project",
            Name: command.Name,
            Id: project.Id.Value,
            Start: command.StartCommand,
            DockerComposeFile: command.DockerComposeFile);

        await mediator.Send(
            new DeployProjectCommand(
                project.Id,
                CommitHash: "migration",
                CommitMessage: "Initial migration to compozerr",
                CommitAuthor: "compozerr-cli",
                CommitBranch: "main",
                CommitEmail: "",
                OverrideAuthorization: true),
            cancellationToken);

        return new MigrateProjectResponse(project.Id, config);
    }
}
