using Core.Feature;

namespace Storage;

public class StorageFeature : IFeature
{
    public void Configure(IServiceCollection services)
    {
        services.AddSingleton<IStorageService, StorageService>();
    }
}