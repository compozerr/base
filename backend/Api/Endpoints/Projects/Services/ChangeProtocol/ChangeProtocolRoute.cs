using Api.Abstractions;
using Api.Data;
using MediatR;

namespace Api.Endpoints.Projects.Services.ChangeProtocol;

public static class ChangeProtocolRoute
{
    public const string Route = "services/{serviceName}/protocol";

    public static RouteHandlerBuilder AddChangeProtocolRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPatch(Route, ExecuteAsync);
    }

    public static Task<ChangeProtocolResponse> ExecuteAsync(
        ProjectId projectId,
        string serviceName,
        ChangeProtocolRequest request,
        IMediator mediator)
        => mediator.Send(
            new ChangeProtocolCommand(
                projectId,
                serviceName,
                request.Protocol));
}

public sealed record ChangeProtocolRequest(Protocol Protocol);
