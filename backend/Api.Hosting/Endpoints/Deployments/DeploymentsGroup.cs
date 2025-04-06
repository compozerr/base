using Api.Hosting.Endpoints.Deployments.ChangeDeploymentStatus;
using Api.Hosting.Endpoints.Deployments.Logs.Add;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Deployments;

public static class DeploymentsGroup
{
    public const string Route = "deployments";

    public static RouteGroupBuilder AddDeploymentsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddChangeDeploymentStatusRoute();
        group.AddAddLogRoute();

        return group;
    }
}
