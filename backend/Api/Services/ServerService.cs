using Api.Abstractions;
using Api.Data.Repositories;
using Api.Hosting.Services;

namespace Api.Services;

public interface IServerService
{
    Task<string> CreateNewServer();
    Task<ServerId> UpdateServer(
        string secret,
        string isoCountryCode,
        string machineId,
        string ram,
        string vCpu,
        string ip,
        string hostName,
        string apiDomain);

    Task<string> CreateAndStorePrivateKeyAsync(ServerId serverId);
}

public class ServerService(
    IServerRepository serverRepository,
    ICryptoService cryptoService) : IServerService
{
    private static string GenerateNewSecret()
    {
        return $"sec_{Guid.NewGuid().ToString().Replace("-", "")}";
    }

    public async Task<string> CreateNewServer()
    {
        var newSecret = GenerateNewSecret();

        await serverRepository.AddNewServer(newSecret);

        return newSecret;
    }

    public Task<string> CreateAndStorePrivateKeyAsync(ServerId serverId)
        => cryptoService.CreateAndStorePrivateKeyAsync(serverId.Value.ToString());

    public async Task<ServerId> UpdateServer(
        string secret,
        string isoCountryCode,
        string machineId,
        string ram,
        string vCpu,
        string ip,
        string hostName,
        string apiDomain)
    {
        var server = await serverRepository.UpdateServer(
            secret,
            isoCountryCode,
            machineId,
            ram,
            vCpu,
            ip,
            hostName,
            apiDomain);

        return server.Id;
    }
}