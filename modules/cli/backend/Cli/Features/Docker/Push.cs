using Core.Feature;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Cli.Features.Docker;

public class Push : IFeature
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/docker/push", async ([FromHeader] string apiKey) =>
        {
            await Task.Delay(1000);
            return Results.Ok("Pushing docker image...");
        })
        .WithTags(nameof(Docker));
    }
}