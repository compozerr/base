using Api.Abstractions;

namespace Api.Hosting.Services;

public interface IHostingApiFactory
{
    Task<HostingApi> GetHostingApiAsync(ServerId serverId);
}

public sealed class HostingApiFactory(IHostingServerHttpClientFactory hostingServerHttpClientFactory) : IHostingApiFactory
{
    public Task<HostingApi> GetHostingApiAsync(ServerId serverId)
        => new HostingApi(
            serverId,
            hostingServerHttpClientFactory).Initialize();
}