using Auth.Services;
using Github.Repositories;
using Github.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace Github.Endpoints;

public sealed record InstallationDto(string Id, string Name);

public sealed record GetInstalledOrganizationsResponse(
    string SelectedInstallationId,
    List<InstallationDto> Installations
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

        var userClient = await githubService.GetUserClient(userId);
        ArgumentNullException.ThrowIfNull(userClient);

        var installations = await userClient.Organization.GetAllForCurrent();

        var githubUserSettings = await githubUserSettingsRepository.GetOrDefaultByUserIdAsync(userId);
        ArgumentNullException.ThrowIfNull(githubUserSettings);

        var selectedOrganizationId = githubUserSettings.SelectedOrganizationId;

        if (installations.Count == 0)
        {
            Log.ForContext(nameof(userId), userId)
               .Information("No installations found for user");

            return new GetInstalledOrganizationsResponse(selectedOrganizationId, []);
        }

        var hasSelectedOrganizationInInstallationsList = installations.Select(i => i.Id.ToString())
                                                                      .Contains(githubUserSettings.SelectedOrganizationId);
                                                                      
        var hasSelectedOrganization = !string.IsNullOrEmpty(selectedOrganizationId);

        if (!hasSelectedOrganizationInInstallationsList || !hasSelectedOrganization)
        {
            selectedOrganizationId = await githubUserSettingsRepository.SetSelectedOrganizationForUserAsync(
                userId,
                installations[0].Id.ToString());
        }

        return new GetInstalledOrganizationsResponse(
            selectedOrganizationId,
            installations.Select(i => new InstallationDto(i.Id.ToString(), i.Name))
                         .ToList()
        );
    }
}