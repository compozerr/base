namespace Stripe.Services;

public sealed record InvoiceLineDto(
    string Id,
    Money Amount,
    string Description)
{
    public static InvoiceLineDto FromInvoiceLineItem(InvoiceLineItem line)
    {
        return new InvoiceLineDto(
            Id: line.Id,
            Amount: new Money(line.Amount, line.Currency),
            Description: line.Description ?? "");
    }
}
