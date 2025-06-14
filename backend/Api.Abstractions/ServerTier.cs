namespace Api.Abstractions;

public sealed record ServerTier(
    ServerTierId Id,
    decimal RamGb,
    decimal Cores,
    decimal DiskGb,
    Price Price,
    string? PromotionalText
);
