using Core.Extensions;
using Core.Feature;
using Microsoft.Extensions.DependencyInjection;
using Storage.Options;

namespace Storage;

public class StorageFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {
        var storageProvider = Environment.GetEnvironmentVariable("STORAGE__PROVIDER") ?? "filesystem";

        switch (storageProvider.ToLowerInvariant())
        {
            case "garage":
                services.AddRequiredConfigurationOptions<GarageStorageOptions>("GARAGE");
                services.AddSingleton<IStorageService, GarageStorageService>();
                break;
            case "filesystem":
            default:
                services.AddRequiredConfigurationOptions<FileSystemStorageOptions>("STORAGE");
                services.AddSingleton<IStorageService, FileSystemStorageService>();
                break;
        }
    }
}
