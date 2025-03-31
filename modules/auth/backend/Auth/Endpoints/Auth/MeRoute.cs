using Auth.Repositories;
using Auth.Services;
using Core.Extensions;
using Microsoft.AspNetCore.Builder;

namespace Auth.Endpoints.Auth;

public static class MeRoute
{
    public static RouteHandlerBuilder AddMeRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/me", async (
            ICurrentUserAccessor currentUserAccessor,
            IUserRepository userRepository) =>
        {
            var userId = currentUserAccessor.CurrentUserId;
            userId.ThrowIfNull(nameof(userId));

            var user = await userRepository.GetByIdAsync(userId);
            user.ThrowIfNull(nameof(user));

            return new MeResponse(
                user.Id.Value,
                user.Name,
                user.Email,
                user.AvatarUrl);
        });
    }
}
