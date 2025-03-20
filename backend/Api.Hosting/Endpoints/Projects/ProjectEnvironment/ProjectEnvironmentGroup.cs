using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.ProjectEnvironment;

public static class ProjectEnvironmentGroup
{
    public const string Route = "{projectId:guid}/environment";

    public static RouteGroupBuilder AddProjectEnvironmentGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetProjectEnvironmentRoute();

        return group;
    }
}
