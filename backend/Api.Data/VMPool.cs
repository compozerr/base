using System.Text.Json.Serialization;

namespace Api.Data;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VMPoolType
{
    Unknown = 0,
    N8n = 1
}

public class VMPool : BaseEntityWithId<VMPoolId>
{
    public required ServerId ServerId { get; set; }
    public required VMPoolType VMPoolType { get; set; }

    public required int InstanceCount { get; set; } = 0;

    public virtual Server? Server { get; set; }
    public virtual ICollection<VMPoolItem>? VMPoolItems { get; set; }
}