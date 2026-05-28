namespace Api.Abstractions;

public sealed record AllocatedServerSummary(
    ServerId Id,
    string HostName,
    string? LocationIsoCountryCode);
