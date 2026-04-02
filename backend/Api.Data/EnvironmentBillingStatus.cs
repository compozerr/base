using System.Text.Json.Serialization;

namespace Api.Data;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnvironmentBillingStatus
{
    Active = 0,
    Trialing = 1,
    Suspended = 2,
    Cancelled = 3,
}
