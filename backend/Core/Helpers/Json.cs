using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Json;
using Core.Abstractions;

namespace Core.Helpers;

public class Json
{
    private static JsonSerializerOptions? _options;
    public static JsonSerializerOptions Options => _options ??= ConfigureOptions();

    public static JsonSerializerOptions ConfigureOptions(JsonSerializerOptions? options = null)
    {
        options ??= new JsonSerializerOptions(JsonSerializerDefaults.Web);

        options.WriteIndented = true;
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new IdConverterFactory());

        options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

        return options;
    }

    public static object? Deserialize(string json, Type type)
        => JsonSerializer.Deserialize(json, type, Options);

    public static object Deserialize(string json, Type type, Func<object> defaultValue)
        => Deserialize(json, type) ?? defaultValue();

    public static object Deserialize(string json, Type type, object defaultValue)
        => Deserialize(json, type, () => defaultValue);

    public static T? Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, Options);

    public static T Deserialize<T>(string json, Func<T> defaultValue)
        => Deserialize<T>(json) ?? defaultValue();

    public static T Deserialize<T>(string json, T defaultValue)
        => Deserialize(json, () => defaultValue);

    public static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, Options);

    public static string Serialize(object value, Type type)
        => JsonSerializer.Serialize(value, type, Options);
}