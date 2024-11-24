using Auth.Data;
using Auth.Repositories;
using Core.Feature;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth;

public class AuthFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(options =>
       {
           options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), b =>
           {
               b.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName);
           });
       });

       services.AddScoped<IUserRepository, UserRepository>();
    }
}
