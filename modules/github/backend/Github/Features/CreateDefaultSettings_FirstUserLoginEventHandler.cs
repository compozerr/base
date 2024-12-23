using Auth.Abstractions;
using Core.Abstractions;
using Github.Data;
using Github.Models;
using Github.Services;

namespace Github.Features;

public sealed class CreateDefaultSettings_UserCreatedEventHandler(
    GithubDbContext dbContext,
    IGithubService githubService) : IDomainEventHandler<FirstUserLoginEvent>
{
    public async Task Handle(FirstUserLoginEvent notification, CancellationToken cancellationToken)
    {
        string? selectedInstallationId = null;

        var userInstallations = await githubService.GetInstallationsForUserByAccessTokenAsync(notification.AccessToken);

        if (userInstallations.Count > 0)
            selectedInstallationId = userInstallations[0].InstallationId;

        var settings = new GithubUserSettings
        {
            UserId = notification.UserId,
            SelectedProjectsInstallationId = selectedInstallationId,
            SelectedModulesInstallationId = selectedInstallationId
        };

        await dbContext.AddAsync(settings, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}