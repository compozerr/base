namespace Api.Data.Extensions;

public static class DomainExtensions
{
    public static Domain? GetPrimary(this ICollection<Domain>? domains)
    {
        if (domains is null) return null;

        return domains.OrderBy(x => x.Type == DomainType.External)
                     .ThenBy(x => x.IsPrimary)
                     .ThenBy(x => x.ServiceName == "Frontend")
                     .FirstOrDefault();
    }
}