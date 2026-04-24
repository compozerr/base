using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;

namespace Stripe.Endpoints.Invoices.GetInvoices;

public static class GetInvoicesRoute
{
	public const string Route = "";

	public static RouteHandlerBuilder AddGetInvoicesRoute(this IEndpointRouteBuilder app)
	{
		return app.MapGet(Route, ExecuteAsync);
	}

	public static Task<GetInvoicesResponse> ExecuteAsync(
		IMediator mediator,
		int limit = 20,
		string? startingAfter = null)
		=> mediator.Send(
			new GetInvoicesCommand(limit, startingAfter));
}
