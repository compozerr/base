namespace Stripe.Services;

public sealed record InvoiceDto(
    string Id,
    Money Total,
    List<InvoiceLineDto> Lines)
{
    public static InvoiceDto FromInvoice(Invoice invoice)
    {
        return new InvoiceDto(
            Id: invoice.Id,
            Total: new Money(invoice.AmountPaid, invoice.Currency),
            Lines: [.. invoice.Lines.Select(
                InvoiceLineDto.FromInvoiceLineItem)]);
    }
}
