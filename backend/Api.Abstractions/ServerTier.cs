namespace Api.Abstractions;

public sealed record ServerTier(
    ServerTierId Id,
    decimal RamGb,
    int Cores,
    decimal DiskGb,
    Price Price
);
