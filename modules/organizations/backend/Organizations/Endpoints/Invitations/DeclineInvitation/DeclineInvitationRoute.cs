using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Organizations.Data.Repositories;
using Organizations.Models;

namespace Organizations.Endpoints.Invitations.DeclineInvitation;

public static class DeclineInvitationRoute
{
    public const string Route = "{token}/decline";

    public static RouteHandlerBuilder AddDeclineInvitationRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, async (
            string token,
            IOrganizationInvitationRepository invitationRepository) =>
        {
            var invitation = await invitationRepository.GetByTokenAsync(token);
            if (invitation is null) return Results.NotFound();
            if (invitation.Status != InvitationStatus.Pending) return Results.BadRequest("Invitation is no longer valid.");

            invitation.Status = InvitationStatus.Declined;
            await invitationRepository.UpdateAsync(invitation);

            return Results.Ok(new { Message = "Invitation declined." });
        });
    }
}
