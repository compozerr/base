using Cli.Endpoints.Modules.Add;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Modules;

public static class ModuleGroup
{
    public const string Route = "modules";

    public static RouteGroupBuilder AddModuleGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddAddModuleRoute();

        return group;
    }
}
