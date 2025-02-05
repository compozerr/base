using Api.Services;

namespace Api.Endpoints.Server;

public static class UpdateServerRoute
{
    public const string Route = "/";

    public static RouteHandlerBuilder AddUpdateServerRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPut(Route, ExecuteAsync);
    }

    public static async Task<bool> ExecuteAsync(UpdateServerRequest request, IServerService serverService)
    {
        try
        {
            await serverService.UpdateServer(
                request.Secret,
                request.IsoCountryCode,
                request.MachineId,
                request.Ram,
                request.VCpu,
                request.Ip);

            return true;
        }
        catch (ServerNotFoundException)
        {
            return false;
        }
    }
}
