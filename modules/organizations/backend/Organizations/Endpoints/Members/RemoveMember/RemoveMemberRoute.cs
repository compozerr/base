using Auth.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Organizations.Abstractions;
using Organizations.Data.Repositories;
using Organizations.Services;

namespace Organizations.Endpoints.Members.RemoveMember;

public static class RemoveMemberRoute
{
    public const string Route = "{memberId:guid}";

    public static RouteHandlerBuilder AddRemoveMemberRoute(this IEndpointRouteBuilder app)
    {
        return app.MapDelete(Route, async (
            OrganizationId organizationId,
            OrganizationMemberId memberId,
            ICurrentUserAccessor currentUserAccessor,
            IOrganizationMemberRepository memberRepository,
            IOrganizationAuthorizationService authService,
            IOrganizationRepository organizationRepository) =>
        {
            var userId = currentUserAccessor.CurrentUserId!;
            if (!await authService.IsAdminAsync(organizationId, userId))
                return Results.Forbid();

            var member = await memberRepository.GetByIdAsync(memberId);
            if (member is null || member.OrganizationId != organizationId)
                return Results.NotFound();

            // Prevent removing the org owner
            var org = await organizationRepository.GetByIdAsync(organizationId);
            if (org is not null && member.UserId == org.OwnerUserId)
                return Results.BadRequest("Cannot remove the organization owner.");

            await memberRepository.DeleteAsync(memberId);
            return Results.NoContent();
        });
    }
}
