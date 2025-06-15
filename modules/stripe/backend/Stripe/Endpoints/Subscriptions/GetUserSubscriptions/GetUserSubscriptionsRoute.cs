using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;

namespace Stripe.Endpoints.Subscriptions.GetUserSubscriptions;

public static class GetUserSubscriptionsRoute
{
	public const string Route = "user/{userId}";

	public static RouteHandlerBuilder AddGetUserSubscriptionsRoute(this IEndpointRouteBuilder app)
	{
		return app.MapGet(Route, ExecuteAsync);
	}

	public static Task<GetUserSubscriptionsResponse> ExecuteAsync(
		string userId,
		IMediator mediator)
		=> mediator.Send(new GetUserSubscriptionsCommand(userId));
}
