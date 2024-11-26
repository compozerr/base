using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Auth.Endpoints.Auth;

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
