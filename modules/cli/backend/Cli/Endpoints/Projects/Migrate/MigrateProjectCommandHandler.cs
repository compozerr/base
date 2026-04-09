using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Auth.Services;
using Cli.Abstractions;
using Core.MediatR;
using Core.Services;
using Database.Extensions;
using Github.Services;
using MediatR;
using Organizations.Services;

namespace Cli.Endpoints.Projects.Migrate;

public sealed class MigrateProjectCommandHandler(
    IProjectRepository projectRepository,
    ICurrentUserAccessor currentUserAccessor,
    ILocationRepository locationRepository,
    IGithubService githubService,
    IFrontendLocation frontendLocation,
    IMediator mediator,
    IOrganizationContextAccessor organizationContextAccessor) : ICommandHandler<MigrateProjectCommand, MigrateProjectResponse>
{
    public async Task<MigrateProjectResponse> Handle(
        MigrateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserAccessor.CurrentUserId!;
        var organizationId = await organizationContextAccessor.GetCurrentOrganizationIdAsync();

        var hasAccess = await githubService.HasAccessToRepositoryAsync(command.RepoUrl, userId);
        if (!hasAccess)
        {
            var settingsUrl = frontendLocation.GetFromPath(
                "/settings?install-guide-show=true&reason=migrate");

            return MigrateProjectResponse.Fail(new MigrateProjectError(
                Code: "GITHUB_ACCESS_REQUIRED",
                Message: "Compozerr does not have access to this repository. Please install the GitHub app for this organization.",
                SettingsUrl: settingsUrl.ToString()));
        }

        var location = await locationRepository.GetLocationByIso(command.LocationIso);

        // Map CLI project type to enum
        var projectType = command.ProjectType?.ToLowerInvariant() switch
        {
            "dockerfile" => ProjectType.Dockerfile,
            "docker-compose" => ProjectType.DockerCompose,
            "compozerr-standard" => ProjectType.Compozerr,
            _ => ProjectType.Dockerfile // Default to Dockerfile for single Dockerfile projects
        };

        var repoUri = new Uri(command.RepoUrl);

        var newProject = new Project
        {
            Name = command.Name,
            RepoUri = repoUri,
            UserId = userId,
            LocationId = location.Id,
            ServerTierId = ServerTiers.GetById(new ServerTierId(command.Tier)).Id,
            State = ProjectState.Stopped,
            Type = projectType,
            OrganizationId = organizationId
        };

        newProject.QueueDomainEvent<ProjectCreatedEvent>();

        var project = await projectRepository.AddAsync(newProject, cancellationToken);

        var config = new CompozerrConfig(
            Type: "project",
            Name: command.Name,
            Id: project.Id.Value);

        // Get the latest commit from GitHub for accurate deployment info
        var latestCommit = await githubService.GetLatestCommitAsync(
            repoUri,
            userId,
            cancellationToken);

        var commitHash = latestCommit?.Sha ?? "unknown";
        var commitMessage = latestCommit?.Commit.Message ?? "Initial migration to compozerr";
        var commitAuthor = latestCommit?.Commit.Author.Name ?? "compozerr-cli";
        var commitEmail = latestCommit?.Commit.Author.Email ?? "cli@compozerr.com";

        await mediator.Send(
            new DeployProjectCommand(
                project.Id,
                CommitHash: commitHash,
                CommitMessage: commitMessage,
                CommitAuthor: commitAuthor,
                CommitBranch: "main",
                CommitEmail: commitEmail,
                OverrideAuthorization: true),
            cancellationToken);

        return MigrateProjectResponse.Ok(project.Id, config);
    }
}
