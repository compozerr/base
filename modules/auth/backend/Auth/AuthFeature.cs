using AspNet.Security.OAuth.GitHub;
using Auth.AuthProviders;
using Auth.Data;
using Auth.Repositories;
using Core.Feature;
using Microsoft.AspNetCore.Authentication.Cookies;
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

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = GitHubAuthenticationDefaults.AuthenticationScheme;
        })
        .AddGithubAuthProvider()
        .AddCookie(options =>
        {
            options.AccessDeniedPath = "/access-denied";
        });

        services.AddAuthorization();

        services.AddAuthorizationBuilder()
            .AddPolicy("admin", policy => policy.RequireRole("admin"));
    }
}
