using Carter;
using Cli.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cli.Features.Docker;

public class Push : ICarterModule
{

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/docker/push", async (HttpContext context, [FromHeader(Name = "x-api-key")] string apiKey) =>
        {
            var apiKeyService = context.RequestServices.GetRequiredService<IApiKeyService>();
            if (!await apiKeyService.ValidateApiKeyAsync())
            {
                return Results.Unauthorized();
            }

            return Results.Ok("Pushing docker image...");
        })
        .WithTags(nameof(Docker));
    }
}