using Auth.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Organizations.Abstractions;
using Organizations.Data.Repositories;
using Organizations.Models;
using Organizations.Services;

namespace Organizations.Endpoints.Invitations.GetInvitations;

public record InvitationResponse(Guid Id, string? Email, string? GithubUsername, OrganizationRole Role, InvitationStatus Status, DateTime ExpiresAtUtc, DateTime CreatedAtUtc);

public static class GetInvitationsRoute
{
    public static RouteHandlerBuilder AddGetInvitationsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet("", async (
            OrganizationId organizationId,
            ICurrentUserAccessor currentUserAccessor,
            IOrganizationInvitationRepository invitationRepository,
            IOrganizationAuthorizationService authService) =>
        {
            var userId = currentUserAccessor.CurrentUserId!;
            if (!await authService.IsAdminAsync(organizationId, userId))
                return Results.Forbid();

            var invitations = await invitationRepository.GetPendingForOrganizationAsync(organizationId);

            return Results.Ok(invitations.Select(i => new InvitationResponse(
                i.Id.Value, i.Email, i.GithubUsername, i.Role, i.Status, i.ExpiresAtUtc, i.CreatedAtUtc)).ToList());
        });
    }
}
