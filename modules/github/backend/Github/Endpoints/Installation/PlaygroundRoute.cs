using Auth.Abstractions;
using Github.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Github.Endpoints.Installation;

public static class PlaygroundRoute
{
    public const string Route = "playground";
    public static RouteHandlerBuilder AddPlaygroundRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, async (IGithubService githubService) =>
        {

            var client = await githubService.GetUserClient(UserId.Parse("2e60160d-ed5d-4cfd-9bb0-999560193b51"));

            if (client is null)
            {
                return Results.Unauthorized();
            }

            var repositories = await client.Repository.GetAllForCurrent();
            var installations = await client.GitHubApps.GetAllInstallationsForCurrentUser();



            return Results.Ok();
        });
    }
}