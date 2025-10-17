using Api.Endpoints.Projects.Services.Get;

namespace Api.Endpoints.Projects.Services;

public static class ServicesGroup
{
    public const string Route = "{projectId:guid}";

    public static RouteGroupBuilder AddServicesGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetProjectServicesRoute();

        return group;
    }
}
