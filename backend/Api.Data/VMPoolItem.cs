using Auth.Abstractions;

namespace Api.Data;

public class VMPoolItem : BaseEntityWithId<VMPoolItemId>
{
    public required VMPoolId VMPoolId { get; set; }
    public required ProjectId ProjectId { get; set; }

    public DateTime? DelegatedAt { get; set; }
    public UserId? DelegatedToUserId { get; set; }

    public virtual VMPool? VMPool { get; set; } = null;
}