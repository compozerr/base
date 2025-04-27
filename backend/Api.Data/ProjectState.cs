using System.Text.Json.Serialization;

namespace Api.Data;

[JsonConverter(typeof(JsonStringEnumConverter<ProjectState>))]
public enum ProjectState
{
    Unknown = 0,
    Running = 1,
    Starting = 2,
    Stopped = 3,
    Deleting = 4,
}