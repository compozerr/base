using Carter;
using Core.Feature;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Template.Features.Example.Queries;

public record GetExampleResponse(string Message);

public class GetExample : CarterModule
{
    public GetExample() : base("/")
    {
        WithTags(nameof(Example));
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/example", (string name) => new GetExampleResponse($"Hello, {name}!"));
    }
}