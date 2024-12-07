using AspNet.Security.OAuth.GitHub;
using Auth.AuthProviders;
using Auth.Data;
using Auth.Repositories;
using Auth.Services;
using Core.Feature;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        services.AddTransient<ICurrentUserAccessor, CurrentUserAccessor>();
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

            context.Database.Migrate();
        }
    }
}
