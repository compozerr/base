using Api.Data;
using MediatR;

namespace Api.Endpoints.Projects.ProjectEnvironment;

public static class GetProjectEnvironmentRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddGetProjectEnvironmentRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<GetProjectEnvironmentResponse> ExecuteAsync(
        Guid projectId,
        string branch,
        IMediator mediator)
    => mediator.Send(
        new GetProjectEnvironmentCommand(
            projectId,
            branch));
}
