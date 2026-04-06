using Api.Abstractions;
using Api.Endpoints.Projects.Services.Get;
using MediatR;

namespace Api.Endpoints.Projects.Environments.Services;

public static class GetEnvironmentServicesRoute
{
    public const string Route = "{environmentId:guid}/services";

    public static RouteHandlerBuilder AddGetEnvironmentServicesRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<GetProjectServicesResponse> ExecuteAsync(
        ProjectId projectId,
        ProjectEnvironmentId environmentId,
        IMediator mediator)
        => mediator.Send(new GetEnvironmentServicesCommand(projectId, environmentId));
}
