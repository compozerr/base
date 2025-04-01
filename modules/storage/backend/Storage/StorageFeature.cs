using Core.Feature;
using Microsoft.Extensions.DependencyInjection;

namespace Storage;

public class StorageFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IStorageService, StorageService>();
    }
}