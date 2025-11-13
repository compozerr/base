using Core.MediatR;

namespace Stripe.Endpoints.Invoices.DownloadMonthlyInvoice;

public sealed record DownloadMonthlyInvoiceCommand(
	string YearMonth) : ICommand<byte[]>;
