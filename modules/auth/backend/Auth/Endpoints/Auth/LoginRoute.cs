using AspNet.Security.OAuth.GitHub;
using Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Auth.Endpoints.Auth;

public static class LoginRoute
{
    public static RouteHandlerBuilder AddLoginRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/login", (HttpContext context, IDateTimeProvider dateTimeProvider) =>
        {
            try
            {
                var returnUrl = context.Request.Query["returnUrl"].ToString();

                var properties = new AuthenticationProperties
                {
                    RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl,
                    IsPersistent = true,
                    ExpiresUtc = dateTimeProvider.UtcNow.AddDays(7),
                    Items =
                    {
                        { "state", Guid.NewGuid().ToString() },
                        { "login_time", dateTimeProvider.UtcNow.ToString() }
                    }
                };

                Log.ForContext("ReturnUrl", returnUrl)
                   .ForContext("LoginTime", dateTimeProvider.UtcNow)
                   .ForContext("State", properties.Items["state"])
                   .ForContext("IsPersistent", properties.IsPersistent)
                   .ForContext("ExpiresUtc", properties.ExpiresUtc)
                   .ForContext("RedirectUri", properties.RedirectUri)
                   .Information("Login request processed successfully");

                return Results.Challenge(properties, [GitHubAuthenticationDefaults.AuthenticationScheme]);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Login error: {ex.Message}");
                return Results.Problem(
                    title: "Login Failed",
                    detail: "Unable to process login request",
                    statusCode: 500
                );
            }
        });
    }
}
