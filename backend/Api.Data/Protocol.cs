using System.Text.Json.Serialization;

namespace Api.Data;

[JsonConverter(typeof(JsonStringEnumConverter<Protocol>))]
public enum Protocol
{
    Http = 0,
    Tcp = 1
}
