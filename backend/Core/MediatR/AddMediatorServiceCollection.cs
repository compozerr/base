using Microsoft.Extensions.DependencyInjection;

namespace Core.MediatR;

public static class AddMediatorServiceCollection
{
    public static IServiceCollection UseMediatR(this IServiceCollection services)
        => services.AddMediatR(c =>
                                {
                                    c.Lifetime = ServiceLifetime.Scoped;
                                    c.RegisterServicesFromAssemblyContaining(typeof(ServiceCollectionExtensions));
                                });
}