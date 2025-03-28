using System.Text.Json.Serialization;

namespace Api.Data;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SystemType
{
    Unknown = 0,
    Frontend = 1,
    Backend = 2,
}
