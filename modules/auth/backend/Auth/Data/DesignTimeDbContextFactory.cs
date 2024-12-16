
using Core.Extensions;
using Core.Feature;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Auth.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{

    public AuthDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder().AddAppSettings()
                                                      .Build();

        // Create DbContextOptionsBuilder
        var builder = new DbContextOptionsBuilder<AuthDbContext>();

        Features.RegisterConfigureCallback<AssembliesFeatureConfigureCallback>();
        Features.ConfigureCallbacks();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        builder.UseNpgsql(connectionString, b =>
            b.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName));

        return new AuthDbContext(builder.Options, null!);
    }
}