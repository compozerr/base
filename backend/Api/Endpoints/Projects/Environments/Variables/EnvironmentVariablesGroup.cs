using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Endpoints.Projects.Environments.Variables;

public static class EnvironmentVariablesGroup
{
    public const string Route = "{environmentId:guid}/variables";

    public static RouteGroupBuilder AddEnvironmentVariablesGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetEnvironmentVariablesRoute();
        group.AddUpsertEnvironmentVariablesRoute();

        return group;
    }
}
