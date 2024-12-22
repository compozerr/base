using Auth.Abstractions;
using Core.Abstractions;
using Github.Data;
using Github.Models;
using Github.Services;
using Serilog;

namespace Github.Features;

public sealed class CreateDefaultSettings_UserCreatedEventHandler(
    GithubDbContext dbContext,
    IGithubService githubService) : IDomainEventHandler<FirstUserLoginEvent>
{
    public async Task Handle(FirstUserLoginEvent notification, CancellationToken cancellationToken)
    {
        string? selectedOrganizationId = null;

        var userInstallations = await githubService.GetInstallationsForUserByAccessTokenAsync(notification.AccessToken);

        if (userInstallations.Count > 0)
            selectedOrganizationId = userInstallations[0].OrganizationId;

        var settings = new GithubUserSettings
        {
            UserId = notification.UserId,
            SelectedOrganizationId = selectedOrganizationId
        };

        await dbContext.AddAsync(settings, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}