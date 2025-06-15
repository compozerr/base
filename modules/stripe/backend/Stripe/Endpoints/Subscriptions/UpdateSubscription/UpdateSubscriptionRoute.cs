using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Stripe.Endpoints.UpdateSubscription;

public static class UpdateSubscriptionRoute
{
	public const string Route = "{subscriptionId}"; // Route with subscription ID parameter

	public static RouteHandlerBuilder AddUpdateSubscriptionRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPut(Route, ExecuteAsync);
	}

	public static Task<UpdateSubscriptionResponse> ExecuteAsync(
		string subscriptionId,
		[FromBody] UpdateSubscriptionCommand command,
		IMediator mediator)
		=> mediator.Send(command with { SubscriptionId = subscriptionId });
}
