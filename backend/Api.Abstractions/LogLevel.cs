using System.Text.Json.Serialization;

namespace Api.Abstractions;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LogLevel
{
    Info,
    Warning,
    Error,
    Success
}
