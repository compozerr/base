using Core.Services;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Auth.Endpoints.Users.Create;

public static class CreateUserRoute
{
    public static RouteHandlerBuilder AddCreateUserRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost("/", async (
            CreateUserRequest request,
            IMediator mediator,
            ILinks links) =>
        {
            var command = new CreateUserCommand(request.Email, request.AvatarUrl);

            var user = await mediator.Send(command);

            return Results.Created(
                links.Get(UsersGroup.UsersEndpoint, [("userId", user.Id)]),
                new CreateUserResponse(user.Id));
        });
    }
}
