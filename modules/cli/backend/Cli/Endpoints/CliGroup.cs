using Carter;
using Cli.Endpoints.Docker;
using Cli.Endpoints.Repos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints;

public class CliGroup : CarterModule
{
    public CliGroup() : base("cli")
    {
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddDockerGroup();
        app.AddReposGroup();
    }
}