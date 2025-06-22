using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Stripe.Endpoints.Subscriptions.CancelSubscription;
using Stripe.Endpoints.Subscriptions.GetUserSubscriptions;
using Stripe.Endpoints.Subscriptions.UpsertSubscription;

namespace Stripe.Endpoints.Subscriptions;

public static class SubscriptionsGroup
{
    public const string Route = "subscriptions";

    public static RouteGroupBuilder AddSubscriptionsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddCancelSubscriptionRoute();
        group.AddGetUserSubscriptionsRoute();
        group.AddUpsertSubscriptionRoute();

        return group;
    }
}
