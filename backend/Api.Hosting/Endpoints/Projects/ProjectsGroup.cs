using Api.Hosting.Endpoints.Projects.Config;
using Api.Hosting.Endpoints.Projects.EnvironmentVariables;
using Api.Hosting.Endpoints.Projects.GetAll;
using Api.Hosting.Endpoints.Projects.ProductionEnvironment;
using Api.Hosting.Endpoints.Projects.ProjectDomains;
using Api.Hosting.Endpoints.Projects.ProjectEnvironment;
using Api.Hosting.Endpoints.Projects.ProjectState;
using Api.Hosting.Endpoints.Projects.Services;
using Api.Hosting.Endpoints.Projects.TcpAllocations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects;

public static class ProjectsGroup
{
    public const string Route = "projects";

    public static RouteGroupBuilder AddProjectsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetAllProjectsRoute();
        group.AddProjectDomainsGroup();
        group.AddProjectEnvironmentGroup();
        group.AddEnvironmentVariablesGroup();
        group.AddProductionEnvironmentGroup();
        group.AddProjectStateRoute();
        group.AddReportProjectConfigRoute();
        group.AddReportServicesRoute();
        group.AddReportEnvironmentServicesRoute();
        group.AddGetTcpAllocationsRoute();

        return group;
    }
}
