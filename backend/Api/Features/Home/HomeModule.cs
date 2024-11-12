
using Template;

namespace Api.Features.Home;

public class HomeModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => ExampleClass.ExampleMethod());
    }
}
