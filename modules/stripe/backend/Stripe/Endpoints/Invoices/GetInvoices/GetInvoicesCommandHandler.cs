using Core.MediatR;
using Stripe.Services;

namespace Stripe.Endpoints.Invoices.GetInvoices;

public sealed class GetInvoicesCommandHandler(
	IInvoicesService invoicesService) : ICommandHandler<GetInvoicesCommand, GetInvoicesResponse>
{
	public async Task<GetInvoicesResponse> Handle(
		GetInvoicesCommand command,
		CancellationToken cancellationToken = default)
	{
		var page = await invoicesService.GetInvoicesForCurrentCustomerAsync(
			command.Limit,
			command.StartingAfter,
			cancellationToken);

		return new GetInvoicesResponse(page.Invoices, page.HasMore, page.NextCursor);
	}
}
