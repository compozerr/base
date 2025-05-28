using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.Extensions;

public static class ServiceCollectionExtensions
{
    private static readonly List<(Type Type, string ConfigPath)> _trackedOptions = [];
    
    public static IServiceCollection AddConfigurationOptions<T>(this IServiceCollection services, string configSectionPath) where T : class
    {
        return services.AddConfigurationOptions<T>(configSectionPath, false);
    }

    public static IServiceCollection AddRequiredConfigurationOptions<T>(this IServiceCollection services, string configSectionPath) where T : class
    {
        return services.AddConfigurationOptions<T>(configSectionPath, true);
    }

    public static IServiceCollection ValidateAllConfiguration(this IServiceCollection services)
    {
        var tempServiceProvider = services.BuildServiceProvider();
        var configuration = tempServiceProvider.GetRequiredService<IConfiguration>();
        
        foreach (var (type, configPath) in _trackedOptions)
        {
            var options = Activator.CreateInstance(type);
            configuration.GetSection(configPath).Bind(options);
            
            var method = typeof(ServiceCollectionExtensions)
                .GetMethod(nameof(ValidateRequiredProperties), BindingFlags.NonPublic | BindingFlags.Static)
                ?.MakeGenericMethod(type);
            
            method?.Invoke(null, [options, configPath]);
        }
        
        tempServiceProvider.Dispose();
        return services;
    }

    private static IServiceCollection AddConfigurationOptions<T>(this IServiceCollection services, string configSectionPath, bool validate) where T : class
    {
        var bindConfiguration = services.AddOptions<T>()
             .BindConfiguration(configSectionPath)
             .Validate(options => ValidateRequiredProperties(options, configSectionPath));

        if (validate)
        {
            _trackedOptions.Add((typeof(T), configSectionPath));
            
            bindConfiguration.ValidateDataAnnotations()
                           .ValidateOnStart();
        }
        return services;
    }

    private static string DefaultExceptionMessage(string configSectionPath, string propertyName)
    {
        var exceptionMessageProperty = configSectionPath.ToUpperInvariant().Replace(":", "__");

        return $"Define the value inside .env file with __ separator (like '{exceptionMessageProperty}__{ConvertToUppsercaseAndDelimiters(propertyName)}=<value>')";
    }

    private static string ConvertToUppsercaseAndDelimiters(string propertyName)
    {
        var result = propertyName;
        
        // Insert underscore before uppercase letters (for PascalCase)
        for (int i = result.Length - 2; i >= 0; i--)
        {
            if (char.IsUpper(result[i + 1]) && char.IsLetter(result[i]) && !char.IsUpper(result[i]))
            {
            result = result.Insert(i + 1, "_");
            }
            else if (i > 0 && char.IsUpper(result[i]) && char.IsUpper(result[i - 1]) && 
                 i + 1 < result.Length && char.IsLower(result[i + 1]))
            {
            result = result.Insert(i, "_");
            }
        }
        
        // Replace dots with underscore
        result = result.Replace(".", "_");
        
        // Replace colon with double underscore
        result = result.Replace(":", "__");
        
        return result.ToUpperInvariant();
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