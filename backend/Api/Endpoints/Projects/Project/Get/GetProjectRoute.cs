using Api.Abstractions;
using Api.Data.Extensions;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Auth.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints.Projects.Project.Get;

public static class GetProjectRoute
{
    public const string Route = "{projectId:guid}";

    public static RouteHandlerBuilder AddGetProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<Results<Ok<GetProjectResponse>, NotFound>> ExecuteAsync(
        ProjectId projectId,
        ICurrentUserAccessor currentUserAccessor,
        IProjectRepository projectRepository)
    {
        var project = await projectRepository.GetProjectByIdWithDomainsAsync(projectId);

        if (project == null || project.UserId != currentUserAccessor.CurrentUserId)
        {
            return TypedResults.NotFound();
        }

        var projectDomains = project.Domains?.Where(x => x.DeletedAtUtc == null).ToList();

        return TypedResults.Ok(new GetProjectResponse(
            project.Id.Value,
            project.Name,
            RepoUri.Parse(project.RepoUri).RepoName,
            project.State,
            [.. projectDomains?.Select(x => x.GetValue) ?? []],
            project.ServerTierId.Value,
            projectDomains?.GetPrimary()?.GetValue,
            project.Type
        ));
    }
}
