using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace Auth.Endpoints.Auth;

public static class MeRoute
{
    public static RouteHandlerBuilder AddMeRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/me", [Authorize] (ClaimsPrincipal user) =>
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            var name = user.Identity?.Name ?? email;
            var avatarUrl = user.FindFirst("urn:github:avatar")?.Value;

            ArgumentException.ThrowIfNullOrEmpty(userId, nameof(userId));
            ArgumentException.ThrowIfNullOrEmpty(email, nameof(email));
            ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
            ArgumentException.ThrowIfNullOrEmpty(avatarUrl, nameof(avatarUrl));

            return new MeResponse(
                UserId.Parse(userId),
                name,
                email,
                avatarUrl);
        });
    }
}
