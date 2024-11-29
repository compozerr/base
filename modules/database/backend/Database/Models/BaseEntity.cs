using Core.Abstractions;

namespace Database.Models;

public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public abstract class BaseEntityWithId<TId> : BaseEntity where TId : IdBase<TId>, IId<TId>
{
    public TId Id { get; set; } = TId.Create(Guid.NewGuid());
}
