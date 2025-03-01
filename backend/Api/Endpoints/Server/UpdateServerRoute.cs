using System.Security.Cryptography;
using Api.Services;

namespace Api.Endpoints.Server;

public static class UpdateServerRoute
{
    public const string Route = "/";

    public static RouteHandlerBuilder AddUpdateServerRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPut(Route, ExecuteAsync);
    }

    public static async Task<UpdateServerResponse> ExecuteAsync(UpdateServerRequest request, IServerService serverService)
    {
        try
        {
            var serverId = await serverService.UpdateServer(
                request.Secret,
                request.IsoCountryCode,
                request.MachineId,
                request.Ram,
                request.VCpu,
                request.Ip);


            var publicKeyBase64 = await serverService.CreateAndStorePrivateKeyAsync(serverId);

            return new(true, publicKeyBase64);
        }
        catch (ServerNotFoundException)
        {
            return new(false, null);
        }
    }
}
