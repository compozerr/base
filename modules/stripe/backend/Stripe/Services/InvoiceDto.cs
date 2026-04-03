namespace Stripe.Services;

public sealed record InvoiceDto(
    string Id,
    Money Total,
    string Status,
    string? HostedInvoiceUrl,
    string? InvoicePdf,
    long PeriodStart,
    long PeriodEnd,
    long Created)
{
    public static InvoiceDto FromInvoice(Invoice invoice)
    {
        return new InvoiceDto(
            Id: invoice.Id,
            Total: new Money(invoice.AmountPaid, invoice.Currency),
            Status: invoice.Status ?? "unknown",
            HostedInvoiceUrl: invoice.HostedInvoiceUrl,
            InvoicePdf: invoice.InvoicePdf,
            PeriodStart: ((DateTimeOffset)invoice.PeriodStart).ToUnixTimeSeconds(),
            PeriodEnd: ((DateTimeOffset)invoice.PeriodEnd).ToUnixTimeSeconds(),
            Created: ((DateTimeOffset)invoice.Created).ToUnixTimeSeconds());
    }
}
