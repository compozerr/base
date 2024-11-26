using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Auth.AuthEndpoints;

public static class LogoutRoute
{
    public static void AddLogoutRoute(this IEndpointRouteBuilder app)
    {
        app.MapGet("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync();
            return Results.Ok();
        });
    }
}
