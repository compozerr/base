namespace Base.Core;

public static class Core_DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    => services.AddEndpointsApiExplorer()
               .AddSwaggerGen()
               .AddCors(new Action<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>(options =>
                {
                    options.AddDefaultPolicy(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
                }));
}