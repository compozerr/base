namespace Stripe.Services;

public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }
    public decimal? OriginalAmount { get; }

    public Money(
        decimal amount,
        string currency,
        decimal? originalAmount = null)
    {
        Amount = amount;
        Currency = currency.ToUpper();
        OriginalAmount = originalAmount;
    }

    public bool IsDiscounted => OriginalAmount.HasValue && OriginalAmount.Value > Amount;

    public override string ToString() => $"{Amount} {Currency}";
};
