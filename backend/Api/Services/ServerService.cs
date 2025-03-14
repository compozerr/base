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
    IHashService hashService,
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
        var hashedSecret = hashService.Hash(newSecret);

        await serverRepository.AddNewServer(hashedSecret);

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
        var hashedSecret = hashService.Hash(secret);

        var server = await serverRepository.UpdateServer(
            hashedSecret,
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