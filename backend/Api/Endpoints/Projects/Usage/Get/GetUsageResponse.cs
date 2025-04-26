namespace Api.Endpoints.Projects.Usage.Get;

public enum UsagePointType
{
    CPU,
    Ram,
    Disk,
    Network,
}

public sealed record UsagePoint(
    DateTime Timestamp,
    decimal Value);

public sealed record GetUsageResponse(
    Dictionary<UsagePointType, List<UsagePoint>> Points,
    UsageSpan UsageSpan);