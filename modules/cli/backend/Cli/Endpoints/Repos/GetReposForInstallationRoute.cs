using Auth.Services;
using Github.Endpoints.SetDefaultInstallationId;
using Github.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Repos;

public static class GetReposForInstallationRoute
{
    public const string Route = "get-repos-for-installation";

    public static RouteHandlerBuilder AddGetReposForInstallationRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<IReadOnlyList<RepositoryDto>> ExecuteAsync(
        DefaultInstallationIdSelectionType type,
        ICurrentUserAccessor currentUserAccessor,
        IGithubService githubService)
        => githubService.GetRepositoriesByUserDefaultId(currentUserAccessor.CurrentUserId!, type);
}
