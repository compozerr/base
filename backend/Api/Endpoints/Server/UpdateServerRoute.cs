using Api.Services;

namespace Api.Endpoints.Server;

public static class UpdateServerRoute
{
    public const string Route = "/";

    public static RouteHandlerBuilder AddUpdateServerRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPut(Route, ExecuteAsync);
    }

    public static Task ExecuteAsync(UpdateServerRequest request, IServerService serverService)
        => serverService.UpdateServer(
            request.Secret,
            request.IsoCountryCode,
            request.MachineId,
            request.Ram,
            request.VCpu,
            request.Ip);
}
