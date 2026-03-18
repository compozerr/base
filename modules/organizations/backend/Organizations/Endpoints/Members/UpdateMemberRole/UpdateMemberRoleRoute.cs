using Auth.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Organizations.Abstractions;
using Organizations.Data.Repositories;
using Organizations.Models;
using Organizations.Services;

namespace Organizations.Endpoints.Members.UpdateMemberRole;

public record UpdateMemberRoleRequest(OrganizationRole Role);

public static class UpdateMemberRoleRoute
{
    public const string Route = "{memberId:guid}";

    public static RouteHandlerBuilder AddUpdateMemberRoleRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPut(Route, async (
            OrganizationId organizationId,
            OrganizationMemberId memberId,
            UpdateMemberRoleRequest request,
            ICurrentUserAccessor currentUserAccessor,
            IOrganizationMemberRepository memberRepository,
            IOrganizationAuthorizationService authService) =>
        {
            var userId = currentUserAccessor.CurrentUserId!;
            if (!await authService.IsAdminAsync(organizationId, userId))
                return Results.Forbid();

            var member = await memberRepository.GetByIdAsync(memberId);
            if (member is null || member.OrganizationId != organizationId)
                return Results.NotFound();

            member.Role = request.Role;
            await memberRepository.UpdateAsync(member);

            return Results.Ok();
        });
    }
}
