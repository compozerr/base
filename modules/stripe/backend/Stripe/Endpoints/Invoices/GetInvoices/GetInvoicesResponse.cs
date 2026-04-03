using Stripe.Services;

namespace Stripe.Endpoints.Invoices.GetInvoices;

public sealed record GetInvoicesResponse(
    List<InvoiceDto> Invoices);
