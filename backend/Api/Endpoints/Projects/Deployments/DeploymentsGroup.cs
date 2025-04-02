using Api.Endpoints.Projects.Deployments.Logs.Get;

namespace Api.Endpoints.Projects.Deployments;

public static class DeploymentsGroup
{
    public const string Route = "{projectId:guid}/deployments";

    public static RouteGroupBuilder AddDeploymentsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetDeploymentsRoute();
        group.AddGetDeploymentRoute();
        group.AddGetLogRoute();

        return group;
    }
}
