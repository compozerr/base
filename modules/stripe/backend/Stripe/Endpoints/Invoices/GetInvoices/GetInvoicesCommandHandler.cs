using Core.MediatR;
using Stripe.Services;

namespace Stripe.Endpoints.PaymentMethods.GetInvoices;

public sealed class GetInvoicesCommandHandler(
	IInvoicesService invoicesService) : ICommandHandler<GetInvoicesCommand, GetInvoicesResponse>
{
	public async Task<GetInvoicesResponse> Handle(GetInvoicesCommand command, CancellationToken cancellationToken = default)
	{
		// Implementation goes here
		return new GetInvoicesResponse();
	}
}
