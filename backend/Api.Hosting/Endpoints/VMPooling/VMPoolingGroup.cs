using Api.Hosting.Endpoints.VMPooling.InitiatePoolSync;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.VMPooling;

public static class VMPoolingGroup
{
    public const string Route = "vmpooling";

    public static RouteGroupBuilder AddVMPoolingGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddInitiatePoolSyncRoute();

        return group;
    }
}
