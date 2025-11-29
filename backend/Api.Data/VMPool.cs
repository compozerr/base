namespace Api.Data;


public class VMPool : BaseEntityWithId<VMPoolId>
{
    public required LocationId LocationId { get; set; }
    public required ProjectType ProjectType { get; set; }
    public required string ServerTierId { get; set; }

    public required int InstanceCount { get; set; } = 0;

    public virtual ICollection<VMPoolItem>? VMPoolItems { get; set; }
}