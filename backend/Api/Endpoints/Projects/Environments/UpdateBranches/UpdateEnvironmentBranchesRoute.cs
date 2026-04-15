using Api.Abstractions;
using Api.Data.Repositories;

namespace Api.Endpoints.Projects.Environments.UpdateBranches;

public sealed record UpdateEnvironmentBranchesRequest(List<string> Branches);
public sealed record UpdateEnvironmentBranchesResponse(List<string> Branches, bool EffectiveAutoDeploy);

public static class UpdateEnvironmentBranchesRoute
{
    public const string Route = "{environmentId:guid}/branches";

    public static RouteHandlerBuilder AddUpdateEnvironmentBranchesRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPut(Route, ExecuteAsync);
    }

    public static async Task<IResult> ExecuteAsync(
        ProjectId projectId,
        ProjectEnvironmentId environmentId,
        UpdateEnvironmentBranchesRequest request,
        IProjectEnvironmentRepository environmentRepository)
    {
        var environment = await environmentRepository.GetByIdAsync(environmentId)
            ?? throw new InvalidOperationException($"Environment {environmentId} not found.");

        if (environment.ProjectId != projectId)
            throw new InvalidOperationException("Environment does not belong to the specified project.");

        environment.Branches = request.Branches ?? [];
        await environmentRepository.UpdateAsync(environment);

        return TypedResults.Ok(new UpdateEnvironmentBranchesResponse(
            environment.Branches,
            environment.EffectiveAutoDeploy));
    }
}
