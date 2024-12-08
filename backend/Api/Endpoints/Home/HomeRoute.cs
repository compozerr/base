
using Template;

namespace Api.Endpoints.Home;

public static class HomeRoute
{
    public static RouteHandlerBuilder AddHomeRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/", (IConfiguration configuration) => $"{ExampleClass.ExampleMethod()}");
    }
}