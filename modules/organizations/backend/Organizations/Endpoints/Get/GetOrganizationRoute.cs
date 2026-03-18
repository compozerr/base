using Auth.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Organizations.Abstractions;
using Organizations.Data.Repositories;
using Organizations.Services;

namespace Organizations.Endpoints.Get;

public record GetOrganizationResponse(Guid Id, string Name, string Slug, bool IsPersonal, string? AvatarUrl, int MemberCount);

public static class GetOrganizationRoute
{
    public const string Route = "{organizationId:guid}";

    public static RouteHandlerBuilder AddGetOrganizationRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, async (
            OrganizationId organizationId,
            ICurrentUserAccessor currentUserAccessor,
            IOrganizationRepository organizationRepository,
            IOrganizationAuthorizationService authService) =>
        {
            var userId = currentUserAccessor.CurrentUserId!;
            if (!await authService.IsMemberAsync(organizationId, userId))
                return Results.NotFound();

            var org = await organizationRepository.GetByIdWithMembersAsync(organizationId);
            if (org is null) return Results.NotFound();

            return Results.Ok(new GetOrganizationResponse(
                org.Id.Value, org.Name, org.Slug, org.IsPersonal, org.AvatarUrl,
                org.Members?.Count ?? 0));
        });
    }
}
