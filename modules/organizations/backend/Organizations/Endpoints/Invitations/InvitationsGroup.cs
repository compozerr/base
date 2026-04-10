using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Organizations.Endpoints.Invitations.GetInvitations;
using Organizations.Endpoints.Invitations.InviteUser;
using Organizations.Endpoints.Invitations.ResendInvitation;
using Organizations.Endpoints.Invitations.RevokeInvitation;

namespace Organizations.Endpoints.Invitations;

public static class InvitationsGroup
{
    public const string Route = "{organizationId:guid}/invitations";

    public static RouteGroupBuilder AddInvitationsGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddInviteUserRoute();
        group.AddGetInvitationsRoute();
        group.AddRevokeInvitationRoute();
        group.AddResendInvitationRoute();

        return group;
    }
}
