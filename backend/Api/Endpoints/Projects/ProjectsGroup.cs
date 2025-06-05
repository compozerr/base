
using Api.Endpoints.Projects.Deployments;
using Api.Endpoints.Projects.Domains;
using Api.Endpoints.Projects.Project.ChangeTier;
using Api.Endpoints.Projects.Project.Delete;
using Api.Endpoints.Projects.Project.Get;
using Api.Endpoints.Projects.Project.Start;
using Api.Endpoints.Projects.Project.Stop;
using Api.Endpoints.Projects.ProjectEnvironment;
using Api.Endpoints.Projects.Usage.Get;

namespace Api.Endpoints.Projects;

public class ProjectsGroup : CarterModule
{
    public ProjectsGroup() : base("/projects")
    {
        WithTags(nameof(Projects));
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddGetProjectRoute();
        app.AddStartProjectRoute();
        app.AddStopProjectRoute();
        app.AddDeleteProjectRoute();

        app.AddGetProjectsRoute();

        app.AddDeploymentsGroup();
        app.AddProjectEnvironmentGroup();
        app.AddDomainsGroup();

        app.AddGetUsageRoute();

        app.AddChangeTierRoute();
    }
}
