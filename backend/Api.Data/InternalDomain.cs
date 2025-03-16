namespace Api.Data;

public sealed class InternalDomain : Domain
{
    public InternalDomain()
    {
        Type = DomainType.Internal;
    }

    public required string Subdomain { get; set; } = string.Empty;

    public string Value => $"{Subdomain}.{Server!.HostName}.sites.compozerr.com";
}