using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

public sealed class ProjectServiceConfiguration : IEntityTypeConfiguration<ProjectService>
{
    public void Configure(EntityTypeBuilder<ProjectService> builder)
    {
        builder.HasOne(x => x.Project)
            .WithMany(x => x.ProjectServices)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ProjectId, x.Name })
            .IsUnique();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Port)
            .IsRequired()
            .HasMaxLength(10);
    }
}
