namespace Stripe.Helpers;

public static class CurrencyHelper
{
    public static string GetSymbol(string currencyCode)
    {
        return currencyCode.ToUpper() switch
        {
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            "JPY" => "¥",
            "AUD" => "A$",
            "CAD" => "C$",
            "CHF" => "CHF",
            "CNY" => "¥",
            "SEK" => "kr",
            "NZD" => "NZ$",
            _ => currencyCode.ToUpper(), // Fallback to currency code if symbol is unknown
        };
    }
}