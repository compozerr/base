using Carter;
using Microsoft.AspNetCore.Routing;
using Organizations.Endpoints.Invitations.AcceptInvitation;
using Organizations.Endpoints.Invitations.DeclineInvitation;

namespace Organizations.Endpoints;

public class InvitationActionsGroup : CarterModule
{
    public InvitationActionsGroup() : base("invitations")
    {
        RequireAuthorization();
        RequireRateLimiting("strict");
        WithTags("Invitations");
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddAcceptInvitationRoute();
        app.AddDeclineInvitationRoute();
    }
}
