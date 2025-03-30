namespace Api.Endpoints.Projects.Domains.Add;

public static class AddDomainRoute
{
    public const string Route = "";

    public static RouteHandlerBuilder AddAddDomainRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task ExecuteAsync()
    {
        return Task.CompletedTask;
    }
}
