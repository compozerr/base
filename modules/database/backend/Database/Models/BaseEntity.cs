using Core.Abstractions;

namespace Database.Models;

public abstract class BaseEntity<TId> where TId : IdBase<TId>, IId<TId>
{
    public TId Id { get; set; } = TId.Create(Guid.NewGuid());
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}