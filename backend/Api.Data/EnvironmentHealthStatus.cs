using System.Text.Json.Serialization;

namespace Api.Data;

[JsonConverter(typeof(JsonStringEnumConverter<EnvironmentHealthStatus>))]
public enum EnvironmentHealthStatus
{
    Unknown = 0,
    Healthy = 1,
    Unhealthy = 2,
}
