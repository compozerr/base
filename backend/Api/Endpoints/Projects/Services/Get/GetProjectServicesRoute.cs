using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Services.Get;

public static class GetProjectServicesRoute
{
    public const string Route = "services";

    public static RouteHandlerBuilder AddGetProjectServicesRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<GetProjectServicesResponse> ExecuteAsync(
        ProjectId projectId,
        IMediator mediator)
        => mediator.Send(new GetProjectServicesCommand(projectId));
}
