using Auth.Abstractions;
using Core.Abstractions;
using Github.Data;
using Github.Models;
using Github.Services;

namespace Github.Features;

public sealed class CreateDefaultSettings_UserCreatedEventHandler(
    GithubDbContext dbContext,
    IGithubService githubService) : EntityDomainEventHandlerBase<FirstUserLoginEvent>
{
    protected override async Task HandleBeforeSaveAsync(FirstUserLoginEvent domainEvent, CancellationToken cancellationToken)
    {
        string? selectedInstallationId = null;

        var userInstallations = await githubService.GetInstallationsForUserByAccessTokenAsync(
            domainEvent.AccessToken);

        if (userInstallations.Count > 0)
            selectedInstallationId = userInstallations[0].InstallationId;

        var settings = new GithubUserSettings
        {
            UserId = domainEvent.UserId,
            SelectedProjectsInstallationId = selectedInstallationId,
            SelectedModulesInstallationId = selectedInstallationId
        };

        await dbContext.AddAsync(settings, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}