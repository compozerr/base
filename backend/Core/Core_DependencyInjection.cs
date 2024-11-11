namespace Base.Core;

public static class Core_DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    => services.AddEndpointsApiExplorer()
               .AddSwaggerGen();
}