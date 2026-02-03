using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.GithubStatus;

public static class GithubGroup
{
    public const string Route = "github";

    public static RouteGroupBuilder AddGithubGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetGithubStatusRoute();

        return group;
    }
}
