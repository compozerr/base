using Auth.Abstractions;
using Core.Abstractions;
using Github.Data;
using Github.Models;
using Github.Services;
using Octokit;
using Serilog;

namespace Github.Features;

public sealed class CreateDefaultSettings_UserCreatedEventHandler(
    GithubDbContext dbContext,
    IGithubService githubService) : IDomainEventHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent notification, CancellationToken cancellationToken)
    {
        var userClient = await githubService.GetUserClient(notification.UserId);

        if (userClient is null)
        {
            Log.ForContext(nameof(notification), notification, true)
               .Error("Failed to create default settings for user {UserId}", notification.UserId);
            return;
        }

        var organizations = await userClient.Organization.GetAllForCurrent();

        var organization = organizations.FirstOrDefault();

        if (organization is null)
        {
            Log.ForContext(nameof(notification), notification, true)
               .Error("Failed to create default settings for user {UserId}", notification.UserId);
            return;
        }

        var settings = new GithubUserSettings
        {
            UserId = notification.UserId,
            SelectedOrganization = organization.Id.ToString(),
        };

        await dbContext.AddAsync(settings, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}