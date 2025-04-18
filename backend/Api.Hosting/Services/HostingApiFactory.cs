using Api.Abstractions;
using Github.Services;
using MediatR;
using Microsoft.Extensions.Hosting;

namespace Api.Hosting.Services;

public interface IHostingApiFactory
{
    Task<IHostingApi> GetHostingApiAsync(ServerId serverId);
}

public sealed class HostingApiFactory(
    IHostingServerHttpClientFactory hostingServerHttpClientFactory,
    IGithubService githubService,
    IHostEnvironment hostEnvironment,
    IMediator mediator) : IHostingApiFactory
{
    public async Task<IHostingApi> GetHostingApiAsync(ServerId serverId)
    {
        if (hostEnvironment.IsDevelopment())
            return new MockHostingApi();

        return await new HostingApi(
           serverId,
           hostingServerHttpClientFactory,
           githubService,
           mediator).InitializeAsync();
    }
}