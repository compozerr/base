using Auth.Services;
using Api.Abstractions;
using Api.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Organizations.Abstractions;
using Organizations.Services;

namespace Api.Endpoints.Organizations;

public record TransferProjectRequest(Guid ProjectId);

public static class TransferProjectRoute
{
    public const string Route = "{organizationId:guid}/transfer-project";

    public static RouteHandlerBuilder AddTransferProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, async (
            OrganizationId organizationId,
            TransferProjectRequest request,
            ICurrentUserAccessor currentUserAccessor,
            IProjectRepository projectRepository,
            IOrganizationAuthorizationService authService) =>
        {
            var userId = currentUserAccessor.CurrentUserId!;

            // Must be admin of the target organization
            if (!await authService.IsAdminAsync(organizationId, userId))
                return Results.Forbid();

            var projectId = ProjectId.Create(request.ProjectId);
            var project = await projectRepository.GetByIdAsync(projectId);
            if (project is null)
                return Results.NotFound("Project not found.");

            // Must have access to the project (member of source org, or direct owner)
            var hasAccess = project.OrganizationId is not null
                ? await authService.CanAccessProjectAsync(project.OrganizationId, userId)
                : project.UserId == userId;

            if (!hasAccess)
                return Results.Forbid();

            project.OrganizationId = organizationId;
            await projectRepository.UpdateAsync(project);

            return Results.Ok(new { message = "Project transferred successfully.", organizationId = organizationId.Value });
        });
    }
}
