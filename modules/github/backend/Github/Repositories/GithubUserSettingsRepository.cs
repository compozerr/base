using Auth.Abstractions;
using Database.Repositories;
using Github.Abstractions;
using Github.Data;
using Github.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Github.Repositories;

public interface IGithubUserSettingsRepository
{
    Task<GithubUserSettings?> GetOrDefaultByUserIdAsync(UserId userId);
    Task<string> SetSelectedOrganizationForUserAsync(
        UserId userId,
        string selectedOrganizationId);
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
        string selectedOrganizationId)
    {
        var settings = await GetOrDefaultByUserIdAsync(userId);

        if (settings is null)
        {
            Log.ForContext(nameof(userId), userId)
               .Error("No GithubUserSettings was created for user, so cannot set organization");

            throw new ArgumentNullException();
        }

        settings.SelectedInstallationId = selectedOrganizationId;

        await _context.SaveChangesAsync();

        return selectedOrganizationId;
    }
}
