using Database.Repositories;
using Github.Abstractions;
using Github.Data;
using Github.Endpoints.SetDefaultInstallationId;
using Github.Models;
using Microsoft.EntityFrameworkCore;
using Organizations.Abstractions;

namespace Github.Repositories;

public interface IGithubOrganizationSettingsRepository
{
    Task<GithubOrganizationSettings?> GetOrDefaultByOrganizationIdAsync(OrganizationId organizationId);
    Task<string> SetSelectedOrganizationForOrgAsync(
        OrganizationId organizationId,
        string selectedInstallationId,
        DefaultInstallationIdSelectionType defaultInstallationIdSelectionType);
}

public sealed class GithubOrganizationSettingsRepository(
    GithubDbContext context) : GenericRepository<GithubOrganizationSettings, GithubOrganizationSettingsId, GithubDbContext>(context), IGithubOrganizationSettingsRepository
{
    public Task<GithubOrganizationSettings?> GetOrDefaultByOrganizationIdAsync(OrganizationId organizationId)
        => Query().Where(g => g.OrganizationId == organizationId)
                  .FirstOrDefaultAsync();

    public async Task<string> SetSelectedOrganizationForOrgAsync(
        OrganizationId organizationId,
        string selectedInstallationId,
        DefaultInstallationIdSelectionType defaultInstallationIdSelectionType)
    {
        var settings = await GetOrDefaultByOrganizationIdAsync(organizationId);
        if (settings is null)
        {
            settings = new GithubOrganizationSettings
            {
                OrganizationId = organizationId,
                SelectedProjectsInstallationId = defaultInstallationIdSelectionType == DefaultInstallationIdSelectionType.Projects
                    ? selectedInstallationId : null,
                SelectedModulesInstallationId = defaultInstallationIdSelectionType == DefaultInstallationIdSelectionType.Modules
                    ? selectedInstallationId : null
            };
            await AddAsync(settings);
        }
        else
        {
            if (defaultInstallationIdSelectionType == DefaultInstallationIdSelectionType.Projects)
                settings.SelectedProjectsInstallationId = selectedInstallationId;
            else
                settings.SelectedModulesInstallationId = selectedInstallationId;
            await UpdateAsync(settings);
        }
        return selectedInstallationId;
    }
}
