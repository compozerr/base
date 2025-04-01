using Core.Extensions;
using Core.Feature;
using Microsoft.AspNetCore.Builder;
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

    void IFeature.ConfigureBuilder(WebApplicationBuilder builder)
    {
        builder.Configuration.AddAppSettings();
    }
}