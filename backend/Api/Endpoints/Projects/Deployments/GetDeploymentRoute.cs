using Api.Abstractions;
using Api.Data.Repositories;
using Auth.Services;

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
        IDeploymentRepository deploymentRepository
        )
    {
        var projectIdConverted = ProjectId.Create(projectId);
        var deploymentIdConverted = DeploymentId.Create(deploymentId);

        var deployment = await deploymentRepository.GetByIdAsync(deploymentIdConverted);

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
            deployment.Status,
            "Production",
            "main",
            deployment.CommitHash,
            "CommitMessage",
            deployment.CreatedAtUtc,
            "Creator",
            false);
    }
}
