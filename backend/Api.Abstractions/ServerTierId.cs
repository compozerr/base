namespace Api.Abstractions;

public sealed record ServerTierId
{
    public string Value { get; init; }

    public ServerTierId(string value)
    {
        Value = value.ToUpperInvariant();
    }
};
