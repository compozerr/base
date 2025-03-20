using Api.Hosting.Endpoints.Projects.ProjectDomains;
using Api.Hosting.Endpoints.Projects.ProjectEnvironment;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects;

public static class ProjectsGroup
{
    public const string Route = "projects";

    public static RouteGroupBuilder AddProjectsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddProjectDomainsGroup();
        group.AddProjectEnvironmentGroup();

        return group;
    }
}
