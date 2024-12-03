using AspNet.Security.OAuth.GitHub;
using Core.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Build.Utilities;
using Serilog;

namespace Auth.Endpoints.Auth;

public static class LoginRoute
{
    public static RouteHandlerBuilder AddLoginRoute(this IEndpointRouteBuilder app)
    {
        //When going directly to the login page, the user will be redirected to the home page after logging in
        return app.MapGet("/login", async (HttpContext context, IDateTimeProvider dateTimeProvider) =>
        {
            try
            {
                var returnUrl = context.Request.Query["returnUrl"].ToString();

                if (!string.IsNullOrEmpty(returnUrl))
                {
                    if (!returnUrl.StartsWith('/') || returnUrl.Contains("//"))
                    {
                        return Results.BadRequest("Invalid return URL");
                    }
                }

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

                await context.ChallengeAsync(GitHubAuthenticationDefaults.AuthenticationScheme, properties);

                Log.ForContext("ReturnUrl", returnUrl)
                   .ForContext("LoginTime", dateTimeProvider.UtcNow)
                   .ForContext("User", context.User.Identity?.Name)
                   .ForContext("State", properties.Items["state"])
                   .ForContext("IsPersistent", properties.IsPersistent)
                   .ForContext("ExpiresUtc", properties.ExpiresUtc)
                   .ForContext("RedirectUri", properties.RedirectUri)
                   .Information("Login request processed successfully");

                return Results.Empty;
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
