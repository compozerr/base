
using Auth.Data;
using Core.Extensions;
using Database.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{

    public AuthDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder().AddAppSettings().Build();

        // Create DbContextOptionsBuilder
        var builder = new DbContextOptionsBuilder<AuthDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        builder.UseNpgsql(connectionString, b =>
            b.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName));

        return new AuthDbContext(builder.Options);
    }
}