using Cli.Endpoints.Projects.Deployments;
using Cli.Endpoints.Projects.RestoreProject;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Projects;

public static class ProjectsGroup
{
    public const string Route = "projects";

    public static RouteGroupBuilder AddProjectsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddDeploymentsGroup();

        group.AddCreateProjectRoute();
        group.AddGetProjectRoute();
        group.AddGetProjectByRepoUrlRoute();

        group.AddRestoreProjectRoute();

        return group;
    }
}