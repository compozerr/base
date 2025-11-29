using System.Text.Json.Serialization;

namespace Api.Data;


public class VMPool : BaseEntityWithId<VMPoolId>
{
    public required ServerId ServerId { get; set; }
    public required ProjectType ProjectType { get; set; }
    public required ServerTierId ServerTierId { get; set; }

    public required int InstanceCount { get; set; } = 0;

    public virtual Server? Server { get; set; }
    public virtual ICollection<VMPoolItem>? VMPoolItems { get; set; }
}