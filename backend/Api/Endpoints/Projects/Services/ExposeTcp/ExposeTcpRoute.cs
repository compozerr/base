using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Services.ExposeTcp;

public static class ExposeTcpRoute
{
    public const string Route = "services/{serviceName}/expose-tcp";

    public static RouteHandlerBuilder AddExposeTcpRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<ExposeTcpResponse> ExecuteAsync(
        ProjectId projectId,
        string serviceName,
        IMediator mediator)
        => mediator.Send(
            new ExposeTcpCommand(
                projectId,
                serviceName));
}
