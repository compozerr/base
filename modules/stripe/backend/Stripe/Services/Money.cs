namespace Stripe.Services;

public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency.ToUpper();
    }

    public override string ToString() => $"{Amount} {Currency}";
};
