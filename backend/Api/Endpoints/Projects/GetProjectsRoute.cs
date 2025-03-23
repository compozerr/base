using Api.Data.Repositories;
using Api.Hosting.Services;

namespace Api.Endpoints.Projects;

public static class GetProjectsRoute
{
    public const string Route = "/";

    public static RouteHandlerBuilder AddGetProjectsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<IReadOnlyList<GetProjectResponse>> ExecuteAsync(
        IProjectRepository projectRepository)
    {
        var projects = await projectRepository.GetProjectsForUserAsync();

        return [.. projects.Select(
            p => new GetProjectResponse(
                p.Name,
                RepoUri.Parse(p.RepoUri).RepoName,
                State.Running,
                0.5m,
                p.UpdatedAtUtc ?? DateTime.Now))];
    }
}
