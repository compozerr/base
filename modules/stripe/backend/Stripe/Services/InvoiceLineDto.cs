namespace Stripe.Services;

public sealed record InvoiceLineDto(
    string Id,
    Money Amount,
    string Description)
{
    public static InvoiceLineDto FromInvoiceLineItem(InvoiceLineItem line)
    {
        var discountedPrice = line.Amount - line.DiscountAmounts.Select(x => x.Amount).Sum();

        return new InvoiceLineDto(
            Id: line.Id,
            Amount: new Money(discountedPrice, line.Currency, line.Amount),
            Description: GetDescription(line));
    }

    private static string GetDescription(InvoiceLineItem line)
    {
        var stringBuilder = new System.Text.StringBuilder();

        var quantity = line.Quantity ?? 1;
        var unitPrice = line.Amount / quantity / 100m;
        var subTotal = unitPrice * quantity;

        var total = subTotal;
        var currency = GetCurrencySymbol(line.Currency);

        stringBuilder.AppendLine($"{quantity} x ({currency}{unitPrice}");

        var discounts = line.DiscountAmounts.Select(x => x.Amount).Sum() / 100m;
        if (discounts > 0)
        {
            total -= discounts;
            stringBuilder.AppendLine($" - {currency}{discounts} discount)");
        }
        else
        {
            stringBuilder.AppendLine(")");
        }

        stringBuilder.AppendLine($" = {currency}{total}/month");

        return stringBuilder.ToString();
    }

    private static string GetCurrencySymbol(string currency)
    {
        return currency.ToUpper() switch
        {
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            "JPY" => "¥",
            _ => currency.ToUpper()
        };
    }
}
