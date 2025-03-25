
using Api.Endpoints.Projects.Deployments;

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
        app.AddGetProjectsRoute();

        app.AddDeploymentsGroup();
    }
}
