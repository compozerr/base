using Carter;
using Microsoft.AspNetCore.Routing;

namespace Template.Endpoints.Example;

public class ExampleGroup : CarterModule
{
    public ExampleGroup() : base("/example")
    {
        WithTags(nameof(Example));
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddExampleRoute();
    }
}