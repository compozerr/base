using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Auth.Endpoints.Auth;

public static class LogoutRoute
{
    public static RouteHandlerBuilder AddLogoutRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync();
            return Results.Ok();
        });
    }
}
