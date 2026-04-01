using Api.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

public sealed class ProjectEnvironmentConfiguration : IEntityTypeConfiguration<ProjectEnvironment>
{
    public void Configure(EntityTypeBuilder<ProjectEnvironment> builder)
    {
        builder.Property(x => x.ServerTierId)
            .HasConversion(
                v => v != null ? v.Value : null,
                v => v != null ? new ServerTierId(v) : null);
    }
}
