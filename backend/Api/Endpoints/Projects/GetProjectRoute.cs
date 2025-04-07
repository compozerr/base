using Api.Abstractions;
using Api.Data.Extensions;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Auth.Services;

namespace Api.Endpoints.Projects;

public sealed record GetProjectResponse(
    Guid Id,
    string Name,
    string RepoName,
    State State,
    decimal VCpuHours,
    DateTime StartDate,
    List<string> Domains,
    string? PrimaryDomain);

public static class GetProjectRoute
{
    public const string Route = "{projectId:guid}";

    public static RouteHandlerBuilder AddGetProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<GetProjectResponse> ExecuteAsync(
        Guid projectId,
        ICurrentUserAccessor currentUserAccessor,
        IProjectRepository projectRepository)
    {
        var projectIdConverted = ProjectId.Create(projectId);

        var project = await projectRepository.GetProjectByIdWithDomainsAsync(projectIdConverted) ?? throw new ArgumentException("Project not found");

        if (project.UserId != currentUserAccessor.CurrentUserId)
        {
            throw new ArgumentException("Project not found");
        }

        return new GetProjectResponse(
            project.Id.Value,
            project.Name,
            RepoUri.Parse(project.RepoUri).RepoName,
            State.Running,
            0.5m,
            project.UpdatedAtUtc ?? DateTime.Now,
            [.. project.Domains?.Select(x => x.GetValue) ?? []],
            project.Domains?.GetPrimary()?.GetValue
        );
    }
}
