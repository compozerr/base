using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Locations;

public static class LocationsGroup
{
    public const string Route = "Locations";

    public static RouteGroupBuilder AddLocationsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetLocationsRoute();

        return group;
    }
}
