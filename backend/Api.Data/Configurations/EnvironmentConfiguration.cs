using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

public sealed class EnvironmentConfiguration : IEntityTypeConfiguration<Environment>
{
    public void Configure(EntityTypeBuilder<Environment> builder)
    {
        builder.HasOne(e => e.Project)
            .WithMany(p => p.Environments)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Domains)
            .WithOne(d => d.Environment)
            .HasForeignKey(d => d.EnvironmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.Type)
            .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<EnvironmentType>(v, true))
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<EnvironmentStatus>(v, true))
            .IsRequired();
    }
}
