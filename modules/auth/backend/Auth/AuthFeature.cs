using System.Text;
using AspNet.Security.OAuth.GitHub;
using Auth.Authentication;
using Auth.AuthProviders;
using Auth.Data;
using Auth.Repositories;
using Auth.Services;
using Core.Feature;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Auth;

public class AuthFeature : IFeature
{
    void IFeature.ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), b =>
            {
                b.MigrationsAssembly(typeof(AuthDbContext).Assembly.FullName);
            });
        });

        // Configure Data Protection to persist keys across app restarts
        var keysPath = Path.Combine(Directory.GetCurrentDirectory(), "keys");
        Directory.CreateDirectory(keysPath);
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName("Compozerr");

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = GitHubAuthenticationDefaults.AuthenticationScheme;
        })
        .AddGithubAuthProvider()
        .AddCookie(options =>
        {
            // Make cookies persistent across browser sessions
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;

            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"]!,
                ValidAudience = configuration["Jwt:Audience"]!,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)
                )
            };
        })
        .AddScheme<ApiTokenAuthenticationOptions, ApiTokenAuthenticationHandler>(
            ApiTokenAuthenticationDefaults.AuthenticationScheme, null);

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                                        .RequireAuthenticatedUser()
                                        .AddAuthenticationSchemes(
                                            CookieAuthenticationDefaults.AuthenticationScheme,
                                            JwtBearerDefaults.AuthenticationScheme,
                                            ApiTokenAuthenticationDefaults.AuthenticationScheme)
                                        .Build();
        });

        services.AddAuthorizationBuilder()
                .AddPolicy("admin", policy => policy.RequireRole("admin"));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddTransient<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddSingleton<IStateService, StateService>();
        services.AddScoped<IApiTokenService, ApiTokenService>();
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        context.Database.Migrate();
    }
}
