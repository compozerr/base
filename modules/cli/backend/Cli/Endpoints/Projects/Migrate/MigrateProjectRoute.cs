using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Projects.Migrate;

public sealed record MigrateProjectRequest(
    string Name,
    string RepoUrl,
    string LocationIso,
    string Tier);

public static class MigrateProjectRoute
{
    public const string Route = "migrate";

    public static RouteHandlerBuilder AddMigrateProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<MigrateProjectResponse> ExecuteAsync(
        MigrateProjectRequest request,
        IMediator mediator)
        => mediator.Send(
            new MigrateProjectCommand(
                request.Name,
                request.RepoUrl,
                request.LocationIso,
                request.Tier));
}
