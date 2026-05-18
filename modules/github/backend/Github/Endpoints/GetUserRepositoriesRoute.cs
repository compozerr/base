using Auth.Services;
using Github.Services;
using Microsoft.AspNetCore.Builder;

namespace Github.Endpoints;

public static class GetUserRepositoriesRoute
{
    public const string Route = "user-repositories";

    public static RouteHandlerBuilder AddGetUserRepositoriesRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<IReadOnlyList<UserRepositoryDto>> ExecuteAsync(
        ICurrentUserAccessor currentUserAccessor,
        IGithubService githubService)
    {
        var userId = currentUserAccessor.CurrentUserId!;
        return await githubService.GetAllRepositoriesForUserAsync(userId);
    }
}
