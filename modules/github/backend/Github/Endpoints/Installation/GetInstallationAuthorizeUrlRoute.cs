using Microsoft.AspNetCore.Builder;

namespace Github.Endpoints.Installation;

public static class GetInstallationAuthorizeUrlRoute
{
    public const string Route = "get-install-app-url";
    public static RouteHandlerBuilder AddGetInstallAppUrlRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, () => "https://github.com/apps/compozerr/installations/select_target");
    }
}