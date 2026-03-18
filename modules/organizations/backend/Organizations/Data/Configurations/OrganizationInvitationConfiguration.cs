using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Organizations.Models;

namespace Organizations.Data.Configurations;

public class OrganizationInvitationConfiguration : IEntityTypeConfiguration<OrganizationInvitation>
{
    public void Configure(EntityTypeBuilder<OrganizationInvitation> builder)
    {
        builder.Property(x => x.Email).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Token).HasMaxLength(64).IsRequired();

        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.Email, x.Status });
    }
}
