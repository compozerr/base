using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Modules.Release;

public static class ReleaseModuleRoute
{
    public const string Route = "release";

    public static RouteHandlerBuilder AddReleaseModuleRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<ReleaseModuleResponse> ExecuteAsync(
        ReleaseModuleCommand command,
        IMediator mediator)
        => mediator.Send(command);
}
