using Cli.Services;
using Core.Feature;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Cli.Features.Docker;

public class Push(IApiKeyService apiKeyService) : IFeature
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/docker/push", async ([FromHeader(Name = "x-api-key")] string apiKey) =>
        {
            if (!await apiKeyService.ValidateApiKeyAsync())
            {
                return Results.Unauthorized();
            }

            return Results.Ok("Pushing docker image...");
        })
        .WithTags(nameof(Docker));
    }
}