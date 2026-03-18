using Carter;
using Microsoft.AspNetCore.Routing;
using Organizations.Endpoints.Create;
using Organizations.Endpoints.Delete;
using Organizations.Endpoints.Get;
using Organizations.Endpoints.GetAll;
using Organizations.Endpoints.Invitations;
using Organizations.Endpoints.Members;
using Organizations.Endpoints.TransferProject;
using Organizations.Endpoints.Update;

namespace Organizations.Endpoints;

public class OrganizationsGroup : CarterModule
{
    public OrganizationsGroup() : base("organizations")
    {
        RequireAuthorization();
        RequireRateLimiting("strict");
        WithTags("Organizations");
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddCreateOrganizationRoute();
        app.AddGetOrganizationsRoute();
        app.AddGetOrganizationRoute();
        app.AddUpdateOrganizationRoute();
        app.AddDeleteOrganizationRoute();
        app.AddTransferProjectRoute();
        app.AddMembersGroup();
        app.AddInvitationsGroup();
    }
}
