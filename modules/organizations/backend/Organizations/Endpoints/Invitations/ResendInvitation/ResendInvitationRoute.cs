using Auth.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Organizations.Abstractions;
using Organizations.Data.Repositories;
using Organizations.Events;
using Organizations.Models;
using Organizations.Services;

namespace Organizations.Endpoints.Invitations.ResendInvitation;

public static class ResendInvitationRoute
{
    public const string Route = "{invitationId:guid}/resend";
    private static readonly TimeSpan CooldownPeriod = TimeSpan.FromMinutes(30);

    public static RouteHandlerBuilder AddResendInvitationRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, async (
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

            if (invitation.Status != InvitationStatus.Pending)
                return Results.BadRequest(new { error = "Invitation is no longer pending." });

            if (string.IsNullOrWhiteSpace(invitation.Email))
                return Results.BadRequest(new { error = "Cannot resend invitation without an email address." });

            if (invitation.LastEmailSentAtUtc.HasValue)
            {
                var elapsed = DateTime.UtcNow - invitation.LastEmailSentAtUtc.Value;
                if (elapsed < CooldownPeriod)
                {
                    var retryAfterSeconds = (int)Math.Ceiling((CooldownPeriod - elapsed).TotalSeconds);
                    return Results.Json(
                        new { error = "Too many requests", message = "Please wait before resending.", retryAfter = retryAfterSeconds },
                        statusCode: StatusCodes.Status429TooManyRequests);
                }
            }

            invitation.LastEmailSentAtUtc = DateTime.UtcNow;
            invitation.QueueDomainEvent(new InvitationSentEvent(invitation.Id));
            await invitationRepository.UpdateAsync(invitation);

            return Results.Ok(new { message = "Invitation resent." });
        }).RequireRateLimiting("strict");
    }
}
