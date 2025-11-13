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
		var invoices = await invoicesService.GetInvoicesForCurrentCustomerAsync(
			cancellationToken);

		// Group invoices by billing period month
		var currentYearMonth = DateTime.UtcNow.ToString("yyyy-MM");

		var monthlyGroups = invoices
			.GroupBy(invoice =>
			{
				var date = DateTimeOffset.FromUnixTimeSeconds(invoice.PeriodStart);
				return date.ToString("yyyy-MM");
			})
			.Select(group =>
			{
				var firstInvoice = group.First();
				var date = DateTimeOffset.FromUnixTimeSeconds(firstInvoice.PeriodStart);
				var yearMonth = group.Key;

				// Calculate total for the month
				var totalAmount = group.Sum(i => i.Total.Amount);
				var currency = firstInvoice.Total.Currency;

				return new MonthlyInvoiceGroup(
					YearMonth: yearMonth,
					MonthLabel: date.ToString("MMMM yyyy"),
					IsOngoing: yearMonth == currentYearMonth,
					MonthTotal: new Money(totalAmount, currency),
					Invoices: group.ToList()
				);
			})
			.OrderByDescending(g => g.YearMonth)
			.ToList();

		return new GetInvoicesResponse(monthlyGroups);
	}
}
