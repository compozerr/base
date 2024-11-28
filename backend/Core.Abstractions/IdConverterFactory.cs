using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Abstractions;

public class IdConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IId<>));

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var idType = typeToConvert.GetInterfaces()
            .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IId<>))
            .GenericTypeArguments[0];

        return (JsonConverter?)Activator.CreateInstance(
            typeof(IdConverter<>).MakeGenericType(idType));
    }
}

internal class IdConverter<T> : JsonConverter<T>
    where T : IdBase<T>, IId<T>
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && Guid.TryParse(reader.GetString(), out var guid))
            return T.Create(guid);

        return null;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value.ToString());
    }
}