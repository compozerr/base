namespace Api.Data.Extensions;

public static class DomainExtensions
{
    public static Domain? GetPrimary(this ICollection<Domain>? domains)
    {
        if (domains is null) return null;

        return domains.FirstOrDefault(x => x.IsPrimary && x.ServiceName == "Frontend") ?? domains
                      .FirstOrDefault(x => x.ServiceName == "Frontend") ?? domains
                      .FirstOrDefault();
    }
}