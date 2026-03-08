using Api.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Api.Hosting.Endpoints.Projects.TcpAllocations;

public sealed record TcpAllocationDto(
    string ProjectId,
    string ServiceName,
    string InternalPort,
    int AllocatedPort);

public static class GetTcpAllocationsRoute
{
    public const string Route = "/tcp-allocations";

    public static RouteHandlerBuilder AddGetTcpAllocationsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<Ok<List<TcpAllocationDto>>> ExecuteAsync(
        ApiDbContext context)
    {
        var allocations = await context.TcpPortAllocations
            .Select(a => new TcpAllocationDto(
                a.ProjectId.Value.ToString(),
                a.ServiceName,
                a.InternalPort,
                a.AllocatedPort))
            .ToListAsync();

        return TypedResults.Ok(allocations);
    }
}
