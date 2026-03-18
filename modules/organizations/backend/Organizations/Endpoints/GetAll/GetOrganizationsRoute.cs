using Auth.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Organizations.Data.Repositories;

namespace Organizations.Endpoints.GetAll;

public record OrganizationListItem(Guid Id, string Name, string Slug, bool IsPersonal, string? AvatarUrl);

public static class GetOrganizationsRoute
{
    public static RouteHandlerBuilder AddGetOrganizationsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet("", async (
            ICurrentUserAccessor currentUserAccessor,
            IOrganizationRepository organizationRepository) =>
        {
            var userId = currentUserAccessor.CurrentUserId!;
            var orgs = await organizationRepository.GetOrganizationsForUserAsync(userId);

            var response = orgs.Select(o => new OrganizationListItem(
                o.Id.Value, o.Name, o.Slug, o.IsPersonal, o.AvatarUrl)).ToList();

            return TypedResults.Ok(response);
        });
    }
}
