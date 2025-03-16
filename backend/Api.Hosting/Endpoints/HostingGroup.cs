using Api.Data.Repositories;
using Api.Hosting.Endpoints.Deployments;
using Carter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Hosting.Endpoints;

public class HostingGroup : CarterModule
{
    public HostingGroup() : base("hosting")
    {
        WithTags(nameof(Hosting));
    }

    private async Task<bool> HasValidApiKey(string apiKey, IServerRepository serverRepository)
    {
        var server = await serverRepository.GetServerOrDefaultByTokenAsync(apiKey);

        return server is { };
    }

    public async ValueTask<object?> ApiKeyMiddleware(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
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
    }
}
