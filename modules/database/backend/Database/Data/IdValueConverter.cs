using Core.Abstractions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Database.Data;

public class IdValueConverter<TId> : ValueConverter<TId, Guid>
    where TId : IdBase<TId>, IId<TId>
{
    public IdValueConverter() : base(
        id => id.Value,
        value => (TId)Activator.CreateInstance(typeof(TId), value)!
    )
    { }
}

public class NullableIdValueConverter<TId> : ValueConverter<TId?, Guid?>
    where TId : IdBase<TId>, IId<TId>
{
    public NullableIdValueConverter() : base(
        id => id == null ? null : id.Value,
        value => value == null ? null : (TId)Activator.CreateInstance(typeof(TId), value)!
    )
    { }
}