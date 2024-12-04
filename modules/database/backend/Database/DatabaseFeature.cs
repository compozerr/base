using Core.Feature;
using Database.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Database;

public class DatabaseFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped(typeof(IGenericRepository<,,>), typeof(GenericRepository<,,>));
    }
}
