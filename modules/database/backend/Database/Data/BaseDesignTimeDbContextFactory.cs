using Core.Extensions;
using Core.Feature;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Database.Data;

public class BaseDesignTimeDbContextFactory<TDbContext> : IDesignTimeDbContextFactory<TDbContext> where TDbContext : BaseDbContext<TDbContext>
{
    public TDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder().AddAppSettings()
                                                      .Build();

        // Create DbContextOptionsBuilder
        var builder = new DbContextOptionsBuilder<TDbContext>();

        Features.RegisterConfigureCallback<AssembliesFeatureConfigureCallback>();
        Features.ConfigureCallbacks();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        builder.UseNpgsql(connectionString, b =>
            b.MigrationsAssembly(typeof(TDbContext).Assembly.FullName));

        return (TDbContext)Activator.CreateInstance(typeof(TDbContext), builder.Options, null!)!;
    }
}