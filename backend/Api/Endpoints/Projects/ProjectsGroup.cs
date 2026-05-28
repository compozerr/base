
using Api.Abstractions;
using Api.Data.Repositories;
using Api.Endpoints.Projects.Deployments;
using Api.Endpoints.Projects.Domains;
using Api.Endpoints.Projects.Environments;
using Api.Endpoints.Projects.Project.ChangeRepository;
using Api.Endpoints.Projects.Project.ChangeTier;
using Api.Endpoints.Projects.Project.Delete;
using Api.Endpoints.Projects.Project.ExecuteCommand;
using Api.Endpoints.Projects.Project.Get;
using Api.Endpoints.Projects.Project.Start;
using Api.Endpoints.Projects.Project.Stop;
using Api.Endpoints.Projects.ProjectEnvironment;
using Api.Endpoints.Projects.Services;
using Api.Endpoints.Projects.Usage.Get;
using Api.Hosting.Services.EnvironmentTargetResolver;
using Microsoft.EntityFrameworkCore;

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
        app.AddExecuteCommandRoute();
        app.AddChangeRepositoryRoute();

        // Debug endpoint for terminal WebSocket testing. Targets the project's
        // Production env (the env the debug terminal historically pointed at).
        app.MapGet("{projectId:guid}/debug-terminal", async (
            ProjectId projectId,
            Api.Data.ApiDbContext context,
            IEnvironmentTargetResolver environmentTargetResolver,
            CancellationToken ct) =>
        {
            var prodEnvId = await context.ProjectEnvironments
                .Where(e => e.ProjectId == projectId && e.Type == Api.Data.EnvironmentType.Production)
                .Select(e => (ProjectEnvironmentId?)e.Id)
                .FirstOrDefaultAsync(ct);

            if (prodEnvId is null)
                return Results.NotFound("Project not found");

            var server = await environmentTargetResolver.TryGetServerAsync(prodEnvId, ct);
            if (server is null)
                return Results.NotFound("Project not found");

            var hostingApi = await environmentTargetResolver.GetHostingApiAsync(prodEnvId, ct);
            var result = await hostingApi.DebugTerminalAsync(projectId);
            return Results.Content(result, "application/json");
        });

        app.AddGetProjectsRoute();

        app.AddDeploymentsGroup();
        app.AddProjectEnvironmentGroup();
        app.AddEnvironmentsGroup();
        app.AddDomainsGroup();
        app.AddServicesGroup();

        app.AddGetUsageRoute();
    }
}
