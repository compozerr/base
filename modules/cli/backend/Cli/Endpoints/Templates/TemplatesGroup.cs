using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Templates;

public static class TemplatesGroup
{
    public const string Route = "templates";

    public static RouteGroupBuilder AddTemplatesGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddDetectTemplatesRoute();

        return group;
    }
}
