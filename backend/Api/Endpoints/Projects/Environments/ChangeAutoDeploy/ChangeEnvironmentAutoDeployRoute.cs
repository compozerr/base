using Api.Abstractions;
using Api.Data.Repositories;

namespace Api.Endpoints.Projects.Environments.ChangeAutoDeploy;

public sealed record ChangeEnvironmentAutoDeployRequest(bool AutoDeploy);
public sealed record ChangeEnvironmentAutoDeployResponse(bool AutoDeploy);

public static class ChangeEnvironmentAutoDeployRoute
{
    public const string Route = "{environmentId:guid}/auto-deploy";

    public static RouteHandlerBuilder AddChangeEnvironmentAutoDeployRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPut(Route, ExecuteAsync);
    }

    public static async Task<IResult> ExecuteAsync(
        ProjectId projectId,
        ProjectEnvironmentId environmentId,
        ChangeEnvironmentAutoDeployRequest request,
        IProjectEnvironmentRepository environmentRepository)
    {
        var environment = await environmentRepository.GetByIdAsync(environmentId)
            ?? throw new InvalidOperationException($"Environment {environmentId} not found.");

        if (environment.ProjectId != projectId)
            throw new InvalidOperationException("Environment does not belong to the specified project.");

        environment.AutoDeploy = request.AutoDeploy;
        await environmentRepository.UpdateAsync(environment);

        return TypedResults.Ok(new ChangeEnvironmentAutoDeployResponse(environment.AutoDeploy));
    }
}
