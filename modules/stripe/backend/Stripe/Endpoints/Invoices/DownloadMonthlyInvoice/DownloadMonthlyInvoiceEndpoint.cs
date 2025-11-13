using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Stripe.Endpoints.Invoices.DownloadMonthlyInvoice;

public static class DownloadMonthlyInvoiceEndpoint
{
	public static RouteGroupBuilder AddDownloadMonthlyInvoiceRoute(this RouteGroupBuilder group)
	{
		group.MapPost("/monthly/{yearMonth}/download", async (
			string yearMonth,
			IMediator mediator) =>
		{
			var command = new DownloadMonthlyInvoiceCommand(yearMonth);
			var pdfBytes = await mediator.Send(command);

			return Results.File(
				pdfBytes,
				contentType: "application/pdf",
				fileDownloadName: $"invoice-{yearMonth}.pdf");
		})
		.WithTags("Invoices")
		.WithName("DownloadMonthlyInvoice")
		.Produces<byte[]>(StatusCodes.Status200OK, contentType: "application/pdf")
		.Produces(StatusCodes.Status400BadRequest);

		return group;
	}
}
