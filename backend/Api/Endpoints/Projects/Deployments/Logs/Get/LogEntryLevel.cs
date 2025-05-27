using System.Text.Json.Serialization;

namespace Api.Endpoints.Projects.Deployments.Logs.Get;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LogEntryLevel
{
    Info,
    Warning,
    Error,
    Success
}
