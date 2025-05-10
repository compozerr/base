using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Modules.Add;

public static class AddModuleRoute
{
    public const string Route = "add";

    public static RouteHandlerBuilder AddAddModuleRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task ExecuteAsync(
        AddModuleCommand command,
        IMediator mediator)
        => mediator.Send(command);
}
