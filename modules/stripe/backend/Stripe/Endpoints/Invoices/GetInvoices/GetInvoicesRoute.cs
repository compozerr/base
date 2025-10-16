using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using MediatR;

namespace Stripe.Endpoints.Invoices.GetInvoices;

public static class GetInvoicesRoute
{
	public const string Route = "";

	public static RouteHandlerBuilder AddGetInvoicesRoute(this IEndpointRouteBuilder app)
	{
		return app.MapPost(Route, ExecuteAsync);
	}

	public static Task<GetInvoicesResponse> ExecuteAsync(
		GetInvoicesCommand command,
		IMediator mediator)
		=> mediator.Send(command);
}
