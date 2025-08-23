using Core.MediatR;

namespace Stripe.Endpoints.PaymentMethods.GetInvoices;

public sealed record GetInvoicesCommand(
	string Property1) : ICommand<GetInvoicesResponse>;
