using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

public sealed class DomainConfiguration : IEntityTypeConfiguration<Domain>
{
    public void Configure(EntityTypeBuilder<Domain> builder)
    {
        builder.HasDiscriminator(x => x.Type)
            .HasValue<Domain>(DomainType.Unknown)
            .HasValue<InternalDomain>(DomainType.Internal)
            .HasValue<ExternalDomain>(DomainType.External);

        builder.Property(x => x.Protocol)
            .HasConversion(
                v => v.ToString().ToLowerInvariant(),
                v => Enum.Parse<Protocol>(v, true))
            .HasMaxLength(20);
    }
}
