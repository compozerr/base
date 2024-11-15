
using Core.Feature;
using Template;

namespace Api.Features.Home;

public class HomeModule : IFeature
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => ExampleClass.ExampleMethod())
           .WithTags(nameof(Home));
    }
}
