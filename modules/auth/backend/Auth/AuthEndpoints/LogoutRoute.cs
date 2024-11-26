using Carter;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Auth.AuthEndpoints;

public static class LogoputRoute
{
    public static void AddLogoutRoute(this IEndpointRouteBuilder app)
    {
        app.MapPost("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync();
        });
    }
}
