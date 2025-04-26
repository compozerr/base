namespace Api.Endpoints.Projects.Usage.Get;

public static class GetUsageRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddGetUsageRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task ExecuteAsync()
    {
        return Task.CompletedTask;
    }
}
