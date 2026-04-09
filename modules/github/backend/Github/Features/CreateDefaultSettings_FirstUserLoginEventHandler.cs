using Auth.Abstractions;
using Core.Abstractions;
using Github.Data;
using Github.Models;
using Github.Services;
using Organizations.Abstractions;
using Organizations.Data.Repositories;

namespace Github.Features;

public sealed class CreateDefaultSettings_UserCreatedEventHandler(
    GithubDbContext dbContext,
    IGithubService githubService,
    IOrganizationRepository organizationRepository) : EntityDomainEventHandlerBase<FirstUserLoginEvent>
{
    protected override async Task HandleBeforeSaveAsync(FirstUserLoginEvent domainEvent, CancellationToken cancellationToken)
    {
        string? selectedInstallationId = null;

        var userInstallations = await githubService.GetInstallationsForUserByAccessTokenAsync(
            domainEvent.AccessToken);

        if (userInstallations.Count > 0)
            selectedInstallationId = userInstallations[0].InstallationId;

        var personalOrg = await organizationRepository.GetPersonalOrgForUserAsync(domainEvent.UserId, cancellationToken);
        if (personalOrg is null)
        {
            // Personal org should always exist by this point (created in UserCreatedEvent handler)
            return;
        }

        var settings = new GithubOrganizationSettings
        {
            OrganizationId = personalOrg.Id,
            SelectedProjectsInstallationId = selectedInstallationId,
            SelectedModulesInstallationId = selectedInstallationId,
        };

        // Add to context - it will be saved by the outer SaveChangesAsync
        await dbContext.AddAsync(settings, cancellationToken);
    }
}
