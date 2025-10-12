using Core.MediatR;

namespace Stripe.Endpoints.Invoices.GetInvoices;

public sealed record GetInvoicesCommand : ICommand<GetInvoicesResponse>;
