namespace Stripe;

public static class Prices
{
    private static readonly Dictionary<string, string> PricesMap = new()
    {
        { "T0", "price_1RaEqrPw8PYQFLLYbsQgaLbq" },
        { "T1", "price_1RXJ9PPw8PYQFLLYI5CPzexw" },
        { "T2", "price_1RXJ9jPw8PYQFLLYFry6auZy" },
        { "T3", "price_1RXJA1Pw8PYQFLLYcaYQrdIk" },
        { "T4", "price_1RXJAQPw8PYQFLLYMPLCOLVB" },
    };

    public static string GetPriceId(string internalId)
    {
        if (PricesMap.TryGetValue(internalId, out var priceId))
        {
            return priceId;
        }

        throw new ArgumentException($"Price ID for tier {internalId} not found.");
    }

    public static string GetInternalId(string priceId)
    {
        var internalId = PricesMap.FirstOrDefault(x => x.Value == priceId).Key;
        if (internalId != null)
        {
            return internalId;
        }

        throw new ArgumentException($"Internal ID for product {priceId} not found.");
    }
}