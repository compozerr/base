using Api.Features.N8n.Endpoints.CreateN8nProject;

namespace Api.Features.N8n.Endpoints;

public static class N8nGroup
{
    public const string Route = "n8n";

    public static RouteGroupBuilder AddN8nGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddCreateN8nProjectRoute();

        return group;
    }
}
