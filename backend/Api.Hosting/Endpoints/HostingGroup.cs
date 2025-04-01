using Api.Data.Repositories;
using Api.Hosting.Endpoints.Deployments;
using Api.Hosting.Endpoints.Projects;
using Carter;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Api.Hosting.Endpoints;

public class HostingGroup : CarterModule
{
    public HostingGroup() : base("hosting")
    {
        WithTags(nameof(Hosting));
    }

    private static async Task<bool> HasValidApiKey(string apiKey, IServerRepository serverRepository)
    {
        var server = await serverRepository.GetServerOrDefaultByTokenAsync(apiKey);

        return server is { };
    }

    public static async ValueTask<object?> ApiKeyMiddleware(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            return await next(context);
        }

        var httpContext = context.HttpContext;

        var serverRepository = httpContext.RequestServices.GetRequiredService<IServerRepository>();

        if (!httpContext.Request.Headers.TryGetValue("x-api-key", out var apiKeyValues) || !await HasValidApiKey(apiKeyValues.ToString(), serverRepository))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddDeploymentsGroup().AddEndpointFilter(ApiKeyMiddleware);
        app.AddProjectsGroup().AddEndpointFilter(ApiKeyMiddleware);
    }
}
