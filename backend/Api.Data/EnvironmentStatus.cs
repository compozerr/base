using System.Text.Json.Serialization;

namespace Api.Data;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnvironmentStatus
{
    Active = 0,
    Sleeping = 1,
    Deploying = 2,
    Failed = 3,
    Destroying = 4,
}
