namespace Api.Endpoints.Projects.Usage.Get;

public enum UsagePointType
{
    CPU,
    Ram,
    DiskWrite,
    DiskRead,
    NetworkIn,
    NetworkOut,
}

public sealed record UsagePoint(
    DateTime Timestamp,
    decimal Value);

public sealed record GetUsageResponse(
    Dictionary<UsagePointType, List<UsagePoint>> Points,
    UsageSpan UsageSpan,
    decimal AllocatedMemoryGb);