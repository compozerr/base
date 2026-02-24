using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Server.TcpAllocations;

public static class GetTcpAllocationsRoute
{
    public const string Route = "{serverId:guid}/tcp-allocations";

    public static RouteHandlerBuilder AddGetTcpAllocationsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<GetTcpAllocationsResponse> ExecuteAsync(
        ServerId serverId,
        IMediator mediator)
        => mediator.Send(new GetTcpAllocationsCommand(serverId));
}
