using Auth.Services;
using Github.Endpoints.SetDefaultInstallationId;
using Github.Repositories;
using Github.Services;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Serilog;

namespace Github.Endpoints;

public sealed record InstallationDto(string InstallationId, string Name, string? Type);

public sealed record GetInstallationsResponse(
    string? SelectedInstallationId,
    List<InstallationDto> Installations
);

public static class GetInstallationsRoute
{
    public const string Route = "get-installed-organizations";

    public static RouteHandlerBuilder AddGetInstallatonsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<GetInstallationsResponse> ExecuteAsync(
        ICurrentUserAccessor currentUserAccessor,
        IGithubService githubService,
        IGithubUserSettingsRepository githubUserSettingsRepository,
        IMediator mediator)
    {
        var userId = currentUserAccessor.CurrentUserId!;

        var organizationsForUser = await githubService.GetInstallationsForUserAsync(userId);

        var githubUserSettings = await githubUserSettingsRepository.GetOrDefaultByUserIdAsync(userId);
        ArgumentNullException.ThrowIfNull(githubUserSettings);

        var selectedOrganizationId = githubUserSettings.SelectedInstallationId;

        if (organizationsForUser.Count == 0)
        {
            Log.ForContext(nameof(userId), userId)
               .Information("No organizations found for user");

            return new GetInstallationsResponse(selectedOrganizationId, []);
        }

        var hasSelectedOrganizationInInstallationsList = organizationsForUser.Select(i => i.InstallationId)
                                                                             .Contains(githubUserSettings.SelectedInstallationId);

        var hasSelectedOrganization = !string.IsNullOrEmpty(selectedOrganizationId);

        if (!hasSelectedOrganizationInInstallationsList || !hasSelectedOrganization)
        {
            selectedOrganizationId = organizationsForUser[0].InstallationId;

            await mediator.Send(new SetDefaultInstallationIdCommand(userId, selectedOrganizationId));
        }

        return new GetInstallationsResponse(
            selectedOrganizationId,
            organizationsForUser.Select(i => new InstallationDto(i.InstallationId, i.Name, i.AccountType.ToString()))
                         .ToList()
        );
    }
}