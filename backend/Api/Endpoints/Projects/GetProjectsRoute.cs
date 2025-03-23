using Api.Data.Repositories;
using Api.Hosting.Services;

namespace Api.Endpoints.Projects;

public sealed record GetProjectsResponse(
    int TotalProjectsCount,
    int RunningProjectsCount,
    decimal TotalVCpuHours,
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
                State.Running,
                0.5m,
                p.UpdatedAtUtc ?? DateTime.Now))];

        var totalProjectsCount = projectsDto.Count;
        var runningProjectsCount = projectsDto.Sum(x => x.State == State.Running ? 1 : 0);
        var totalVCpuHours = projectsDto.Sum(x => x.VCpuHours);


        return new(
            totalProjectsCount,
            runningProjectsCount,
            totalVCpuHours,
            projectsDto);
    }
}
