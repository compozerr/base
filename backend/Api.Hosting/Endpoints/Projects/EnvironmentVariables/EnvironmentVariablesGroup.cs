using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.EnvironmentVariables;

public static class EnvironmentVariablesGroup
{
    public const string Route = "{projectId:guid}/environments";

    public static RouteGroupBuilder AddEnvironmentVariablesGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetEnvironmentVariablesByIdRoute();

        return group;
    }
}
