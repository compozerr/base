using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;

namespace Stripe.Endpoints.PaymentMethods.GetInvoices;

public static class GetInvoicesRoute
{
	public const string Route = ""; // Set your route path here

	public static RouteHandlerBuilder AddGetInvoicesRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPost(Route, ExecuteAsync);
	}

	public static Task<GetInvoicesResponse> ExecuteAsync(
		GetInvoicesCommand command,
		IMediator mediator)
		=> mediator.Send(command);
}
