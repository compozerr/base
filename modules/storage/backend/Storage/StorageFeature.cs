using Core.Extensions;
using Core.Feature;
using Microsoft.Extensions.DependencyInjection;
using Storage.Options;

namespace Storage;

public class StorageFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IStorageService, StorageService>();
        services.AddRequiredConfigurationOptions<MinioOptions>("MINIO");
    }
}