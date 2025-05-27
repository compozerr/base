using Api.Abstractions;
using Api.Data.Extensions;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Auth.Services;

namespace Api.Endpoints.Projects.Project.Get;

public static class GetProjectRoute
{
    public const string Route = "{projectId:guid}";

    public static RouteHandlerBuilder AddGetProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<GetProjectResponse> ExecuteAsync(
        ProjectId projectId,
        ICurrentUserAccessor currentUserAccessor,
        IProjectRepository projectRepository)
    {
        var project = await projectRepository.GetProjectByIdWithDomainsAsync(projectId) ?? throw new ArgumentException("Project not found");

        if (project.UserId != currentUserAccessor.CurrentUserId)
        {
            throw new ArgumentException("Project not found");
        }

        return new GetProjectResponse(
            project.Id.Value,
            project.Name,
            RepoUri.Parse(project.RepoUri).RepoName,
            project.State,
            [.. project.Domains?.Select(x => x.GetValue) ?? []],
            project.Domains?.GetPrimary()?.GetValue
        );
    }
}
