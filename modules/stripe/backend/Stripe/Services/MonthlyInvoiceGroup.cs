namespace Stripe.Services;

public sealed record MonthlyInvoiceGroup(
    string YearMonth,          // e.g., "2024-11"
    string MonthLabel,         // e.g., "November 2024"
    bool IsOngoing,            // true if current month
    Money MonthTotal,          // Total for all invoices in this month
    List<InvoiceDto> Invoices); // All invoices for this month
