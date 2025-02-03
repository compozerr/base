namespace Api.Endpoints.Deployment;

public static class DeploymentGroup
{
    public const string Route = "deployment";

    public static RouteGroupBuilder AddDeploymentGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddDeployProjectRoute();
        group.AddGetProjectRoute();
        group.AddGetProjectsRoute();

        group.AddDeployModuleRoute();

        return group;
    }
}