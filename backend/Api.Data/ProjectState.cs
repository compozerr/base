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
    /// <summary>
    /// Derived display state: containers are up (Running) but the primary HTTP service
    /// is not responding to health checks. Never written to the DB directly — computed
    /// at read time from Project.State + any non-preview ProjectEnvironment.HealthStatus.
    /// </summary>
    Unhealthy = 5,
}