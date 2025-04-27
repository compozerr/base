using Api.Data;
using Api.Data.Repositories;
using Api.Endpoints.Projects.Project.Get;
using Api.Hosting.Services;

namespace Api.Endpoints.Projects;

public sealed record GetProjectsResponse(
    int TotalProjectsCount,
    int RunningProjectsCount,
    List<GetProjectResponse> Projects
);

public static class GetProjectsRoute
{
    public const string Route = "/";

    public static RouteHandlerBuilder AddGetProjectsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<GetProjectsResponse> ExecuteAsync(
        IProjectRepository projectRepository)
    {
        var projects = await projectRepository.GetProjectsForUserAsync();
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

        var totalProjectsCount = projectsDto.Count;
        var runningProjectsCount = projectsDto.Sum(x => x.State == ProjectState.Running ? 1 : 0);

        return new(
            totalProjectsCount,
            runningProjectsCount,
            projectsDto);
    }
}
