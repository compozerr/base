using Auth.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Auth.Endpoints.Users;

public static class UsersRoute
{
    public static RouteHandlerBuilder AddUsersRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/", async (IUserRepository userRepository) => await userRepository.GetAllAsync());
    }
}