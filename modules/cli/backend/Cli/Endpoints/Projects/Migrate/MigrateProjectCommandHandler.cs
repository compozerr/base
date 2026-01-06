using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Auth.Services;
using Cli.Abstractions;
using Core.MediatR;
using Core.Services;
using Database.Extensions;
using Github.Services;
using Github.Utils;
using MediatR;

namespace Cli.Endpoints.Projects.Migrate;

public sealed class MigrateProjectCommandHandler(
    IProjectRepository projectRepository,
    ICurrentUserAccessor currentUserAccessor,
    ILocationRepository locationRepository,
    IGithubService githubService,
    IFrontendLocation frontendLocation,
    IMediator mediator) : ICommandHandler<MigrateProjectCommand, MigrateProjectResponse>
{
    public async Task<MigrateProjectResponse> Handle(
        MigrateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserAccessor.CurrentUserId!;

        var hasAccess = await CheckRepositoryAccessAsync(command.RepoUrl, userId);
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

        return MigrateProjectResponse.Ok(project.Id, config);
    }

    private async Task<bool> CheckRepositoryAccessAsync(string repoUrl, Auth.Abstractions.UserId userId)
    {
        if (!GitHubRepoUrl.TryParse(repoUrl, out var owner, out var repoName))
            return false;

        var installations = await githubService.GetInstallationsForUserAsync(userId);

        foreach (var installation in installations)
        {
            try
            {
                var clientResponse = await githubService.GetInstallationClientByInstallationIdAsync(
                    installation.InstallationId);

                if (clientResponse is null)
                    continue;

                var repos = await clientResponse.InstallationClient
                    .GitHubApps.Installation.GetAllRepositoriesForCurrent();

                var hasRepo = repos.Repositories.Any(r =>
                    r.Owner.Login.Equals(owner, StringComparison.OrdinalIgnoreCase) &&
                    r.Name.Equals(repoName, StringComparison.OrdinalIgnoreCase));

                if (hasRepo)
                    return true;
            }
            catch
            {
                continue;
            }
        }

        return false;
    }
}
