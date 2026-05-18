using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Project.ChangeRepository;

public static class ChangeRepositoryRoute
{
    public const string Route = "{projectId:guid}/change-repository";

    public sealed record Request(string RepoFullName);

    public static RouteHandlerBuilder AddChangeRepositoryRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    private static Task<ChangeRepositoryResponse> ExecuteAsync(
        ProjectId projectId,
        Request body,
        IMediator mediator)
        => mediator.Send(new ChangeRepositoryCommand(projectId, body.RepoFullName));
}
