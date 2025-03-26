using Api.Abstractions;
using Api.Data.Extensions;
using Api.Data.Repositories;
using Auth.Services;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Projects.Deployments;

public static class GetDeploymentRoute
{
    public const string Route = "{deploymentId:guid}";

    public static RouteHandlerBuilder AddGetDeploymentRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<GetDeploymentResponse> ExecuteAsync(
        Guid projectId,
        Guid deploymentId,
        ICurrentUserAccessor currentUserAccessor,
        IDeploymentRepository deploymentRepository)
    {
        var projectIdConverted = ProjectId.Create(projectId);
        var deploymentIdConverted = DeploymentId.Create(deploymentId);

        var currentDeploymentId = await deploymentRepository.GetCurrentDeploymentId();

        var deployment = await deploymentRepository.GetByIdAsync(
            deploymentIdConverted,
            x => x
                .Include(x => x.Project!)
                    .ThenInclude(x => x.Domains)
                .Include(x => x.Project!)
                    .ThenInclude(x => x.Server!)
                        .ThenInclude(x => x.Location));

        if (deployment is not { ProjectId: not null } || deployment.ProjectId != projectIdConverted)
        {
            throw new ArgumentException("Deployment not found");
        }

        if (deployment.UserId != currentUserAccessor.CurrentUserId)
        {
            throw new ArgumentException("Deployment not found");
        }

        return new GetDeploymentResponse(
            deployment.Id.Value,
            deployment.Project?.Domains?.GetPrimary()?.GetValue ?? "unknown",
            deployment.Status,
            "Production",
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
