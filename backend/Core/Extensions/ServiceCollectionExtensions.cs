using Microsoft.Extensions.DependencyInjection;

namespace Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfigurationOptions<T>(this IServiceCollection services, string configSectionPath, bool validate = true) where T : class
    {
        var bindConfiguration = services.AddOptions<T>()
                                        .BindConfiguration(configSectionPath);
        if (validate)
        {
            bindConfiguration.ValidateDataAnnotations()
                             .ValidateOnStart();
        }

        return services;
    }
}