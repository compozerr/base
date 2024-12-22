using Microsoft.AspNetCore.Builder;

namespace Github.Endpoints.Installation;

public static class GetInstallAppUrlRoute
{
    public const string Route = "get-install-app-url";
    public static RouteHandlerBuilder AddGetInstallAppUrlRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, () => new { InstallUrl = "https://github.com/apps/compozerr/installations/select_target" });
    }
}