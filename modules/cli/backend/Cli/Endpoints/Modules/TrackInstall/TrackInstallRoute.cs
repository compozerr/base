using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Modules.TrackInstall;

public static class TrackInstallRoute
{
    public const string Route = "{organization}/{moduleName}/track-install";

    public static RouteHandlerBuilder AddTrackInstallRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static async Task<IResult> ExecuteAsync(
        string organization,
        string moduleName,
        IMediator mediator)
    {
        await mediator.Send(new TrackInstallCommand(organization, moduleName));
        return Results.Ok();
    }
}
