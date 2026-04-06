using Api.Data;
using Api.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Cli.Endpoints.Modules.List;

public record MyModuleDto(string Name, string RepoUrl);

public static class ListMyModulesRoute
{
    public const string Route = "mine";

    public static RouteHandlerBuilder AddListMyModulesRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, async (ApiDbContext db) =>
        {
            var modules = await db.Modules
                .OrderBy(m => m.Name)
                .Select(m => new MyModuleDto(m.Name, m.RepoUri.ToString()))
                .ToListAsync();

            return Results.Ok(modules);
        });
    }
}
