using Api.Abstractions;
using Api.Endpoints.Server;
using Api.Repositories;

namespace Api.Services;

public interface IChildServerHttpClientFactory
{
    Task<ChildServerHttpClient> GetChildServerHttpClientAsync(ServerId serverId);
}

public class ChildServerHttpClientFactory(
    IServerRepository serverRepository,
    HttpClient httpClient,
    ICryptoService cryptoService) : IChildServerHttpClientFactory
{
    public async Task<ChildServerHttpClient> GetChildServerHttpClientAsync(ServerId serverId)
    {
        var server = await serverRepository.GetByIdAsync(serverId) ?? throw new ServerNotFoundException();

        return new ChildServerHttpClient(
            httpClient,
            server,
            cryptoService);
    }
}