using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.ProjectEnvironments;

public static class ProjectEnvironmentsGroup
{
    public const string Route = "{projectId:guid}/environments";

    public static RouteGroupBuilder AddProjectEnvironmentsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetEnvironmentsRoute();

        return group;
    }
}
