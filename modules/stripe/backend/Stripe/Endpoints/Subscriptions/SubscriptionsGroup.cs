using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Stripe.Endpoints.Subscriptions.CreateSubscription;
using Stripe.Endpoints.UpdateSubscription;
using Stripe.Endpoints.Subscriptions.CancelSubscription;

namespace Stripe.Endpoints.Subscriptions;

public static class SubscriptionsGroup
{
    public const string Route = "subscriptions";

    public static RouteGroupBuilder AddSubscriptionsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddCreateSubscriptionRoute();
        group.AddUpdateSubscriptionRoute();
        group.AddCancelSubscriptionRoute();

        return group;
    }
}
