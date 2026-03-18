using Api.Data;
using Api.Data.Repositories;
using Api.Endpoints.Projects.Project.Get;
using Api.Hosting.Services;
using Organizations.Services;

namespace Api.Endpoints.Projects;

public sealed record GetProjectsResponse(
    int TotalProjectsCount,
    int RunningProjectsCount,
    List<GetProjectResponse> Projects,
    int Page,
    int PageSize
);

public static class GetProjectsRoute
{
    public const string Route = "/";

    public static RouteHandlerBuilder AddGetProjectsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<GetProjectsResponse> ExecuteAsync(
        IProjectRepository projectRepository,
        IOrganizationContextAccessor orgContextAccessor,
        string? search = null,
        int stateFlags = (int)ProjectStateFilter.All,
        int page = 1,
        int pageSize = 20)
    {
        var stateFilter = Enum.Parse<ProjectStateFilter>(stateFlags.ToString());

        // Try to get org-scoped projects; fall back to user-scoped if no org context
        (List<Api.Data.Project> projects, int totalProjectsCount, int runningProjectsCount, Dictionary<Api.Abstractions.ProjectId, Api.Abstractions.DeploymentId> activeDeployments) result;
        try
        {
            var orgId = await orgContextAccessor.GetCurrentOrganizationIdAsync();
            result = await projectRepository.GetProjectsForOrganizationPagedAsync(orgId, page, pageSize, search, stateFilter);
        }
        catch
        {
            result = await projectRepository.GetProjectsForUserPagedAsync(page, pageSize, search, stateFilter);
        }

        var (projects, totalProjectsCount, runningProjectsCount, activeDeployments) = result;

        List<GetProjectResponse> projectsDto = [.. projects.Select(
            p => new GetProjectResponse(
                p.Id.Value,
                p.Name,
                RepoUri.Parse(p.RepoUri).RepoName,
                p.State,
                [.. p.Domains?.Select(x => x.GetValue) ?? []],
                p.ServerTierId.Value,
                null,
                p.Domains!.FirstOrDefault()?.GetValue ?? "Unknown",
                p.Type,
                activeDeployments.TryGetValue(p.Id, out var deploymentId) ? deploymentId.Value : null)
            )];

        return new(
            totalProjectsCount,
            runningProjectsCount,
            projectsDto,
            page,
            pageSize);
    }
}
