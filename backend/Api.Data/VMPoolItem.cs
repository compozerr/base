using Auth.Abstractions;

namespace Api.Data;

public class VMPoolItem : BaseEntityWithId<VMPoolItemId>
{
    public required ProjectId ProjectId { get; set; }
    public DateTime? DelegatedAt { get; set; }
    public UserId? DelegatedToUserId { get; set; }
}