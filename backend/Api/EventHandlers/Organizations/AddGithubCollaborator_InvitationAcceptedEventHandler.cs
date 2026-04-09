using Api.Data.Repositories;
using Auth.Models;
using Auth.Repositories;
using Core.Abstractions;
using Github.Services;
using Github.Utils;
using Octokit;
using Organizations.Events;
using Serilog;

namespace Api.EventHandlers.Organizations;

public sealed class AddGithubCollaborator_InvitationAcceptedEventHandler(
    IProjectRepository projectRepository,
    IGithubService githubService,
    IUserRepository userRepository) : EntityDomainEventHandlerBase<InvitationAcceptedEvent>
{
    private static readonly Serilog.ILogger Logger = Log.ForContext<AddGithubCollaborator_InvitationAcceptedEventHandler>();

    protected override async Task HandleAfterSaveAsync(InvitationAcceptedEvent domainEvent, CancellationToken cancellationToken)
    {
        // Get the new member's GitHub username
        var user = await userRepository.GetUserWithLoginsAsync(domainEvent.UserId, cancellationToken);
        if (user is null) return;

        var githubLogin = user.Logins?.FirstOrDefault(l => l.Provider == Provider.GitHub) as GithubUserLogin;
        if (githubLogin is null)
        {
            Logger.Information("User {UserId} has no GitHub login, skipping collaborator invite", domainEvent.UserId);
            return;
        }

        // Get GitHub username from their profile
        string? githubUsername = null;
        try
        {
            var userClient = githubService.GetUserClientByAccessToken(githubLogin.AccessToken);
            if (userClient is not null)
            {
                var ghUser = await userClient.User.Current();
                githubUsername = ghUser.Login;
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to get GitHub username for user {UserId}", domainEvent.UserId);
            return;
        }

        if (string.IsNullOrEmpty(githubUsername)) return;

        // Get all projects in this organization that have a GitHub repo
        var projects = await projectRepository.GetFilteredAsync(
            p => p.OrganizationId == domainEvent.OrganizationId,
            cancellationToken);

        foreach (var project in projects)
        {
            try
            {
                if (!GitHubRepoUrl.TryParse(project.RepoUri.ToString(), out var owner, out var repoName))
                    continue;

                GetInstallationClientForRepoResponse installationClient;
                try
                {
                    installationClient = await githubService.GetInstallationClientForRepoAsync(project.RepoUri);
                }
                catch (InstallationNotFoundForRepoException)
                {
                    continue;
                }

                await installationClient.InstallationClient.Repository.Collaborator.Add(
                    owner,
                    repoName,
                    githubUsername);

                Logger.Information(
                    "Added GitHub collaborator {Username} to {Owner}/{Repo} for org {OrgId}",
                    githubUsername, owner, repoName, domainEvent.OrganizationId);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex,
                    "Failed to add GitHub collaborator for project {ProjectId}",
                    project.Id);
            }
        }
    }
}
