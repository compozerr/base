using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;

namespace Stripe.Endpoints.Subscriptions.GetUserSubscriptions;

public static class GetUserSubscriptionsRoute
{
    public const string Route = "user";

    public static RouteHandlerBuilder AddGetUserSubscriptionsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<GetUserSubscriptionsResponse> ExecuteAsync(
        IMediator mediator)
        => mediator.Send(new GetUserSubscriptionsCommand());
}
