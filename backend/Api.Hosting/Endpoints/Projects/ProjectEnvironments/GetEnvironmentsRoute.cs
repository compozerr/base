using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.ProjectEnvironments;

public sealed record HostingEnvironmentDto(
    ProjectEnvironmentId Id,
    string? Name,
    EnvironmentType Type,
    string? Subdomain);

public static class GetEnvironmentsRoute
{
    public const string Route = "/";

    public static RouteHandlerBuilder AddGetEnvironmentsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<Ok<List<HostingEnvironmentDto>>> ExecuteAsync(
        ProjectId projectId,
        IProjectEnvironmentRepository envRepository,
        CancellationToken cancellationToken)
    {
        var envs = await envRepository.GetByProjectIdAsync(projectId, cancellationToken);

        var dtos = envs
            .Select(e => new HostingEnvironmentDto(e.Id, e.Name, e.Type, e.Subdomain))
            .ToList();

        return TypedResults.Ok(dtos);
    }
}
