using Auth.Abstractions;
using Auth.Repositories;
using Auth.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Organizations.Abstractions;
using Organizations.Data.Repositories;
using Organizations.Models;
using Organizations.Services;

namespace Organizations.Endpoints.Members.GetMembers;

public record MemberResponse(Guid Id, Guid UserId, string Name, string Email, string? AvatarUrl, OrganizationRole Role, DateTime JoinedAtUtc);

public static class GetMembersRoute
{
    public static RouteHandlerBuilder AddGetMembersRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet("", async (
            OrganizationId organizationId,
            ICurrentUserAccessor currentUserAccessor,
            IOrganizationMemberRepository memberRepository,
            IOrganizationAuthorizationService authService,
            IUserRepository userRepository) =>
        {
            var userId = currentUserAccessor.CurrentUserId!;
            if (!await authService.IsMemberAsync(organizationId, userId))
                return Results.NotFound();

            var members = await memberRepository.GetMembersForOrganizationAsync(organizationId);

            var memberResponses = new List<MemberResponse>();
            foreach (var member in members)
            {
                var user = await userRepository.GetByIdAsync(member.UserId);
                if (user is null) continue;

                memberResponses.Add(new MemberResponse(
                    member.Id.Value,
                    member.UserId.Value,
                    user.Name,
                    user.Email,
                    user.AvatarUrl,
                    member.Role,
                    member.CreatedAtUtc));
            }

            return Results.Ok(memberResponses);
        });
    }
}
