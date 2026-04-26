using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Hosting.Services;

// Parallel to Api.Data.EnvironmentHealthStatus — this one is the hosting-API wire shape
// (lowercase strings from the Node manager), the other is the domain/DB enum.
[JsonConverter(typeof(LowercaseEnvHealthStatusConverter))]
public enum EnvironmentHealthReportStatus
{
    Unknown,
    Healthy,
    Unhealthy,
}

// CamelCase on single-word members == lowercase, matching the hosting manager's "healthy"/"unhealthy"/"unknown".
internal sealed class LowercaseEnvHealthStatusConverter : JsonStringEnumConverter<EnvironmentHealthReportStatus>
{
    public LowercaseEnvHealthStatusConverter()
        : base(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    { }
}

public sealed class EnvironmentHealthResponse
{
    public EnvironmentHealthReportStatus Status { get; set; }
    public string? Reason { get; set; }
    public int? CheckedPort { get; set; }
    public string? CheckedPath { get; set; }
}
