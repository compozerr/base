using Api.Abstractions;
using Api.Data.Repositories;
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
    IProjectRepository projectRepository,
    IMediator mediator) : IHostingApiFactory
{
    public async Task<IHostingApi> GetHostingApiAsync(ServerId serverId)
    {
        if (hostEnvironment.IsDevelopment())
            return new MockHostingApi(projectRepository);

        return await new HostingApi(
           serverId,
           hostingServerHttpClientFactory,
           githubService,
           projectRepository,
           mediator).InitializeAsync();
    }
}