using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Services.UnexposeTcp;

public static class UnexposeTcpRoute
{
    public const string Route = "services/{serviceName}/expose-tcp";

    public static RouteHandlerBuilder AddUnexposeTcpRoute(this IEndpointRouteBuilder app)
    {
        return app.MapDelete(Route, ExecuteAsync);
    }

    public static Task<UnexposeTcpResponse> ExecuteAsync(
        ProjectId projectId,
        string serviceName,
        IMediator mediator)
        => mediator.Send(
            new UnexposeTcpCommand(
                projectId,
                serviceName));
}
