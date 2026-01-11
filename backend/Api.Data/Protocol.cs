using System.Text.Json.Serialization;

namespace Api.Data;

[JsonConverter(typeof(JsonStringEnumConverter<Protocol>))]
public enum Protocol
{
    Http = 0,
    Tcp = 1
}

public static class ProtocolExtensions
{
    public static Protocol ParseOrDefault(string value)
        => Enum.TryParse<Protocol>(value, true, out var result) ? result : Protocol.Http;
}
