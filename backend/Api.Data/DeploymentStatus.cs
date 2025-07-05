using System.Text.Json.Serialization;

namespace Api.Data;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeploymentStatus
{
    Unknown = 0,
    Deploying = 1,
    Completed = 2,
    Queued = 3,
    Failed = 4,
    Cancelled = 5,
}
