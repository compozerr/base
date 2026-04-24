using Mail.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mail.Data.Configurations;

public class SentEmailConfiguration : IEntityTypeConfiguration<SentEmail>
{
    public void Configure(EntityTypeBuilder<SentEmail> builder)
    {
        builder.HasIndex(e => e.SentAtUtc);
        builder.Property(e => e.Status).HasConversion<int>();
    }
}
