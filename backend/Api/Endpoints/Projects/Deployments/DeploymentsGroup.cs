using Api.Endpoints.Projects.Deployments.DeployFromLatestCommit;
using Api.Endpoints.Projects.Deployments.Logs.Get;
using Api.Endpoints.Projects.Deployments.RedeployDeployment;

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
        group.AddRedeployDeploymentRoute();
        group.AddDeployFromLatestCommitRoute();

        return group;
    }
}
