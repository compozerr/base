using Auth.Abstractions;
using Auth.Services;
using Github.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Octokit;

namespace Github.Endpoints.Installation;

public static class PlaygroundRoute
{
    public const string Route = "playground";
    public static RouteHandlerBuilder AddPlaygroundRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, async (IGithubService githubService, ICurrentUserAccessor currentUserAccessor) =>
        {

            var client = await githubService.GetUserClient(currentUserAccessor.CurrentUserId!);

            if (client is null)
            {
                return Results.Unauthorized();
            }

            var repositories = await client.Repository.GetAllForCurrent();
            var installations = await client.GitHubApps.GetAllInstallationsForCurrentUser();

            var newRepository = new NewRepository("compozerr-playground")
            {
                Private = false,
                Description = "Playground repository for Compozerr",
                AutoInit = true
            };

            var response = await client.Repository.Create(newRepository);


            return Results.Ok();
        });
    }
}