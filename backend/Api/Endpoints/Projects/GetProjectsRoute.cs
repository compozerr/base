using Api.Data;
using Api.Data.Repositories;
using Api.Endpoints.Projects.Project.Get;
using Api.Hosting.Services;

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
        string? search = null,
        int stateFlags = (int)ProjectStateFilter.All,
        int page = 1,
        int pageSize = 20)
    {
        var stateFilter = Enum.Parse<ProjectStateFilter>(stateFlags.ToString());
        var (projects, totalProjectsCount, runningProjectsCount) = await projectRepository.GetProjectsForUserPagedAsync(page, pageSize, search, stateFilter);

        List<GetProjectResponse> projectsDto = [.. projects.Select(
            p => new GetProjectResponse(
                p.Id.Value,
                p.Name,
                RepoUri.Parse(p.RepoUri).RepoName,
                p.State,
                p.UpdatedAtUtc ?? DateTime.Now,
                [.. p.Domains?.Select(x => x.GetValue) ?? []],
                p.Domains!.FirstOrDefault()?.GetValue ?? "Unknown")
            )];

        return new(
            totalProjectsCount,
            runningProjectsCount,
            projectsDto,
            page,
            pageSize);
    }
}
