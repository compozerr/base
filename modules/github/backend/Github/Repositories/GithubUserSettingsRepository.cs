using Auth.Abstractions;
using Database.Repositories;
using Github.Abstractions;
using Github.Data;
using Github.Endpoints.SetDefaultInstallationId;
using Github.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Github.Repositories;

public interface IGithubUserSettingsRepository
{
    Task<GithubUserSettings?> GetOrDefaultByUserIdAsync(UserId userId);
    Task<string> SetSelectedOrganizationForUserAsync(
        UserId userId,
        string selectedInstallationId,
        DefaultInstallationIdSelectionType defaultInstallationIdSelectionType);
}

public sealed class GithubUserSettingsRepository(
    GithubDbContext context) : GenericRepository<GithubUserSettings, GithubUserSettingsId, GithubDbContext>(context), IGithubUserSettingsRepository
{
    private readonly GithubDbContext _context = context;

    public Task<GithubUserSettings?> GetOrDefaultByUserIdAsync(UserId userId)
        => _context.GithubUserSettings.Where(g => g.UserId == userId)
                                      .FirstOrDefaultAsync();

    public async Task<string> SetSelectedOrganizationForUserAsync(
        UserId userId,
        string selectedInstallationId,
        DefaultInstallationIdSelectionType defaultInstallationIdSelectionType)
    {
        var settings = await GetOrDefaultByUserIdAsync(userId);

        if (settings is null)
        {
            Log.ForContext(nameof(userId), userId)
               .Error("No GithubUserSettings was created for user, so cannot set organization");

            throw new ArgumentNullException();
        }

        switch (defaultInstallationIdSelectionType)
        {
            case DefaultInstallationIdSelectionType.Projects:
                settings.SelectedProjectsInstallationId = selectedInstallationId;
                break;
            case DefaultInstallationIdSelectionType.Modules:
                settings.SelectedModulesInstallationId = selectedInstallationId;
                break;
            default:
                Log.ForContext(nameof(userId), userId)
                   .Error("No DefaultInstallationIdSelectionType was provided");

                throw new ArgumentNullException(nameof(defaultInstallationIdSelectionType));
        }

        await _context.SaveChangesAsync();

        return selectedInstallationId;
    }
}
