using Core.MediatR;
using Stripe.Services;

namespace Stripe.Endpoints.Invoices.GetInvoices;

public sealed class GetInvoicesCommandHandler(
	IInvoicesService invoicesService) : ICommandHandler<GetInvoicesCommand, GetInvoicesResponse>
{
	public async Task<GetInvoicesResponse> Handle(GetInvoicesCommand command, CancellationToken cancellationToken = default)
	{
		var invoices = await invoicesService.GetInvoicesForCurrentCustomerAsync(
			cancellationToken);

		return new GetInvoicesResponse(invoices);
	}
}
