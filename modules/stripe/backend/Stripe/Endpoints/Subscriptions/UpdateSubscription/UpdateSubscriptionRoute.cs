using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;

namespace Stripe.Endpoints.UpdateSubscription;

public static class UpdateSubscriptionRoute
{
	public const string Route = ""; // Set your route path here

	public static RouteHandlerBuilder AddUpdateSubscriptionRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPut(Route, ExecuteAsync);
	}

	public static Task<UpdateSubscriptionResponse> ExecuteAsync(
		UpdateSubscriptionCommand command,
		IMediator mediator)
		=> mediator.Send(command);
}
