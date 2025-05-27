using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfigurationOptions<T>(this IServiceCollection services, string configSectionPath) where T : class
    {
        return services.AddConfigurationOptions<T>(configSectionPath, false);
    }

    public static IServiceCollection AddRequiredConfigurationOptions<T>(this IServiceCollection services, string configSectionPath) where T : class
    {
        return services.AddConfigurationOptions<T>(configSectionPath, true);
    }

    private static IServiceCollection AddConfigurationOptions<T>(this IServiceCollection services, string configSectionPath, bool validate) where T : class
    {
        var bindConfiguration = services.AddOptions<T>()
             .BindConfiguration(configSectionPath)
             .Validate(options => ValidateRequiredProperties(options, configSectionPath));

        if (validate)
        {
            bindConfiguration.ValidateDataAnnotations()
                           .ValidateOnStart();
        }
        return services;
    }

    private static string DefaultExceptionMessage(string configSectionPath, string propertyName)
    {
        var exceptionMessageProperty = configSectionPath.ToUpperInvariant().Replace(":", "__");

        return $"Define the value inside .env file with __ separator (like '{exceptionMessageProperty}_{propertyName}=<value>')";
    }

    private static bool ValidateRequiredProperties<T>(T options, string configSectionPath) where T : class
    {
        // Get all properties
        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
            // Check if property has the 'required' modifier
            if (property.GetCustomAttributes(typeof(RequiredAttribute), false).Any())
            {
                // Get the value of the property
                var value = property.GetValue(options);

                // For string properties, check if null or empty
                if (property.PropertyType == typeof(string))
                {
                    if (string.IsNullOrWhiteSpace(value as string))
                    {
                        throw new OptionsValidationException(
                            typeof(T).Name,
                            typeof(T),
                            [$"Required property '{configSectionPath}:{property.Name}' is missing or empty",
                             DefaultExceptionMessage(configSectionPath, property.Name)]
                        );
                    }
                }
                // For other types, just check if null
                else if (value == null)
                {
                    throw new OptionsValidationException(
                        typeof(T).Name,
                        typeof(T),
                        [$"Required property '{configSectionPath}:{property.Name}' is missing",
                         DefaultExceptionMessage(configSectionPath, property.Name)]
                    );
                }
            }
        }

        return true;
    }
}