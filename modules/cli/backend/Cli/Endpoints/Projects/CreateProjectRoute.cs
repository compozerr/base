using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Projects;

public record CreateProjectRequest(
    string RepoName,
    string RepoUrl,
    string LocationIso,
    string Tier);

public static class CreateProjectRoute
{
    public const string Route = "/";

    public static RouteHandlerBuilder AddCreateProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<CreateProjectResponse> ExecuteAsync(
        CreateProjectRequest createProjectRequest,
        IMediator mediator)
        => mediator.Send(
            new CreateProjectCommand(
                createProjectRequest.RepoName,
                createProjectRequest.RepoUrl,
                createProjectRequest.LocationIso,
                createProjectRequest.Tier));
}
