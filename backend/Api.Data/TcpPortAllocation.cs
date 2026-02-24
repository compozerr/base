using System.ComponentModel.DataAnnotations;

namespace Api.Data;

public class TcpPortAllocation : BaseEntityWithId<TcpPortAllocationId>
{
    public required ProjectId ProjectId { get; set; }
    [MaxLength(255)]
    public required string ServiceName { get; set; }
    [MaxLength(6)]
    public required string InternalPort { get; set; }
    public required int AllocatedPort { get; set; }
    [MaxLength(45)]
    public required string VmIp { get; set; }

    public virtual Project? Project { get; set; }
}
