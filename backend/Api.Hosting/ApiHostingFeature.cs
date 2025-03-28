using Api.Hosting.Services;
using Core.Feature;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Hosting;

public class ApiHostingFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddSingleton<ICryptoService, CryptoService>();
        services.AddTransient<IHostingServerHttpClientFactory, HostingServerHttpClientFactory>();
        services.AddTransient<IHostingApiFactory, HostingApiFactory>();
    }
}
