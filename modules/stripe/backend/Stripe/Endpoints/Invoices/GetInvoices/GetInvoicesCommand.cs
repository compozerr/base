using Core.MediatR;

namespace Stripe.Endpoints.Invoices.GetInvoices;

public sealed record GetInvoicesCommand(
    int Limit = 20,
    string? StartingAfter = null) : ICommand<GetInvoicesResponse>;
