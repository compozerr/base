using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;

namespace Stripe.Endpoints.PaymentMethods.GetUserPaymentMethods;

public static class GetUserPaymentMethodsRoute
{
	public const string Route = "user";

	public static RouteHandlerBuilder AddGetUserPaymentMethodsRoute(this IEndpointRouteBuilder app)
	{
		return app.MapGet(Route, ExecuteAsync);
	}

	public static Task<GetUserPaymentMethodsResponse> ExecuteAsync(
		IMediator mediator)
		=> mediator.Send(new GetUserPaymentMethodsCommand());
}
