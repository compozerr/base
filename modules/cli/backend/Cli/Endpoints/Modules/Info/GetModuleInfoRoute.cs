using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Modules.Info;

public static class GetModuleInfoRoute
{
    public const string Route = "info/{organization}/{moduleName}";

    public static RouteHandlerBuilder AddGetModuleInfoRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync).AllowAnonymous();
    }

    public static Task<GetModuleInfoResponse> ExecuteAsync(
        string organization,
        string moduleName,
        IMediator mediator)
        => mediator.Send(new GetModuleInfoCommand(organization, moduleName));
}
