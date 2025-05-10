using Carter;
using Cli.Endpoints.Docker;
using Cli.Endpoints.Locations;
using Cli.Endpoints.Modules;
using Cli.Endpoints.Projects;
using Cli.Endpoints.Repos;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints;

public class CliGroup : CarterModule
{
    public CliGroup() : base("cli")
    {
        RequireAuthorization();
        WithTags(nameof(Cli));
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddDockerGroup();
        app.AddReposGroup();
        app.AddProjectsGroup();
        app.AddLocationsGroup();
        app.AddModuleGroup();
    }
}