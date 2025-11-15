namespace Stripe.Services;

public sealed record InvoiceDto(
    string Id,
    Money Total,
    List<InvoiceLineDto> Lines,
    string? HostedInvoiceUrl,
    string? InvoicePdf,
    long PeriodStart,
    long PeriodEnd,
    long Created,
    long? StartingBalance,
    long? EndingBalance)
{
    public static InvoiceDto FromInvoice(Invoice invoice)
    {
        return new InvoiceDto(
            Id: invoice.Id,
            Total: new Money(invoice.AmountPaid, invoice.Currency),
            Lines: [.. invoice.Lines.Select(InvoiceLineDto.FromInvoiceLineItem)],
            HostedInvoiceUrl: invoice.HostedInvoiceUrl,
            InvoicePdf: invoice.InvoicePdf,
            PeriodStart: ((DateTimeOffset)invoice.PeriodStart).ToUnixTimeSeconds(),
            PeriodEnd: ((DateTimeOffset)invoice.PeriodEnd).ToUnixTimeSeconds(),
            Created: ((DateTimeOffset)invoice.Created).ToUnixTimeSeconds(),
            StartingBalance: invoice.StartingBalance,
            EndingBalance: invoice.EndingBalance);
    }
}
