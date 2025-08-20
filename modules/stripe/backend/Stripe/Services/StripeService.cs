using Api.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Stripe.Endpoints.Subscriptions.GetUserSubscriptions;
using Stripe.Events;
using Stripe.Options;

namespace Stripe.Services;

public class StripeService : IStripeService
{
    private readonly StripeOptions _options;
    private readonly StripeClient _stripeClient;
    private readonly ICurrentStripeCustomerIdAccessor _currentStripeCustomerIdAccessor;

    private readonly bool _isProduction;

    public StripeService(
        IOptions<StripeOptions> options,
        IWebHostEnvironment environment,
        ICurrentStripeCustomerIdAccessor currentStripeCustomerIdAccessor)
    {
        _options = options.Value;
        _stripeClient = new StripeClient(_options.ApiKey);
        _currentStripeCustomerIdAccessor = currentStripeCustomerIdAccessor;
        _isProduction = environment.IsProduction();
    }

    



    
}
