using Microsoft.Extensions.DependencyInjection;

namespace Storage;

public class StorageFeature : IFeature
{
    public void Configure(IServiceCollection services)
    {
        services.AddSingleton<IStorageService, StorageService>();
        services.AddScoped<IStorageClient, StorageClient>();
    }
} 