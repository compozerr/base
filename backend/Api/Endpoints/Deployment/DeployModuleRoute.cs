namespace Api.Endpoints.Deployment;

public static class DeployModuleRoute
{
    public const string Route = "/modules";

    public static RouteHandlerBuilder AddDeployModuleRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task ExecuteAsync()
    {
        return Task.CompletedTask;
    }
}
