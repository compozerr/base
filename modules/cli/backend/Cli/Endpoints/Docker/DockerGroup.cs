using Carter;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Docker;

public class DockerGroup : CarterModule
{
    public DockerGroup() : base("/docker")
    {
        WithTags(nameof(Docker));
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddPushRoute();
    }
}