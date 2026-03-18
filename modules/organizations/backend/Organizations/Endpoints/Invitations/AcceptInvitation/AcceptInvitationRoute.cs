using Auth.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Organizations.Data.Repositories;
using Organizations.Events;
using Organizations.Models;

namespace Organizations.Endpoints.Invitations.AcceptInvitation;

public static class AcceptInvitationRoute
{
    public const string Route = "{token}/accept";

    public static RouteHandlerBuilder AddAcceptInvitationRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, async (
            string token,
            ICurrentUserAccessor currentUserAccessor,
            IOrganizationInvitationRepository invitationRepository,
            IOrganizationMemberRepository memberRepository) =>
        {
            var userId = currentUserAccessor.CurrentUserId!;

            var invitation = await invitationRepository.GetByTokenAsync(token);
            if (invitation is null)
                return Results.NotFound("Invitation not found.");

            if (invitation.Status != InvitationStatus.Pending)
                return Results.BadRequest("This invitation is no longer valid.");

            if (invitation.ExpiresAtUtc < DateTime.UtcNow)
            {
                invitation.Status = InvitationStatus.Expired;
                await invitationRepository.UpdateAsync(invitation);
                return Results.BadRequest("This invitation has expired.");
            }

            // Check if already a member
            if (await memberRepository.IsMemberAsync(invitation.OrganizationId, userId))
            {
                invitation.Status = InvitationStatus.Accepted;
                await invitationRepository.UpdateAsync(invitation);
                return Results.Ok(new { Message = "You are already a member of this organization." });
            }

            var member = new OrganizationMember
            {
                OrganizationId = invitation.OrganizationId,
                UserId = userId,
                Role = invitation.Role
            };

            member.QueueDomainEvent(new InvitationAcceptedEvent(
                invitation.OrganizationId, userId, invitation.Id));

            await memberRepository.AddAsync(member);

            invitation.Status = InvitationStatus.Accepted;
            await invitationRepository.UpdateAsync(invitation);

            return Results.Ok(new { Message = "Invitation accepted.", OrganizationId = invitation.OrganizationId.Value });
        });
    }
}
