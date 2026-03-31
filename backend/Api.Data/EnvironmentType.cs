using System.Text.Json.Serialization;

namespace Api.Data;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnvironmentType
{
    Production = 0,
    Preview = 1,
}
