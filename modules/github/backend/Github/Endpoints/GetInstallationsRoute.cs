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
    string? SelectedProjectsInstallationId,
    string? SelectedModulesInstallationId,
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

        var selectedProjectsInstallationId = githubUserSettings.SelectedProjectsInstallationId;
        var selectedModulesInstallationId = githubUserSettings.SelectedModulesInstallationId;

        if (organizationsForUser.Count == 0)
        {
            Log.ForContext(nameof(userId), userId)
               .Information("No organizations found for user");

            return new GetInstallationsResponse(
                selectedProjectsInstallationId,
                selectedModulesInstallationId,
                []);
        }

        //Check for project
        var hasSelectedProjectsInstallationIdInInstallationsList = organizationsForUser.Select(i => i.InstallationId)
                                                                                      .Contains(githubUserSettings.SelectedProjectsInstallationId);

        var hasSelectedProjectsInstallationId = !string.IsNullOrEmpty(selectedProjectsInstallationId);

        if (!hasSelectedProjectsInstallationIdInInstallationsList || !hasSelectedProjectsInstallationId)
        {
            selectedProjectsInstallationId = organizationsForUser[0].InstallationId;

            await mediator.Send(
                new SetDefaultInstallationIdCommand(
                    userId,
                    selectedProjectsInstallationId,
                    DefaultInstallationIdSelectionType.Projects));
        }

        //Check for module
        var hasSelectedModulesInstallationIdInInstallationsList = organizationsForUser.Select(i => i.InstallationId)
                                                                                      .Contains(githubUserSettings.SelectedModulesInstallationId);

        var hasSelectedModulesInstallationId = !string.IsNullOrEmpty(selectedModulesInstallationId);

        if (!hasSelectedModulesInstallationIdInInstallationsList || !hasSelectedModulesInstallationId)
        {
            selectedModulesInstallationId = organizationsForUser[0].InstallationId;

            await mediator.Send(
                new SetDefaultInstallationIdCommand(
                    userId,
                    selectedModulesInstallationId,
                    DefaultInstallationIdSelectionType.Modules));
        }

        return new GetInstallationsResponse(
            selectedProjectsInstallationId,
            selectedModulesInstallationId,
            organizationsForUser.Select(i => new InstallationDto(i.InstallationId, i.Name, i.AccountType.ToString()))
                                .ToList()
        );
    }
}