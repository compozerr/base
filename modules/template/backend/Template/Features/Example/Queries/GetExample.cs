using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Template.Features.Example.Queries;

public class GetExample : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/example", async context =>
        {
            await context.Response.WriteAsJsonAsync(new { Message = "Hello World!" });
        })
        .WithName(nameof(GetExample));
    }
}