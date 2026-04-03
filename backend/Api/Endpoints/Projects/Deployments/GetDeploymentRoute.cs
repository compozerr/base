using Api.Abstractions;
using Api.Data.Extensions;
using Api.Data.Repositories;
using Auth.Services;
using Microsoft.EntityFrameworkCore;
using Organizations.Services;

namespace Api.Endpoints.Projects.Deployments;

public static class GetDeploymentRoute
{
    public const string Route = "{deploymentId:guid}";

    public static RouteHandlerBuilder AddGetDeploymentRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<GetDeploymentResponse> ExecuteAsync(
        ProjectId projectId,
        DeploymentId deploymentId,
        ICurrentUserAccessor currentUserAccessor,
        IDeploymentRepository deploymentRepository,
        IOrganizationAuthorizationService orgAuthService)
    {
        var currentDeploymentId = await deploymentRepository.GetCurrentDeploymentId(projectId);

        var deployment = await deploymentRepository.GetByIdAsync(
            deploymentId,
            x => x
                .Include(x => x.Project!)
                    .ThenInclude(x => x.Domains)
                .Include(x => x.Project!)
                    .ThenInclude(x => x.Server!)
                        .ThenInclude(x => x.Location)
                .Include(x => x.Environment));

        if (deployment is not { ProjectId: not null } || deployment.ProjectId != projectId)
        {
            throw new ArgumentException("Deployment not found");
        }

        var userId = currentUserAccessor.CurrentUserId!;
        var hasAccess = deployment.Project?.OrganizationId is not null
            ? await orgAuthService.CanAccessProjectAsync(deployment.Project.OrganizationId, userId)
            : deployment.UserId == userId;

        if (!hasAccess)
        {
            throw new ArgumentException("Deployment not found");
        }

        return new GetDeploymentResponse(
            deployment.Id.Value,
            deployment.Project?.Domains?.GetPrimary()?.GetValue ?? "unknown",
            deployment.Status,
            deployment.Environment?.Name ?? deployment.Environment?.Type.ToString() ?? "Production",
            deployment.CommitBranch,
            deployment.CommitHash,
            deployment.CommitMessage,
            deployment.CreatedAtUtc,
            deployment.CommitAuthor,
            deployment.Id == currentDeploymentId,
            deployment.GetBuildDuration(),
            deployment.Project?.Server?.Location?.IsoCountryCode ?? "unknown",
            []);
    }
}
