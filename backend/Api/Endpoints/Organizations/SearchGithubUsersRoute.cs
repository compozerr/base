using Auth.Services;
using Github.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints.Organizations;

public static class SearchGithubUsersRoute
{
    public static RouteHandlerBuilder AddSearchGithubUsersRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet("github-user-search", async (
            string q,
            ICurrentUserAccessor currentUserAccessor,
            IGithubService githubService) =>
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Results.Ok(new { users = Array.Empty<object>() });

            var userId = currentUserAccessor.CurrentUserId!;
            var client = await githubService.GetUserClient(userId);
            if (client is null)
                return Results.Ok(new { users = Array.Empty<object>() });

            var searchResult = await client.Search.SearchUsers(
                new Octokit.SearchUsersRequest(q) { PerPage = 8 });

            var users = searchResult.Items.Select(u => new
            {
                login = u.Login,
                avatarUrl = u.AvatarUrl,
                name = u.Name,
                email = u.Email
            });

            return Results.Ok(new { users });
        });
    }
}
