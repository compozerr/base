using Api.Abstractions;
using Api.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.ProductionEnvironment;

public sealed record GetProductionEnvironmentResponse(Guid Id, string? Name);

public static class GetProductionEnvironmentRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddGetProductionEnvironmentRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<Results<Ok<GetProductionEnvironmentResponse>, NotFound>> ExecuteAsync(
        ProjectId projectId,
        IProjectEnvironmentRepository environmentRepository)
    {
        var env = await environmentRepository.GetProductionEnvironmentAsync(projectId);
        return env is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new GetProductionEnvironmentResponse(env.Id.Value, env.Name));
    }
}
