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

            // Batch fetch all users in one query instead of N+1
            var userIds = members.Select(m => m.UserId).Distinct().ToList();
            var users = await userRepository.GetFilteredAsync(u => userIds.Contains(u.Id));
            var usersById = users.ToDictionary(u => u.Id);

            var memberResponses = members
                .Where(m => usersById.ContainsKey(m.UserId))
                .Select(m =>
                {
                    var user = usersById[m.UserId];
                    return new MemberResponse(
                        m.Id.Value, m.UserId.Value, user.Name, user.Email,
                        user.AvatarUrl, m.Role, m.CreatedAtUtc);
                })
                .ToList();

            return Results.Ok(memberResponses);
        });
    }
}
