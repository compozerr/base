namespace Api.Data;

public class VMPoolItem : BaseEntityWithId<VMPoolItemId>
{
    public required ProjectId ProjectId { get; set; }
}