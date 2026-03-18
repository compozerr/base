using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Organizations.Abstractions;
using Organizations.Models;

namespace Organizations.Endpoints.Invitations.InviteUser;

public record InviteUserRequest(string Email, OrganizationRole Role);

public static class InviteUserRoute
{
    public static RouteHandlerBuilder AddInviteUserRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost("", async (
            OrganizationId organizationId,
            InviteUserRequest request,
            IMediator mediator) =>
        {
            var result = await mediator.Send(new InviteUserCommand(organizationId, request.Email, request.Role));
            return TypedResults.Created($"/invitations/{result.InvitationId}", result);
        });
    }
}
