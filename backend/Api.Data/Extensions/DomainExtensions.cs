namespace Api.Data.Extensions;

public static class DomainExtensions
{
    // Common multi-part TLDs for apex domain detection
    private static readonly string[] MultiPartTlds = ["co.uk", "com.au", "co.nz", "com.br", "co.jp", "co.kr", "com.cn", "org.uk", "net.au"];

    public static Domain? GetPrimary(this ICollection<Domain>? domains)
    {
        if (domains is null) return null;

        var primary = domains.FirstOrDefault(x => x.IsPrimary);

        if (primary is not null) return primary;

        return domains.OrderByDescending(x => x.Type == DomainType.External)
                     .ThenByDescending(x => x.ServiceName.Equals("Frontend", StringComparison.OrdinalIgnoreCase))
                     .FirstOrDefault();
    }

    /// <summary>
    /// Determines if a domain is an apex/root domain (e.g., example.com)
    /// as opposed to a subdomain (e.g., www.example.com).
    /// Apex domains require A records; subdomains can use CNAME records.
    /// </summary>
    public static bool IsApexDomain(string domainName)
    {
        var cleanDomain = domainName.TrimEnd('.');
        var parts = cleanDomain.Split('.');

        // Check if domain ends with a multi-part TLD
        foreach (var tld in MultiPartTlds)
        {
            if (cleanDomain.EndsWith($".{tld}", StringComparison.OrdinalIgnoreCase) ||
                cleanDomain.Equals(tld, StringComparison.OrdinalIgnoreCase))
            {
                // For multi-part TLDs, apex has 3 parts (e.g., example.co.uk)
                return parts.Length <= 3;
            }
        }

        // For standard TLDs, apex has 2 parts (e.g., example.com)
        return parts.Length <= 2;
    }
}