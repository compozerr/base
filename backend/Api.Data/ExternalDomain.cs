namespace Api.Data;

public sealed class ExternalDomain : Domain
{
    public ExternalDomain()
    {
        Type = DomainType.External;
    }

    public required string Value { get; set; }
    public required bool IsVerified { get; set; } = false;

    public override string GetValue => Value;
}