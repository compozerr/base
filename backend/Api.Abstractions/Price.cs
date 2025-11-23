namespace Api.Abstractions;

public sealed record Price(
    decimal Value,
    string Currency,
    decimal? OriginalAmount = null)
{
    public bool IsDiscounted => OriginalAmount.HasValue && OriginalAmount.Value > Value;
};