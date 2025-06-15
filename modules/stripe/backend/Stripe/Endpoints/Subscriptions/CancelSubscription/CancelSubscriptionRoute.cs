using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Stripe.Endpoints.Subscriptions.CancelSubscription;

public static class CancelSubscriptionRoute
{
    public const string Route = "{subscriptionId}/cancel";

    public static RouteHandlerBuilder AddCancelSubscriptionRoute(this IEndpointRouteBuilder app)
    {
        return app.MapDelete(Route, ExecuteAsync);
    }

    public static Task<CancelSubscriptionResponse> ExecuteAsync(
        string subscriptionId,
        [FromBody] CancelSubscriptionCommand command,
        IMediator mediator)
        => mediator.Send(command with { SubscriptionId = subscriptionId });
}
