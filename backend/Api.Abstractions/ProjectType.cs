using System.Text.Json.Serialization;

namespace Api.Abstractions;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProjectType
{
    /// <summary>
    /// Compozerr standard project with frontend/ and backend/ folders
    /// Maps to CLI template type: "compozerr-standard"
    /// </summary>
    Compozerr,

    /// <summary>
    /// N8n workflow automation
    /// </summary>
    N8n,

    /// <summary>
    /// Docker Compose based project
    /// Maps to CLI template type: "docker-compose"
    /// </summary>
    DockerCompose,

    /// <summary>
    /// Single Dockerfile based project
    /// Maps to CLI template type: "dockerfile"
    /// </summary>
    Dockerfile
}
