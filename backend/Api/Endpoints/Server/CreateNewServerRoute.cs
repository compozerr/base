using Api.Services;

namespace Api.Endpoints.Server;

public static class CreateNewServerRoute
{
    public const string Route = "/";

    public static RouteHandlerBuilder AddCreateNewServerRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<string> ExecuteAsync(IServerService serverService)
        => serverService.CreateNewServer();
}
