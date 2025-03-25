namespace Api.Data;

public sealed class InternalDomain : Domain
{
    public InternalDomain()
    {
        Type = DomainType.Internal;
    }

    public required string Subdomain { get; set; } = string.Empty;

    public string Value => $"{Subdomain}.sites.{Project?.Server?.HostName}.compozerr.com";

    public override string GetValue => Value;
}