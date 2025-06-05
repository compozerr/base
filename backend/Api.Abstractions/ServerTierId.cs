namespace Api.Abstractions;

public sealed record ServerTierId
{
    public string Value;

    public ServerTierId(string value)
    {
        Value = value.ToUpperInvariant();
    }
};
