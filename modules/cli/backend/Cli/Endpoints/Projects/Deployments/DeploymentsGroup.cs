using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Projects.Deployments;

public static class DeploymentsGroup
{
    public const string Route = "{projectId:guid}/deployments";

    public static RouteGroupBuilder AddDeploymentsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddCreateDeploymentRoute();

        return group;
    }
}
