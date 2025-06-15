using Core.Extensions;
using Core.Feature;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Stripe.Options;
using Stripe.Services;

namespace Stripe;

public class StripeFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddRequiredConfigurationOptions<StripeOptions>("Stripe");
        services.AddScoped<IStripeService, StripeService>();
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var stripeOptions = scope.ServiceProvider.GetRequiredService<IOptions<StripeOptions>>().Value;

        StripeConfiguration.ApiKey = stripeOptions.ApiKey;
    }
}