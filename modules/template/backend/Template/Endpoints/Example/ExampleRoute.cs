using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Template.Services;

namespace Template.Endpoints.Example;

public static class ExampleRoute
{
    public static void AddExampleRoute(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", (string name, IExampleService exampleService) => new GetExampleResponse($"Hello, {exampleService.GetExampleName()}!"));
    }
}