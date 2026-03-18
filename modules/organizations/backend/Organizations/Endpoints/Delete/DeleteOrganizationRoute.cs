using Auth.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Organizations.Abstractions;
using Organizations.Data.Repositories;
using Organizations.Services;

namespace Organizations.Endpoints.Delete;

public static class DeleteOrganizationRoute
{
    public const string Route = "{organizationId:guid}";

    public static RouteHandlerBuilder AddDeleteOrganizationRoute(this IEndpointRouteBuilder app)
    {
        return app.MapDelete(Route, async (
            OrganizationId organizationId,
            ICurrentUserAccessor currentUserAccessor,
            IOrganizationRepository organizationRepository,
            IOrganizationAuthorizationService authService) =>
        {
            var userId = currentUserAccessor.CurrentUserId!;
            if (!await authService.IsAdminAsync(organizationId, userId))
                return Results.Forbid();

            var org = await organizationRepository.GetByIdAsync(organizationId);
            if (org is null) return Results.NotFound();
            if (org.IsPersonal) return Results.BadRequest("Cannot delete a personal organization.");

            await organizationRepository.DeleteAsync(organizationId);
            return Results.NoContent();
        });
    }
}
