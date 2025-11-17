using Auth.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Stripe.Services;

public sealed class MonthlyInvoicePdfService : IMonthlyInvoicePdfService
{
	public byte[] GenerateMonthlyInvoicePdf(MonthlyInvoiceGroup monthlyGroup, string? userEmail)
	{
		QuestPDF.Settings.License = LicenseType.Community;

		var document = Document.Create(container =>
		{
			container.Page(page =>
			{
				page.Size(PageSizes.A4);
				page.Margin(2, Unit.Centimetre);
				page.DefaultTextStyle(x => x.FontSize(11));

				page.Header().Column(column =>
				{
					column.Item().Text("MONTHLY INVOICE")
						.FontSize(24)
						.Bold()
						.FontColor(Colors.Blue.Darken2);

					column.Item().PaddingTop(10).Text(monthlyGroup.MonthLabel)
						.FontSize(16)
						.SemiBold();

					column.Item().PaddingTop(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten1);
				});

				page.Content().PaddingTop(20).Column(column =>
				{
					// Customer Information
					if (!string.IsNullOrEmpty(userEmail))
					{
						column.Item().Text("Bill To:")
							.FontSize(12)
							.SemiBold();

						column.Item().PaddingTop(5).Text(userEmail)
							.FontSize(11);
					}

					column.Item().PaddingTop(5).Text($"Invoice Period: {monthlyGroup.MonthLabel}")
						.FontSize(10)
						.Italic();

					column.Item().PaddingTop(20).Text("Services")
						.FontSize(14)
						.SemiBold();

					// Line Items Table
					column.Item().PaddingTop(10).Table(table =>
					{
						table.ColumnsDefinition(columns =>
						{
							columns.RelativeColumn(4); // Description
							columns.RelativeColumn(1); // Amount
						});

						// Header
						table.Header(header =>
						{
							header.Cell().Background(Colors.Grey.Lighten3)
								.Padding(5)
								.Text("Description")
								.SemiBold();

							header.Cell().Background(Colors.Grey.Lighten3)
								.Padding(5)
								.AlignRight()
								.Text("Amount")
								.SemiBold();
						});

						// Body - All line items from all invoices
						foreach (var invoice in monthlyGroup.Invoices)
						{

							var lines = invoice.Lines;
							if (invoice.EndingBalance.HasValue && invoice.StartingBalance.HasValue)
							{
								var balanceAdjustment = invoice.StartingBalance.Value - invoice.EndingBalance.Value;
								if (balanceAdjustment != 0)
								{
									var balanceLine = new InvoiceLineDto(
										"",
										Amount: new Money(balanceAdjustment, invoice.Total.Currency),
										Description: "Balance Adjustment"
									);

									lines = lines.Append(balanceLine).ToList();
								}
							}

							foreach (var line in lines)
							{
								table.Cell()
									.BorderBottom(1)
									.BorderColor(Colors.Grey.Lighten2)
									.Padding(5)
									.Text(line.Description ?? "Service");

								table.Cell()
									.BorderBottom(1)
									.BorderColor(Colors.Grey.Lighten2)
									.Padding(5)
									.AlignRight()
									.Text(FormatMoney(line.Amount));
							}
						}
					});

					// Summary Section
					column.Item().PaddingTop(20).AlignRight().Column(summaryColumn =>
					{
						summaryColumn.Item().Row(row =>
						{
							row.RelativeItem().Text("Total:");
							row.RelativeItem().AlignRight()
								.Text(FormatMoney(monthlyGroup.MonthTotal))
								.FontSize(14)
								.SemiBold();
						});
					});

					// Footer
					column.Item().PaddingTop(30).BorderTop(1).BorderColor(Colors.Grey.Lighten1);

					column.Item().PaddingTop(10).Text("Thank you for your business!")
						.FontSize(10)
						.Italic()
						.FontColor(Colors.Grey.Darken1);

					column.Item().PaddingTop(5).Text($"Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
						.FontSize(8)
						.FontColor(Colors.Grey.Medium);
				});
			});
		});

		return document.GeneratePdf();
	}

	private static string FormatMoney(Money money)
	{
		var amount = money.Amount / 100m; // Convert from cents
		return $"{amount:0.00} {money.Currency.ToUpperInvariant()}";
	}
}
