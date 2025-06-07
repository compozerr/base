using Core.Feature;
using Microsoft.Extensions.DependencyInjection;

namespace Stripe;

public class StripeFeature : IFeature
{

    void IFeature.ConfigureServices(IServiceCollection services)
    {
    }
}