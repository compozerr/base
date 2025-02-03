using Auth.Services;

namespace Api.Endpoints.Deployment;

public static class DeployProjectRoute
{
    public const string Route = "/projects";

    public static RouteHandlerBuilder AddDeployProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task ExecuteAsync(
        ICurrentUserAccessor currentUserAccessor)
        => throw new NotImplementedException();
}
