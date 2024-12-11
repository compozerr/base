using Core.Abstractions;

namespace Database.Models;

public abstract class BaseEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public abstract class BaseEntityWithId<TId> : BaseEntity where TId : IdBase<TId>, IId<TId>
{
    public TId Id { get; set; } = TId.Create(Guid.NewGuid());
}
