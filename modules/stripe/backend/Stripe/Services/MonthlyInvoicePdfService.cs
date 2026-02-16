using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Stripe.Services;

public sealed class MonthlyInvoicePdfService : IMonthlyInvoicePdfService
{
	private const string CompanyName = "Mappso";
	private const string CompanyAddress = "Vilh. Bergsøes Vej 11";
	private const string CompanyCity = "8210 Aarhus V";
	private const string CompanyCountry = "Denmark";
	private const string CompanyCvr = "42149705";

	// Colors — monochrome palette
	private static readonly string Black = Colors.Black;
	private static readonly string DarkGray = "#333333";
	private static readonly string MediumGray = "#666666";
	private static readonly string LightGray = "#999999";
	private static readonly string VeryLightGray = "#E5E5E5";
	private static readonly string TableHeaderBg = "#F5F5F5";

	public byte[] GenerateMonthlyInvoicePdf(MonthlyInvoiceGroup monthlyGroup, string? userEmail)
	{
		QuestPDF.Settings.License = LicenseType.Community;

		var invoiceNumber = $"INV-{monthlyGroup.YearMonth}";
		var issueDate = DateTime.UtcNow;

		var document = Document.Create(container =>
		{
			container.Page(page =>
			{
				page.Size(PageSizes.A4);
				page.MarginVertical(40);
				page.MarginHorizontal(50);
				page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(DarkGray));

				page.Header().Element(c => ComposeHeader(c, invoiceNumber, issueDate));
				page.Content().Element(c => ComposeContent(c, monthlyGroup, userEmail));
				page.Footer().Element(ComposeFooter);
			});
		});

		return document.GeneratePdf();
	}

	private static void ComposeHeader(IContainer container, string invoiceNumber, DateTime issueDate)
	{
		container.Column(column =>
		{
			column.Item().Row(row =>
			{
				// Left: INVOICE title + number
				row.RelativeItem().Column(left =>
				{
					left.Item().Text("INVOICE")
						.FontSize(28)
						.Bold()
						.FontColor(Black)
						.LetterSpacing(0.05f);

					left.Item().PaddingTop(6).Text(invoiceNumber)
						.FontSize(11)
						.FontColor(MediumGray);

					left.Item().PaddingTop(4).Text($"Issued {issueDate:MMMM d, yyyy}")
						.FontSize(9.5f)
						.FontColor(LightGray);
				});

				// Right: Company details
				row.ConstantItem(200).AlignRight().Column(right =>
				{
					right.Item().Text(CompanyName)
						.FontSize(14)
						.SemiBold()
						.FontColor(Black);

					right.Item().PaddingTop(4).Text(CompanyAddress)
						.FontSize(9)
						.FontColor(MediumGray);

					right.Item().Text(CompanyCity)
						.FontSize(9)
						.FontColor(MediumGray);

					right.Item().Text(CompanyCountry)
						.FontSize(9)
						.FontColor(MediumGray);

					right.Item().PaddingTop(4).Text($"CVR: {CompanyCvr}")
						.FontSize(9)
						.FontColor(MediumGray);
				});
			});

			// Divider
			column.Item().PaddingTop(20).LineHorizontal(1).LineColor(VeryLightGray);
		});
	}

	private static void ComposeContent(
		IContainer container,
		MonthlyInvoiceGroup monthlyGroup,
		string? userEmail)
	{
		container.PaddingTop(20).Column(column =>
		{
			// Bill To + Invoice Meta
			column.Item().Row(row =>
			{
				// Left: Bill To
				row.RelativeItem().Column(billTo =>
				{
					billTo.Item().Text("BILL TO")
						.FontSize(8)
						.Bold()
						.FontColor(LightGray)
						.LetterSpacing(0.1f);

					if (!string.IsNullOrEmpty(userEmail))
					{
						billTo.Item().PaddingTop(4).Text(userEmail)
							.FontSize(10)
							.FontColor(DarkGray);
					}
				});

				// Right: Invoice details
				row.ConstantItem(200).AlignRight().Column(meta =>
				{
					meta.Item().Text("SERVICE PERIOD")
						.FontSize(8)
						.Bold()
						.FontColor(LightGray)
						.LetterSpacing(0.1f);

					meta.Item().PaddingTop(4).Text(monthlyGroup.MonthLabel)
						.FontSize(10)
						.FontColor(DarkGray);
				});
			});

			// Line Items Table
			column.Item().PaddingTop(30).Element(c => ComposeLineItemsTable(c, monthlyGroup));

			// Summary
			column.Item().PaddingTop(16).Element(c => ComposeSummary(c, monthlyGroup));

			// VAT note
			column.Item().PaddingTop(24).Text("Not VAT registered (Ikke momsregistreret)")
				.FontSize(8.5f)
				.FontColor(LightGray);
		});
	}

	private static void ComposeLineItemsTable(IContainer container, MonthlyInvoiceGroup monthlyGroup)
	{
		container.Table(table =>
		{
			table.ColumnsDefinition(columns =>
			{
				columns.RelativeColumn(5); // Description
				columns.RelativeColumn(1.5f); // Amount
			});

			// Header
			table.Header(header =>
			{
				header.Cell()
					.Background(TableHeaderBg)
					.Padding(8)
					.Text("Description")
					.FontSize(8)
					.Bold()
					.FontColor(MediumGray)
					.LetterSpacing(0.05f);

				header.Cell()
					.Background(TableHeaderBg)
					.Padding(8)
					.AlignRight()
					.Text("Amount")
					.FontSize(8)
					.Bold()
					.FontColor(MediumGray)
					.LetterSpacing(0.05f);
			});

			// Body
			foreach (var invoice in monthlyGroup.Invoices)
			{
				var lines = invoice.Lines.ToList();

				// Add balance adjustment line if applicable
				if (invoice.EndingBalance.HasValue && invoice.StartingBalance.HasValue)
				{
					var balanceAdjustment = invoice.StartingBalance.Value - invoice.EndingBalance.Value;
					if (balanceAdjustment != 0)
					{
						lines.Add(new InvoiceLineDto(
							"",
							Amount: new Money(balanceAdjustment, invoice.Total.Currency),
							Description: "Balance Adjustment"
						));
					}
				}

				foreach (var line in lines)
				{
					table.Cell()
						.BorderBottom(1)
						.BorderColor(VeryLightGray)
						.PaddingVertical(8)
						.PaddingHorizontal(8)
						.Text(line.Description ?? "Service")
						.FontSize(9.5f)
						.FontColor(DarkGray);

					table.Cell()
						.BorderBottom(1)
						.BorderColor(VeryLightGray)
						.PaddingVertical(8)
						.PaddingHorizontal(8)
						.AlignRight()
						.Text(FormatMoney(line.Amount))
						.FontSize(9.5f)
						.FontColor(DarkGray);
				}
			}
		});
	}

	private static void ComposeSummary(IContainer container, MonthlyInvoiceGroup monthlyGroup)
	{
		container.AlignRight().Width(200).Column(summary =>
		{
			// Applied balance if present
			if (monthlyGroup.AppliedBalance is { Amount: not 0 })
			{
				summary.Item().Row(row =>
				{
					row.RelativeItem().Text("Applied Balance")
						.FontSize(9.5f)
						.FontColor(MediumGray);
					row.ConstantItem(100).AlignRight().Text(FormatMoney(monthlyGroup.AppliedBalance))
						.FontSize(9.5f)
						.FontColor(MediumGray);
				});

				summary.Item().PaddingVertical(4);
			}

			// Divider before total
			summary.Item().LineHorizontal(1).LineColor(DarkGray);

			// Total
			summary.Item().PaddingTop(8).Row(row =>
			{
				row.RelativeItem().Text("Total")
					.FontSize(12)
					.Bold()
					.FontColor(Black);
				row.ConstantItem(100).AlignRight().Text(FormatMoney(monthlyGroup.MonthTotal))
					.FontSize(12)
					.Bold()
					.FontColor(Black);
			});
		});
	}

	private static void ComposeFooter(IContainer container)
	{
		container.Column(footer =>
		{
			footer.Item().LineHorizontal(0.5f).LineColor(VeryLightGray);

			footer.Item().PaddingTop(8).Row(row =>
			{
				row.RelativeItem().Text($"{CompanyName}  ·  CVR {CompanyCvr}")
					.FontSize(7.5f)
					.FontColor(LightGray);

				row.RelativeItem().AlignRight().Text($"Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
					.FontSize(7.5f)
					.FontColor(LightGray);
			});
		});
	}

	private static string FormatMoney(Money money)
	{
		var amount = money.Amount / 100m;
		var symbol = money.Currency.ToUpperInvariant() switch
		{
			"USD" => "$",
			"EUR" => "\u20ac",
			"GBP" => "\u00a3",
			"DKK" => "kr ",
			"SEK" => "kr ",
			"NOK" => "kr ",
			_ => money.Currency.ToUpperInvariant() + " "
		};

		// For currencies where symbol comes after the number
		return money.Currency.ToUpperInvariant() switch
		{
			"DKK" or "SEK" or "NOK" => $"{amount:N2} {symbol.Trim()}",
			_ => $"{symbol}{amount:N2}"
		};
	}
}
