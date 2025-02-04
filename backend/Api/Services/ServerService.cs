using Api.Repositories;

namespace Api.Services;

public interface IServerService
{
    Task<string> CreateNewServer();
    Task UpdateServer(
        string secret,
        string isoCountryCode,
        string machineId,
        string ram,
        string vCpu,
        string ip);
}

public class ServerService(
    IHashService hashService,
    IServerRepository serverRepository) : IServerService
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

    public async Task UpdateServer(
        string secret,
        string isoCountryCode,
        string machineId,
        string ram,
        string vCpu,
        string ip)
    {
        var hashedSecret = hashService.Hash(secret);

        await serverRepository.UpdateServer(hashedSecret, isoCountryCode, machineId, ram, vCpu, ip);
    }
}