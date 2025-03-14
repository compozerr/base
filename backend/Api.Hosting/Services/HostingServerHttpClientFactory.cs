using Api.Abstractions;
using Api.Abstractions.Exceptions;
using Api.Data.Repositories;

namespace Api.Hosting.Services;

public interface IHostingServerHttpClientFactory
{
    Task<HostingServerHttpClient> GetHostingServerHttpClientAsync(ServerId serverId);
}

public class HostingServerHttpClientFactory(
    IServerRepository serverRepository,
    IHttpClientFactory httpClientFactory,
    ICryptoService cryptoService) : IHostingServerHttpClientFactory
{
    public async Task<HostingServerHttpClient> GetHostingServerHttpClientAsync(ServerId serverId)
    {
        var server = await serverRepository.GetByIdAsync(serverId) ?? throw new ServerNotFoundException();

        var httpClient = httpClientFactory.CreateClient(nameof(HostingServerHttpClient));
        httpClient.BaseAddress = new Uri(server.ApiDomain);

        return new HostingServerHttpClient(
            httpClient,
            server,
            cryptoService);
    }
}