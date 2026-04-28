using Auth.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Organizations.Data.Repositories;
using Organizations.Models;

namespace Organizations.Endpoints.Invitations.GetInvitationByToken;

public static class GetInvitationByTokenRoute
{
    public const string Route = "{token}";

    public static RouteHandlerBuilder AddGetInvitationByTokenRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, async (
            string token,
            IOrganizationInvitationRepository invitationRepository,
            IOrganizationRepository organizationRepository,
            IUserRepository userRepository,
            CancellationToken cancellationToken) =>
        {
            var invitation = await invitationRepository.GetByTokenAsync(token, cancellationToken);
            if (invitation is null)
                return Results.NotFound();

            var organization = await organizationRepository.GetByIdAsync(invitation.OrganizationId, cancellationToken);
            if (organization is null)
                return Results.NotFound();

            var inviter = await userRepository.GetByIdAsync(invitation.InvitedByUserId, cancellationToken);

            var status = invitation.Status;
            var isExpired = invitation.ExpiresAtUtc < DateTime.UtcNow;
            if (isExpired && status == InvitationStatus.Pending)
                status = InvitationStatus.Expired;

            return Results.Ok(new GetInvitationByTokenResponse(
                invitation.Id.Value,
                invitation.OrganizationId.Value,
                organization.Name,
                inviter?.Name,
                invitation.Role,
                status,
                invitation.ExpiresAtUtc,
                isExpired));
        })
        .Produces<GetInvitationByTokenResponse>()
        .Produces(StatusCodes.Status404NotFound);
    }
}
