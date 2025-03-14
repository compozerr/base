using Api.Abstractions;
using Github.Services;

namespace Api.Hosting.Services;

public interface IHostingApiFactory
{
    Task<HostingApi> GetHostingApiAsync(ServerId serverId);
}

public sealed class HostingApiFactory(
    IHostingServerHttpClientFactory hostingServerHttpClientFactory,
    IGithubService githubService) : IHostingApiFactory
{
    public Task<HostingApi> GetHostingApiAsync(ServerId serverId)
        => new HostingApi(
            serverId,
            hostingServerHttpClientFactory,
            githubService).Initialize();
}