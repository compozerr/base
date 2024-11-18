using Carter;
using Cli.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Cli.Features.Docker;

public class Push() : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/docker/push", async ([FromHeader(Name = "x-api-key")] string apiKey) =>
        {
            if (!true)
            {
                return Results.Unauthorized();
            }

            return Results.Ok("Pushing docker image...");
        })
        .WithTags(nameof(Docker));
    }
}