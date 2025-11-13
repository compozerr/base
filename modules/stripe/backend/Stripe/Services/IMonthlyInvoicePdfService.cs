namespace Stripe.Services;

public interface IMonthlyInvoicePdfService
{
	byte[] GenerateMonthlyInvoicePdf(MonthlyInvoiceGroup monthlyGroup, string? userEmail);
}
