using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Docker;

public static class DockerGroup
{
    public const string Route = "docker";

    public static RouteGroupBuilder AddDockerGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);
        
        group.AddPushRoute();

        return group;
    }
}