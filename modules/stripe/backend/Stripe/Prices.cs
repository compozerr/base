namespace Stripe;

public enum PricesEnvironment
{
    Sandbox,
    Production
}

public static class Prices
{
    private static readonly Dictionary<string, Dictionary<PricesEnvironment, string>> PricesMap = new()
    {
        { "T0", new Dictionary<PricesEnvironment, string> {
            [PricesEnvironment.Sandbox] = "price_1RaEqrPw8PYQFLLYbsQgaLbq",
            [PricesEnvironment.Production] = "price_1RhW6qLbalvevoyGSTDRTcQg"
        }},
        { "T1", new Dictionary<PricesEnvironment, string> {
            [PricesEnvironment.Sandbox] = "price_1RXJ9PPw8PYQFLLYI5CPzexw",
            [PricesEnvironment.Production] = "price_1RhW6zLbalvevoyGDGcvt52z"
        }},
        { "T2", new Dictionary<PricesEnvironment, string> {
            [PricesEnvironment.Sandbox] = "price_1RXJ9jPw8PYQFLLYFry6auZy",
            [PricesEnvironment.Production] = "price_1RhW6xLbalvevoyGGWvp1URj"
        }},
        { "T3", new Dictionary<PricesEnvironment, string> {
            [PricesEnvironment.Sandbox] = "price_1RXJA1Pw8PYQFLLYcaYQrdIk",
            [PricesEnvironment.Production] = "price_1RhW6vLbalvevoyGkLWsUqSG"
        }},
        { "T4", new Dictionary<PricesEnvironment, string> {
            [PricesEnvironment.Sandbox] = "price_1RXJAQPw8PYQFLLYMPLCOLVB",
            [PricesEnvironment.Production] = "price_1RhW6uLbalvevoyGTzRSQRVv"
        }},
    };

    public static string GetPriceId(string internalId, bool isProduction = false)
    {
        var environment = isProduction ? PricesEnvironment.Production : PricesEnvironment.Sandbox;

        if (PricesMap.TryGetValue(internalId, out var priceIds) && priceIds.TryGetValue(environment, out var priceId))
        {
            return priceId;
        }

        throw new ArgumentException($"Price ID for tier {internalId} in environment {environment} not found.");
    }

    public static string GetInternalId(string priceId, bool isProduction = false)
    {
        var environment = isProduction ? PricesEnvironment.Production : PricesEnvironment.Sandbox;

        var internalId = PricesMap.FirstOrDefault(x => x.Value[environment] == priceId).Key;
        if (internalId != null)
        {
            return internalId;
        }

        throw new ArgumentException($"Internal ID for product {priceId} not found.");
    }
}