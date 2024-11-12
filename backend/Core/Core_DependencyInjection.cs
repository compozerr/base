using Carter.OpenApi;
using Core.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

public static class Core_DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    => services.AddWebApiConfig();

    private static IServiceCollection AddWebApiConfig(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(name: AppConstants.CorsPolicy,
                builder => { builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); });
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "conpozerr base", Version = "v1" });

            c.DocInclusionPredicate((s, description) =>
            {
                foreach (var metaData in description.ActionDescriptor.EndpointMetadata)
                {
                    if (metaData is IIncludeOpenApi)
                    {
                        return true;
                    }
                }
                return false;
            });
        });


        return services;
    }
}