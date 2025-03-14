using Api.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Locations;

public static class GetLocationsRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddGetLocationsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<List<string>> ExecuteAsync(ILocationRepository locationRepository)
        => locationRepository.GetUniquePublicLocationIsoCodes();
}
