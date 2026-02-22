using Api.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Modules.SyncStatus;

public static class GetModuleSyncStatusRoute
{
    public const string Route = "sync-status";

    public static RouteHandlerBuilder AddGetModuleSyncStatusRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<GetModuleSyncStatusResponse> ExecuteAsync(
        ProjectId projectId,
        IMediator mediator)
        => mediator.Send(new GetModuleSyncStatusCommand(projectId));
}
