
using Carter;
using Microsoft.AspNetCore.Routing;
using Stripe.Endpoints.Subscriptions;

namespace Stripe.Endpoints;

public class StripeGroup : CarterModule
{
    public StripeGroup() : base("stripe")
    {
        WithTags(nameof(Stripe));
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddSubscriptionsGroup();
    }
}
