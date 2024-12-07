using Auth.Repositories;
using Auth.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Auth.Endpoints.Users;

public static class UserRoute
{
    public static RouteHandlerBuilder AddUserMeRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/me", async (ICurrentUserAccessor currentUserAccessor, IUserRepository userRepository)
            => currentUserAccessor.CurrentUserId is not null
                ? Results.Ok(await userRepository.GetByIdAsync(currentUserAccessor.CurrentUserId))
                : Results.NotFound());
    }
}