using Api.Endpoints.Projects.ProjectEnvironment.ChangeAutoDeploy;

namespace Api.Endpoints.Projects.ProjectEnvironment;

public static class ProjectEnvironmentGroup
{
    public const string Route = "{projectId:guid}/environment";

    public static RouteGroupBuilder AddProjectEnvironmentGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetProjectEnvironmentRoute();
        group.AddUpsertProjectEnvironmentVariablesRoute();
        group.AddChangeAutoDeployRoute();

        return group;
    }
}
