using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Organizations.Endpoints.Members.GetMembers;
using Organizations.Endpoints.Members.RemoveMember;
using Organizations.Endpoints.Members.UpdateMemberRole;

namespace Organizations.Endpoints.Members;

public static class MembersGroup
{
    public const string Route = "{organizationId:guid}/members";

    public static RouteGroupBuilder AddMembersGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetMembersRoute();
        group.AddUpdateMemberRoleRoute();
        group.AddRemoveMemberRoute();

        return group;
    }
}
