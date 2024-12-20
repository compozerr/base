using Core.Extensions;
using Core.Feature;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Github.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<GithubDbContext>
{

    public GithubDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder().AddAppSettings()
                                                      .Build();

        // Create DbContextOptionsBuilder
        var builder = new DbContextOptionsBuilder<GithubDbContext>();

        Core.Feature.Features.RegisterConfigureCallback<AssembliesFeatureConfigureCallback>();
        Core.Feature.Features.ConfigureCallbacks();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        builder.UseNpgsql(connectionString, b =>
            b.MigrationsAssembly(typeof(GithubDbContext).Assembly.FullName));

        return new GithubDbContext(builder.Options, null!);
    }
}