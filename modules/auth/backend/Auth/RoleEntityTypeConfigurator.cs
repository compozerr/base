
using Auth.Models;
using Database.Data;
using Microsoft.EntityFrameworkCore;

namespace Auth;

public class RoleEntityTypeConfigurator : IEntityTypeConfiguration<Role>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Role> builder)
    {
        builder.ToTable(nameof(Role));

        builder.Property(x => x.Id)
            .HasConversion(new IdValueConverter<RoleId>())
            .IsRequired();

        builder.HasKey(x => x.Id);
    }
}
