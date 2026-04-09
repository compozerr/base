using Api.Abstractions;
using Api.Data.Repositories;
using Github.Services;
using Github.Utils;

namespace Api.Endpoints.Projects.Environments.GetBranches;

public sealed record GetBranchesResponse(List<string> Branches);

public static class GetBranchesRoute
{
    public const string Route = "branches";

    public static RouteHandlerBuilder AddGetBranchesRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<GetBranchesResponse> ExecuteAsync(
        ProjectId projectId,
        IProjectRepository projectRepository,
        IGithubService githubService,
        CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new ArgumentException("Project not found");

        var (owner, repo) = GitHubRepoUrl.Parse(project.RepoUri);

        GetInstallationClientForRepoResponse clientResponse;
        try
        {
            clientResponse = await githubService.GetInstallationClientForRepoAsync(project.RepoUri);
        }
        catch (InstallationNotFoundForRepoException)
        {
            return new GetBranchesResponse([]);
        }

        var branches = await clientResponse.InstallationClient.Repository.Branch.GetAll(owner, repo);

        return new GetBranchesResponse(
            branches.Select(b => b.Name).OrderBy(n => n).ToList());
    }
}
