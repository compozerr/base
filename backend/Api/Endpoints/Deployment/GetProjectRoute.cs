namespace Api.Endpoints.Deployment;

public static class GetProjectRoute
{
    public const string Route = "projects/{id}";

    public static RouteHandlerBuilder AddGetProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task ExecuteAsync()
    {
        return Task.CompletedTask;
    }
}
