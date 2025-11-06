using Api.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.Property(x => x.ServerTierId)
            .HasConversion(
                v => v.Value,
                v => new ServerTierId(v));

        builder.Property(x => x.Type)
            .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<ProjectType>(v, true))
            .IsRequired()
            .HasDefaultValue(ProjectType.Compozerr);
    }
}
