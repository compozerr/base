using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

public sealed class TcpPortAllocationConfiguration : IEntityTypeConfiguration<TcpPortAllocation>
{
    public void Configure(EntityTypeBuilder<TcpPortAllocation> builder)
    {
        builder.HasOne(x => x.Project)
            .WithMany()
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.AllocatedPort)
            .IsUnique();

        builder.Property(x => x.ServiceName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.InternalPort)
            .IsRequired()
            .HasMaxLength(6);

        builder.Property(x => x.VmIp)
            .IsRequired()
            .HasMaxLength(45);
    }
}
