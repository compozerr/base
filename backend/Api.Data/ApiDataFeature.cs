using Core.Feature;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Data;

public class ApiDataFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApiDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), b =>
            {
                b.MigrationsAssembly(typeof(ApiDbContext).Assembly.FullName);
            });
        });
    }
}