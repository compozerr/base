using Auth.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Organizations.Abstractions;
using Organizations.Data.Repositories;
using Organizations.Models;
using Organizations.Services;

namespace Organizations.Endpoints.Invitations.RevokeInvitation;

public static class RevokeInvitationRoute
{
    public const string Route = "{invitationId:guid}";

    public static RouteHandlerBuilder AddRevokeInvitationRoute(this IEndpointRouteBuilder app)
    {
        return app.MapDelete(Route, async (
            OrganizationId organizationId,
            OrganizationInvitationId invitationId,
            ICurrentUserAccessor currentUserAccessor,
            IOrganizationInvitationRepository invitationRepository,
            IOrganizationAuthorizationService authService) =>
        {
            var userId = currentUserAccessor.CurrentUserId!;
            if (!await authService.IsAdminAsync(organizationId, userId))
                return Results.Forbid();

            var invitation = await invitationRepository.GetByIdAsync(invitationId);
            if (invitation is null || invitation.OrganizationId != organizationId)
                return Results.NotFound();

            await invitationRepository.DeleteAsync(invitationId);
            return Results.NoContent();
        });
    }
}
