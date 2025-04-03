using System.Text.Json.Serialization;

namespace Api.Abstractions;

[JsonConverter(typeof(JsonStringEnumConverter<LogLevel>))]
public enum LogLevel
{
    Info,
    Warning,
    Error,
    Success
}
