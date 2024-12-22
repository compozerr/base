using Auth.Services;
using Github.Repositories;
using Github.Services;
using Microsoft.AspNetCore.Builder;
using Serilog;

namespace Github.Endpoints;

public sealed record OrganizationDto(string OrganizationId, string Name);

public sealed record GetInstalledOrganizationsResponse(
    string? SelectedInstallationId,
    List<OrganizationDto> Installations
);

public static class GetInstalledOrganizationsRoute
{
    public const string Route = "get-installed-organizations";

    public static RouteHandlerBuilder AddGetInstalledOrganizationsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<GetInstalledOrganizationsResponse> ExecuteAsync(
        ICurrentUserAccessor currentUserAccessor,
        IGithubService githubService,
        IGithubUserSettingsRepository githubUserSettingsRepository)
    {
        var userId = currentUserAccessor.CurrentUserId!;

        var organizationsForUser = await githubService.GetInstallationsForUserAsync(userId);

        var githubUserSettings = await githubUserSettingsRepository.GetOrDefaultByUserIdAsync(userId);
        ArgumentNullException.ThrowIfNull(githubUserSettings);

        var selectedOrganizationId = githubUserSettings.SelectedOrganizationId;

        if (organizationsForUser.Count == 0)
        {
            Log.ForContext(nameof(userId), userId)
               .Information("No organizations found for user");

            return new GetInstalledOrganizationsResponse(selectedOrganizationId, []);
        }

        var hasSelectedOrganizationInInstallationsList = organizationsForUser.Select(i => i.OrganizationId)
                                                                             .Contains(githubUserSettings.SelectedOrganizationId);

        var hasSelectedOrganization = !string.IsNullOrEmpty(selectedOrganizationId);

        if (!hasSelectedOrganizationInInstallationsList || !hasSelectedOrganization)
        {
            selectedOrganizationId = await githubUserSettingsRepository.SetSelectedOrganizationForUserAsync(
                userId,
                organizationsForUser[0].OrganizationId);
        }

        return new GetInstalledOrganizationsResponse(
            selectedOrganizationId,
            organizationsForUser.Select(i => new OrganizationDto(i.OrganizationId, i.Name))
                         .ToList()
        );
    }
}