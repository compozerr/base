using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;

namespace Stripe.Endpoints.Subscriptions.CreateSubscription;

public static class CreateSubscriptionRoute
{
	public const string Route = ""; // Root of the subscriptions route

	public static RouteHandlerBuilder AddCreateSubscriptionRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPost(Route, ExecuteAsync);
	}

	public static Task<CreateSubscriptionResponse> ExecuteAsync(
		CreateSubscriptionCommand command,
		IMediator mediator)
		=> mediator.Send(command);
}
