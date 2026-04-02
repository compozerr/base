using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Environments.Get;

public static class GetEnvironmentsRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddGetEnvironmentsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<GetEnvironmentsResponse> ExecuteAsync(
        ProjectId projectId,
        IMediator mediator)
        => mediator.Send(new GetEnvironmentsCommand(projectId));
}
