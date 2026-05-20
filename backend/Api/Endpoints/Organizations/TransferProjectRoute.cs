using Auth.Services;
using Api.Abstractions;
using Api.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Organizations.Abstractions;
using Organizations.Services;
using Stripe.Services;

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
            IOrganizationAuthorizationService authService,
            ISubscriptionTransferService subscriptionTransferService) =>
        {
            var userId = currentUserAccessor.CurrentUserId!;

            var projectId = ProjectId.Create(request.ProjectId);
            var project = await projectRepository.GetByIdAsync(projectId);
            if (project is null)
                return Results.NotFound("Project not found.");

            // The legacy user-owned path predates the org model — there is no source
            // org to admin-check against, so refuse rather than fall back to a weaker
            // check that would re-open the gap this endpoint was hardened to close.
            if (project.OrganizationId is null)
                throw new ArgumentException("Project is not owned by an organization and cannot be transferred.");

            var sourceOrganizationId = project.OrganizationId;

            // Admin in BOTH orgs. Target-only would let any admin pull a paying project
            // out of someone else's org into theirs; source-only would let an admin
            // dump a project into an org they have no say in.
            if (!await authService.IsAdminAsync(sourceOrganizationId, userId))
                return Results.Forbid();
            if (!await authService.IsAdminAsync(organizationId, userId))
                return Results.Forbid();

            // Migrate the Stripe subscription FIRST. If it fails (throws
            // SubscriptionTransferException → 409 via the global ExceptionHandler) we
            // leave the project where it is. If it succeeds the new subscription is
            // live on the target customer and the old one is cancelled, so we must
            // flip the project immediately after — otherwise the source org loses
            // billing for a project they still own.
            await subscriptionTransferService.TransferAsync(
                projectId,
                sourceOrganizationId,
                organizationId);

            project.OrganizationId = organizationId;
            await projectRepository.UpdateAsync(project);

            return Results.Ok(new { message = "Project transferred successfully.", organizationId = organizationId.Value });
        });
    }
}
