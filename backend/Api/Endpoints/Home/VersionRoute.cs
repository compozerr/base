namespace Api.Endpoints.Home;

public static class VersionRoute
{
    public static RouteHandlerBuilder AddVersionRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/v1/version", () =>
        {
            var version = System.Environment.GetEnvironmentVariable("APP_VERSION") ?? "dev";
            return Results.Ok(new { version });
        }).AllowAnonymous();
    }
}
