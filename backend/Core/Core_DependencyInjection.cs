using Base.Helpers;

namespace Base.Core;

public static class Core_DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    => services.AddWebApiConfig();

    public static IServiceCollection AddWebApiConfig(this IServiceCollection services)
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
        });


        return services;
    }
}