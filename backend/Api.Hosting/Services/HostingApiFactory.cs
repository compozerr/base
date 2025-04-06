using Api.Abstractions;
using Github.Services;
using MediatR;

namespace Api.Hosting.Services;

public interface IHostingApiFactory
{
    Task<HostingApi> GetHostingApiAsync(ServerId serverId);
}

public sealed class HostingApiFactory(
    IHostingServerHttpClientFactory hostingServerHttpClientFactory,
    IGithubService githubService,
    IMediator mediator) : IHostingApiFactory
{
    public Task<HostingApi> GetHostingApiAsync(ServerId serverId)
        => new HostingApi(
            serverId,
            hostingServerHttpClientFactory,
            githubService,
            mediator).Initialize();
}