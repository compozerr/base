
using Api.Abstractions;
using Api.Data.Repositories;
using Api.Endpoints.Projects.Deployments;
using Api.Endpoints.Projects.Domains;
using Api.Endpoints.Projects.Project.ChangeTier;
using Api.Endpoints.Projects.Project.Delete;
using Api.Endpoints.Projects.Project.ExecuteCommand;
using Api.Endpoints.Projects.Project.Get;
using Api.Endpoints.Projects.Project.Start;
using Api.Endpoints.Projects.Project.RepoStatus;
using Api.Endpoints.Projects.Project.Stop;
using Api.Endpoints.Projects.Project.UpdateRepoUrl;
using Api.Endpoints.Projects.ProjectEnvironment;
using Api.Endpoints.Projects.Services;
using Api.Endpoints.Projects.Usage.Get;
using Api.Hosting.Services;

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
        app.AddGetRepoStatusRoute();
        app.AddUpdateRepoUrlRoute();
        app.AddStartProjectRoute();
        app.AddStopProjectRoute();
        app.AddDeleteProjectRoute();
        app.AddExecuteCommandRoute();

        // Debug endpoint for terminal WebSocket testing
        app.MapGet("{projectId:guid}/debug-terminal", async (
            ProjectId projectId,
            IProjectRepository projectRepository,
            IHostingApiFactory hostingApiFactory,
            CancellationToken ct) =>
        {
            var project = await projectRepository.GetByIdAsync(projectId, ct);
            if (project?.ServerId is not { } serverId)
                return Results.NotFound("Project not found");

            var hostingApi = await hostingApiFactory.GetHostingApiAsync(serverId);
            var result = await hostingApi.DebugTerminalAsync(projectId);
            return Results.Content(result, "application/json");
        });

        app.AddGetProjectsRoute();

        app.AddDeploymentsGroup();
        app.AddProjectEnvironmentGroup();
        app.AddDomainsGroup();
        app.AddServicesGroup();

        app.AddGetUsageRoute();
    }
}
