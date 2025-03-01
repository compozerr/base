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

            using var rsa = RSA.Create(2048);
            var privateKey = rsa.ExportRSAPrivateKey();
            await serverService.StorePrivateKeyAsync(
                serverId,
                privateKey);

            var publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
            return new(true, $"-----BEGIN PUBLIC KEY-----\n{publicKey}\n-----END PUBLIC KEY-----");
        }
        catch (ServerNotFoundException)
        {
            return new(false, null);
        }
    }
}
