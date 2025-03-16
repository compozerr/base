using Api.Data.Repositories;
using Api.Hosting.Endpoints.Deployments;
using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.IdentityModel.Tokens;

namespace Api.Hosting.Endpoints;

public class HostingGroup : CarterModule
{
    private readonly IServerRepository _serverRepository;

    public HostingGroup(IServerRepository serverRepository) : base("hosting")
    {
        _serverRepository = serverRepository;
        WithTags(nameof(Hosting));
    }

    private async Task<bool> HasValidApiKey(string apiKey)
    {
        var server = await _serverRepository.GetServerOrDefaultByTokenAsync(apiKey);

        return server is { };
    }

    public async ValueTask<object?> ApiKeyMiddleware(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        if (!httpContext.Request.Headers.TryGetValue("x-api-key", out var apiKeyValues) || !await HasValidApiKey(apiKeyValues.ToString()))
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
