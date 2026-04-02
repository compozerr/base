using Api.Endpoints.Projects.Environments.Create;
using Api.Endpoints.Projects.Environments.Delete;
using Api.Endpoints.Projects.Environments.List;
using Api.Endpoints.Projects.Environments.Promote;

namespace Api.Endpoints.Projects.Environments;

public static class EnvironmentsGroup
{
    public const string Route = "{projectId:guid}/environments";

    public static RouteGroupBuilder AddEnvironmentsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetEnvironmentsRoute();
        group.AddCreateEnvironmentRoute();
        group.AddDeleteEnvironmentRoute();
        group.AddPromoteEnvironmentRoute();

        return group;
    }
}
