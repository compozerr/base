
using Core.Abstractions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Data;

public class BaseEntityWithIdEntityTypeConfigurator<TId, TEntity> : IEntityTypeConfiguration<TEntity>
    where TId : IdBase<TId>, IId<TId>
    where TEntity : BaseEntityWithId<TId>
{
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(x => x.Id)
            .HasConversion(new IdValueConverter<TId>())
            .IsRequired();

        builder.HasKey(x => x.Id);
    }
}
