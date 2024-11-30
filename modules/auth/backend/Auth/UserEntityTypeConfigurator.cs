
using Auth.Models;
using Database.Data;
using Microsoft.EntityFrameworkCore;

namespace Auth;

public class UserEntityTypeConfigurator : IEntityTypeConfiguration<User>
{
    public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<User> builder)
    {
        builder.ToTable(nameof(User));

        builder.Property(x => x.Id)
            .HasConversion(new IdValueConverter<UserId>())
            .IsRequired();

        builder.HasKey(x => x.Id);
    }
}
