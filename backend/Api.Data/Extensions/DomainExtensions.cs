namespace Api.Data.Extensions;

public static class DomainExtensions
{
    public static Domain? GetPrimary(this ICollection<Domain>? domains)
    {
        if (domains is null) return null;

        var primary = domains.FirstOrDefault(x => x.IsPrimary);

        if (primary is not null) return primary;

        return domains.OrderBy(x => x.Type == DomainType.External)
                     .ThenBy(x => x.ServiceName == "Frontend")
                     .FirstOrDefault();
    }
}