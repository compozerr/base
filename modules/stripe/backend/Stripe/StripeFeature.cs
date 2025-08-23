using Core.Extensions;
using Core.Feature;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Stripe.Data;
using Stripe.Data.Repositories;
using Stripe.Options;
using Stripe.Services;

namespace Stripe;

public class StripeFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<StripeDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), b =>
            {
                b.MigrationsAssembly(typeof(StripeDbContext).Assembly.FullName);
            });
        });

        services.AddRequiredConfigurationOptions<StripeOptions>("Stripe");
        services.AddScoped<IStripeService, StripeService>();

        services.AddScoped<ICurrentStripeCustomerIdAccessor, CurrentStripeCustomerIdAccessor>();

        services.AddScoped<IStripeCustomerRepository, StripeCustomerRepository>();
        services.AddScoped<IPaymentMethodsService, PaymentMethodsService>();
        services.AddScoped<ISubscriptionService, Services.SubscriptionService>();
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var stripeOptions = scope.ServiceProvider.GetRequiredService<IOptions<StripeOptions>>().Value;

        StripeConfiguration.ApiKey = stripeOptions.ApiKey;

        var context = scope.ServiceProvider.GetRequiredService<StripeDbContext>();

        context.Database.Migrate();
    }
}