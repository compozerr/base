using System.Text.Json.Serialization;

namespace Api.Abstractions;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProjectType
{
    Compozerr,
    N8n
}
