using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Repos;

public static class ReposGroup
{
    public const string Route = "repos";

    public static RouteGroupBuilder AddReposGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetReposForInstallationRoute();

        return group;
    }
}