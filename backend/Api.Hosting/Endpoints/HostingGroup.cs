using System.Security.Cryptography.X509Certificates;
using Api.Data.Repositories;
using Api.Hosting.Endpoints.Deployments;
using Api.Hosting.Endpoints.Projects;
using Api.Hosting.Endpoints.VMPooling;
using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

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
        var isDevelopment = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
        Log.ForContext("isDevelopment", isDevelopment).Information("isDevelopment");

        if (isDevelopment)
            return await next(context);

        var httpContext = context.HttpContext;

        var serverRepository = httpContext.RequestServices.GetRequiredService<IServerRepository>();

        if (!httpContext.Request.Headers.TryGetValue("x-api-key", out var apiKeyValues) || !await HasValidApiKey(apiKeyValues.ToString(), serverRepository))
        {
            Log.ForContext("apiKey", apiKeyValues).Information("Unauthorized");
            return Results.Unauthorized();
        }

        return await next(context);
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddDeploymentsGroup().AddEndpointFilter(ApiKeyMiddleware);
        app.AddProjectsGroup().AddEndpointFilter(ApiKeyMiddleware);

#if DEBUG
        app.AddVMPoolingGroup().RequireAuthorization();
#endif
    }
}
