using Api.Data.Repositories;
using Api.Data.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Server;

public static class GetServersRoute
{
    public const string Route = "/";

    public static RouteHandlerBuilder AddGetServersRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync)
                  .RequireAuthorization();
    }

    public static async Task<Ok<GetServersResponse>> ExecuteAsync(
        IServerRepository serverRepository,
        IServerVisibilityFilter serverVisibilityFilter,
        CancellationToken cancellationToken)
    {
        var query = serverRepository.Query()
            .Include(s => s.Location)
            .Where(s => s.DeletedAtUtc == null);

        var visible = serverVisibilityFilter.ApplyForCurrentUser(query);

        var servers = await visible
            .Select(s => new ServerDto(
                s.Id,
                s.LocationId,
                s.Location != null ? s.Location.IsoCountryCode : null,
                s.ServerVisibility,
                s.HostName,
                s.ApiDomain,
                s.Ip))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new GetServersResponse(servers));
    }
}
