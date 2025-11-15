using Auth.Services;
using Core.MediatR;
using Stripe.Services;

namespace Stripe.Endpoints.Invoices.DownloadMonthlyInvoice;

public sealed class DownloadMonthlyInvoiceCommandHandler(
	IInvoicesService invoicesService,
	IMonthlyInvoicePdfService pdfService,
	ICurrentUserAccessor currentUserAccessor) : ICommandHandler<DownloadMonthlyInvoiceCommand, byte[]>
{
	public async Task<byte[]> Handle(
		DownloadMonthlyInvoiceCommand command,
		CancellationToken cancellationToken = default)
	{
		// Validate that the requested month is not the current month (ongoing)
		var currentYearMonth = DateTime.UtcNow.ToString("yyyy-MM");
		if (command.YearMonth == currentYearMonth)
		{
			throw new InvalidOperationException("Cannot download invoice for the current month. Please wait until the month ends.");
		}

		// Get all invoices for the customer
		var invoices = await invoicesService.GetInvoicesForCurrentCustomerAsync(cancellationToken);

		// Filter invoices for the requested month
		var monthInvoices = invoices
			.Where(invoice =>
			{
				var date = DateTimeOffset.FromUnixTimeSeconds(invoice.PeriodStart);
				return date.ToString("yyyy-MM") == command.YearMonth;
			})
			.ToList();

		if (monthInvoices.Count == 0)
		{
			throw new InvalidOperationException($"No invoices found for {command.YearMonth}");
		}

		// Create the monthly group
		var firstInvoice = monthInvoices.First();
		var date = DateTimeOffset.FromUnixTimeSeconds(firstInvoice.PeriodStart);
		var totalAmount = monthInvoices.Sum(i => i.Total.Amount);
		var currency = firstInvoice.Total.Currency;

		// Calculate applied balance for the month
		var totalAppliedBalance = monthInvoices
			.Where(i => i.StartingBalance.HasValue && i.EndingBalance.HasValue)
			.Sum(i => i.StartingBalance!.Value - i.EndingBalance!.Value);

		var appliedBalance = totalAppliedBalance != 0
			? new Money(totalAppliedBalance, currency)
			: null;

		var monthlyGroup = new MonthlyInvoiceGroup(
			YearMonth: command.YearMonth,
			MonthLabel: date.ToString("MMMM yyyy"),
			IsOngoing: false,
			MonthTotal: new Money(totalAmount, currency),
			Invoices: monthInvoices,
			AppliedBalance: appliedBalance
		);

		// Get user email
		var userEmail = currentUserAccessor.CurrentUserEmail;

		// Generate PDF
		var pdfBytes = pdfService.GenerateMonthlyInvoicePdf(monthlyGroup, userEmail?.Address);

		return pdfBytes;
	}
}
